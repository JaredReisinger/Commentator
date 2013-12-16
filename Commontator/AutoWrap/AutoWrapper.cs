using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Spudnoggin.Commontator.AutoWrap
{
    sealed class AutoWrapper : IDisposable
    {
        private GeneralOptions options;
        private IWpfTextView view;
        private IClassifierAggregatorService aggregator;
        private IClassifier classifier;

        public AutoWrapper(IWpfTextView textView, IClassifierAggregatorService aggregator)
        {
            var service = (CommontatorService)Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider.GetService(typeof(CommontatorService));
            this.options = service.GetOptions();

            this.view = textView;
            this.aggregator = aggregator;
            this.classifier = aggregator.GetClassifier(this.view.TextBuffer);

            this.view.TextBuffer.Changed += TextBuffer_Changed;
        }

        public void Dispose()
        {
            this.view.TextBuffer.Changed -= TextBuffer_Changed;
        }

        readonly char[] Whitespace = new char[] { ' ', '\t' };
        readonly char[] DoubleSpace = new char[] { '.', '?', '!' };

        void TextBuffer_Changed(object sender, Microsoft.VisualStudio.Text.TextContentChangedEventArgs e)
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

            // If we just typed a space at the *end* of the line, don't do any
            // wrapping yet.  (It will cause us to trim the trailing space, which
            // would makeeverythingruntogetherlikethis!
            if (change.Delta == 1 &&
                change.OldLength == 0 &&
                change.NewLength == 1 &&
                Whitespace.Contains(change.NewText[0]))
            {
                return;
            }
            
            var buffer = snapshot.TextBuffer;

            if (!buffer.CheckEditAccess())
            {
                return;
            }

            var firstLine = snapshot.GetLineNumberFromPosition(change.NewPosition);

            // Do some quick checks so we can fast-bail if this doesn't appear to
            // be a comment-line edit...
            var line = snapshot.GetLineFromLineNumber(firstLine);
            var info = LineCommentInfo.FromLine(line, this.classifier);

            if (info == null)
            {
                return;
            }

            var commentWrapLength = this.options.AutoWrapColumn - (info.ContentSpan.Start - info.Line.Start);

            // There's some minimum length after which wrapping becomes pointless...
            if (commentWrapLength < this.options.MinimumWrapWidth)
            {
                return;
            }

            var leadingWhitespace = string.Empty;

            if (info.MarkerSpan.Start > info.Line.Start)
            {
                leadingWhitespace = new string(' ', info.MarkerSpan.Start - info.Line.Start);
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

            for (var lineNumber = firstLine + 1; lineNumber < snapshot.LineCount; lineNumber++)
            {
                line = snapshot.GetLineFromLineNumber(lineNumber);
                info = LineCommentInfo.FromLine(line, this.classifier);

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

                comments.Insert(0, info);
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

                    if (DoubleSpace.Contains(builder[builder.Length-1]))
                    {
                        wrappingWhitespace = "  ";
                    }

                    builder.Append(wrappingWhitespace);
                }

                builder.Append(comment.ContentSpan.GetText().Trim());
            }

            // Now update all of the comment spans in the range...
            using (var edit = buffer.CreateEdit())
            {
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

                            builder.Remove(0, end); // would be +1, but the force-wrap kills this...
                            if (Whitespace.Contains(builder[0]))
                            {
                                builder.Remove(0, 1);
                            }
                        }
                        else
                        {
                            final = builder.ToString();
                            builder.Clear();
                        }

                        if (final.Length != comment.ContentSpan.Length ||
                            !string.Equals(final, comment.ContentSpan.GetText(), StringComparison.Ordinal))
                        {
                            edit.Replace(comment.ContentSpan.Span, final);
                        }
                    }
                    else
                    {
                        if (comment.ContentSpan.Length > 0)
                        {
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

                            builder.Remove(0, end); // would be +1, but the force-wrap kills this...
                            if (Whitespace.Contains(builder[0]))
                            {
                                builder.Remove(0, 1);
                            }
                        }
                        else
                        {
                            final = builder.ToString();
                            builder.Clear();
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
                    edit.Apply();
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
    }
}
