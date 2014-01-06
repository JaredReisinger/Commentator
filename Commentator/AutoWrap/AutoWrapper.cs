using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Spudnoggin.Commentator.AutoWrap
{
    internal class AutoWrapper
    {
        private GeneralOptions options;
        private IWpfTextView view;
        private IClassifier classifier;
        private SnapshotPoint newCaretPoint = default(SnapshotPoint);

        public AutoWrapper(SVsServiceProvider serviceProvider, IClassifierAggregatorService aggregatorService, IWpfTextView textView)
        {
            var service = serviceProvider.GetService<CommentatorService>();
            this.options = service.GetOptions();

            this.view = textView;
            this.classifier = aggregatorService.GetClassifier(this.view.TextBuffer);

            this.view.TextBuffer.Changed += this.TextBuffer_Changed;
            this.view.Closed += this.View_Closed;
        }

        void View_Closed(object sender, EventArgs e)
        {
            this.view.TextBuffer.Changed -= this.TextBuffer_Changed;
            this.view.Closed -= this.View_Closed;
        }

        readonly char[] Whitespace = new char[] { ' ', '\t', '\r', '\n' };
        readonly char[] DoubleSpace = new char[] { '.', '?', '!' };

        private void TextBuffer_Changed(object sender, TextContentChangedEventArgs e)
        {
            if (!this.options.AutoWrapEnabled)
            {
                return;
            }

            // The auto-wrap should only apply during simple typing.  Region
            // edits (simultaneous multi-line typing) could lead to truly bizarre
            // wrapping behavior, so it really doesn't make sense.
            if (e.Changes.Count() > 1)
            {
                return;
            }

            var snapshot = e.After;
            var change = e.Changes[0];
            var buffer = snapshot.TextBuffer;
            var caret = this.view.Caret.Position;  // caret is in *old* snapshot, typically(!?)
            var caretPosition = caret.Point.GetPoint(snapshot, caret.Affinity).GetValueOrDefault();

            // If the caret isn't near the change, this was an undo, or other operation
            // we shouldn't wrap on!
            if ((caretPosition.Snapshot != snapshot) ||
                (caretPosition < change.NewPosition) ||
                (caretPosition > change.NewEnd))
            {
                return;
            }

            if (!buffer.CheckEditAccess())
            {
                return;
            }

            var firstLine = snapshot.GetLineNumberFromPosition(change.NewPosition);

            // Do some quick checks so we can fast-bail if this doesn't appear to
            // be a comment-line edit...
            var line = snapshot.GetLineFromLineNumber(firstLine);
            var info = LineCommentInfo.FromLine(line, this.classifier);

            if (info == null || (!info.CommentOnly && !this.options.CodeWrapEnabled))
            {
                return;
            }

            // For now, we're only supporting single-line comments.
            if (info.Style != CommentStyle.SingleLine)
            {
                return;
            }

            // If we just typed whitespace at the *end* of the line, don't do
            // any wrapping yet.  (It will cause us to trim the trailing space,
            // which would makeeverythingruntogetherlikethis!  It also makes
            // newline handling weird.)
            if ((change.Delta > 0) &&
                ((change.NewSpan.End >= line.End) && (change.NewSpan.End <= line.EndIncludingLineBreak)) &&
                change.NewText.All(c => Whitespace.Contains(c)))
            {
                return;
            }

            var commentWrapLength = this.options.AutoWrapColumn - info.ContentColumnStart;

            // There's some minimum length after which wrapping becomes
            // pointless...
            if (commentWrapLength < this.options.MinimumWrapWidth)
            {
                return;
            }

            var leadingWhitespace = string.Empty;

            if (info.MarkerSpan.Start > info.Line.Start)
            {
                // Figure out the leading whitespace.  If there's no code on the
                // line (it's comment-only), we can just take the span from the
                // line start to the marker start.  Otherwise, we try to create
                // the proper combination of tabs and spaces based on the editor
                // settings.
                if (info.CommentOnly)
                {
                    leadingWhitespace = new SnapshotSpan(info.Line.Start, info.MarkerSpan.Start).GetText();
                }
                else
                {
                    bool convertTabs;
                    var convertTabsOption = new ConvertTabsToSpaces();
                    // REVIEW: Is this the correct place to look for the option?
                    if (!buffer.Properties.TryGetProperty(convertTabsOption.Key, out convertTabs))
                    {
                        convertTabs = convertTabsOption.Default;
                    }

                    if (convertTabs)
                    {
                        leadingWhitespace = new string(' ', info.MarkerColumnStart);
                    }
                    else
                    {
                        int tabSize;
                        var tabSizeOption = new TabSize();
                        // REVIEW: Is this the correct place to look for the option?
                        if (!buffer.Properties.TryGetProperty(tabSizeOption.Key, out tabSize))
                        {
                            tabSize = tabSizeOption.Default;
                        }

                        leadingWhitespace = new string('\t', info.MarkerColumnStart / tabSize);

                        // Add spaces if the marker-start isn't a multiple of
                        // the tab size.
                        if (info.MarkerColumnStart % tabSize != 0)
                        {
                            leadingWhitespace = string.Concat(
                                leadingWhitespace,
                                new string(' ', info.MarkerColumnStart % tabSize));
                        }
                    }
                }
            }

            var marker = info.MarkerSpan.GetText();

            var postMarkerWhitespace = new SnapshotSpan(info.MarkerSpan.End, info.ContentSpan.Start).GetText();

            // Do we need to check to see if the edit appeared to happen within
            // the comment span?

            // Get all of the consecutive lines with matching comment info.  They
            // all have the potential to be re-wrapped when edits happen. We
            // could theoretically only grab the previous line--for back-wrapping--
            // and subsequent lines only as needed.  But the odds are that folks
            // aren't editing a several-thousand-line comment!
            ////var lastLine = snapshot.GetLineNumberFromPosition(change.NewEnd);
            var comments = new List<LineCommentInfo>();

            // Start by getting all of the matching lines that follow the beginning
            // of the change...
            comments.Add(info);

            // Figure out the relative position of the caret within the comment
            // content.  We'll need this to dead-reckon the final location the
            // caret should have.
            var caretContentOffset = caretPosition - info.ContentSpan.Start;
            var caretCommentIndex = 0;

            for (var lineNumber = firstLine + 1; lineNumber < snapshot.LineCount; lineNumber++)
            {
                line = snapshot.GetLineFromLineNumber(lineNumber);
                info = LineCommentInfo.FromLine(line, this.classifier);

                // TODO: treat lines with code (non-comment) as non-matching
                // so that behavior is better?
                if (!comments[0].Matches(info))
                {
                    break;
                }

                comments.Add(info);
            }

            // Work backwards to find any previous comments that are a part of
            // the same block...
            for (var lineNumber = firstLine - 1; lineNumber >= 0; lineNumber--)
            {
                line = snapshot.GetLineFromLineNumber(lineNumber);
                info = LineCommentInfo.FromLine(line, this.classifier);

                if (!comments[0].Matches(info))
                {
                    break;
                }

                firstLine = lineNumber;
                comments.Insert(0, info);
                caretCommentIndex++;
            }

            // Now build of a list of "properly wrapped" text for the entire
            // range of lines... Rather than taking each line piecemeal, we
            // concatenate all of the comments together, and wrap completely.
            // (We might end up removing/adding lines.)
            var builder = new StringBuilder(commentWrapLength * comments.Count);

            foreach (var comment in comments)
            {
                // Add proper whitespace depending on the previous text...
                if (builder.Length > 0)
                {
                    var wrappingWhitespace = " ";

                    if (DoubleSpace.Contains(builder[builder.Length - 1]))
                    {
                        wrappingWhitespace = "  ";
                    }

                    builder.Append(wrappingWhitespace);

                    if (caretCommentIndex >= 0)
                    {
                        caretContentOffset += wrappingWhitespace.Length;
                    }
                }

                var text = comment.ContentSpan.GetText().Trim();
                builder.Append(text);

                if (caretCommentIndex > 0)
                {
                    caretContentOffset += text.Length;
                }

                caretCommentIndex--;
            }

            // Now update all of the comment spans in the range...
            using (var edit = buffer.CreateEdit())
            {
                var caretLineOffset = 0;
                var caretPositionOffset = 0;

                foreach (var comment in comments)
                {
                    if (builder.Length > 0)
                    {
                        string final;

                        if (builder.Length > commentWrapLength)
                        {
                            // Find the next line-break...
                            var candidate = builder.ToString(0, commentWrapLength + 1);
                            var end = candidate.LastIndexOfAny(Whitespace);

                            // TODO: if there isn't a wrap-point, take the whole string
                            // and look forward... we'll just have to live with a long
                            // line.  For now, we force-wrap at limit.
                            if (end < 0)
                            {
                                end = commentWrapLength;
                            }

                            // Only replace if we ended up with different text...
                            final = candidate.Substring(0, end);

                            while ((builder.Length > end) && Whitespace.Contains(builder[end]))
                            {
                                end++;
                            }

                            // Remove the line and any trailing whitespace
                            builder.Remove(0, end);

                            if (end < caretContentOffset)
                            {
                                caretContentOffset -= end;
                                caretLineOffset++;
                            }
                            else if (final.Length < caretContentOffset)
                            {
                                // The caret was in the trailing whitespace...
                                // should we advance to the next line, or
                                // leave it at the end of the current line?
                                caretPositionOffset = final.Length;
                                caretContentOffset = -1;

                            }
                            else if (caretContentOffset > -1)
                            {
                                caretPositionOffset = caretContentOffset;
                                caretContentOffset = -1;
                            }
                        }
                        else
                        {
                            final = builder.ToString();
                            builder.Clear();

                            if (caretContentOffset > -1)
                            {
                                caretPositionOffset = caretContentOffset;
                                caretContentOffset = -1;
                            }
                        }

                        if (final.Length != comment.ContentSpan.Length ||
                            !string.Equals(final, comment.ContentSpan.GetText(), StringComparison.Ordinal))
                        {
                            edit.Replace(comment.ContentSpan.Span, final);
                        }
                    }
                    else
                    {
                        if (comment.CommentOnly)
                        {
                            edit.Delete(comment.Line.Start, comment.Line.LengthIncludingLineBreak);
                        }
                        else if (comment.ContentSpan.Length > 0)
                        {
                            // Should we leave the marker, or take it out?
                            edit.Delete(comment.ContentSpan.Span);
                        }
                    }
                }

                // Add new lines as needed...
                if (builder.Length > 0)
                {
                    var newLines = new StringBuilder();

                    while (builder.Length > 0)
                    {
                        string final;

                        if (builder.Length > commentWrapLength)
                        {
                            // Find the next line-break...
                            var candidate = builder.ToString(0, commentWrapLength + 1);
                            var end = candidate.LastIndexOfAny(Whitespace);

                            // TODO: if there isn't a wrap-point, take the whole string
                            // and look forward... we'll just have to live with a long
                            // line.  For now, we force-wrap at limit.
                            if (end < 0)
                            {
                                end = commentWrapLength;
                            }

                            final = candidate.Substring(0, end);

                            while ((builder.Length > end) && Whitespace.Contains(builder[end]))
                            {
                                end++;
                            }

                            builder.Remove(0, end);

                            if (end < caretContentOffset)
                            {
                                caretContentOffset -= end;
                                caretLineOffset++;
                            }
                            else if (final.Length < caretContentOffset)
                            {
                                // The caret was in the trailing whitespace...
                                // should we advance to the next line, or
                                // leave it at the end of the current line?
                                caretPositionOffset = final.Length;
                                caretContentOffset = -1;

                            }
                            else if (caretContentOffset > -1)
                            {
                                caretPositionOffset = caretContentOffset;
                                caretContentOffset = -1;
                            }
                        }
                        else
                        {
                            final = builder.ToString();
                            builder.Clear();

                            if (caretContentOffset > -1)
                            {
                                caretPositionOffset = caretContentOffset;
                                caretContentOffset = -1;
                            }
                        }

                        // Create a new line...
                        newLines.Append("\r\n");
                        newLines.Append(leadingWhitespace);
                        newLines.Append(marker);
                        newLines.Append(postMarkerWhitespace);
                        newLines.Append(final);
                    }

                    edit.Insert(
                        comments.Last().Line.End.Position,
                        newLines.ToString());
                }

                if (edit.HasEffectiveChanges)
                {
                    var newSnapshot = edit.Apply();

                    // Try to set the new caret position...
                    var newCaretLine = newSnapshot.GetLineFromLineNumber(firstLine + caretLineOffset);
                    info = LineCommentInfo.FromLine(newCaretLine, this.classifier);
                    if (info != null)
                    {
                        this.newCaretPoint = info.ContentSpan.Start + caretPositionOffset;

                        // We will need to update the caret to the new location, but
                        // we can't do that until the view updates to the new snapshot
                        // that contains the changes.
                        this.view.LayoutChanged += UpdateCaretEventually;
                    }
                }
                else
                {
                    edit.Cancel();
                }
            }

            // TODO: check to see if the caret/selection needs to be updated,
            // because we might have just moved the text behind it to the
            // next line... (no longer needed... this should trigger an
            // additional change...
        }

        private void UpdateCaretEventually(object sender, TextViewLayoutChangedEventArgs e)
        {
            var removeHandler = true;

            if (!this.newCaretPoint.Equals(default(SnapshotPoint)))
            {
                var layoutVersion = e.NewSnapshot.Version.VersionNumber;
                var caretVersion = this.newCaretPoint.Snapshot.Version.VersionNumber;

                if (layoutVersion < caretVersion)
                {
                    removeHandler = false;

                }
                if (layoutVersion == caretVersion)
                {
                    this.view.Caret.MoveTo(newCaretPoint);
                }
            }

            if (removeHandler)
            {
                this.view.LayoutChanged -= this.UpdateCaretEventually;

            }
        }
    }
}
