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
        private IWpfTextView view;
        private IClassifierAggregatorService aggregator;
        private IClassifier classifier;

        const int LineLimit = 40;   // short for testing!
        public AutoWrapper(IWpfTextView textView, IClassifierAggregatorService aggregator)
        {
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

        void TextBuffer_Changed(object sender, Microsoft.VisualStudio.Text.TextContentChangedEventArgs e)
        {
            foreach (var change in e.Changes)
            {
                var line = e.After.GetLineFromPosition(change.NewPosition);
                var buffer = line.Snapshot.TextBuffer;

                if (line.Length < LineLimit)
                {
                    continue;
                }
                // Check if the line ends with a comment...
                var spans = this.classifier.GetClassificationSpans(line.Extent);

                // If there are no spans, or the last span isn't a comment
                // (multi-line issues?), or if the comment begins *after* the
                // LineLimit, don't even attempt to wrap...
                var comment = (spans != null) ? spans.LastOrDefault() : null;

                if (comment == null ||
                    !comment.ClassificationType.IsOfType("comment"))
                {
                    continue;
                }

                var commentStartColumn = comment.Span.Start - line.Start;

                if (commentStartColumn >= LineLimit)
                {
                    continue;
                }

                // We now know we've got a comment that extends across our limit...
                // look for an matching comment on the next line.  If there isn't
                // one, we'll have to create a new line...
                var needNewLine = false;
                SnapshotPoint wrapPoint = new SnapshotPoint();
                if (line.Snapshot.LineCount <= line.LineNumber + 1)
                {
                    needNewLine = true;
                }
                else
                {
                    var nextLine = line.Snapshot.GetLineFromLineNumber(line.LineNumber + 1);
                    var nextSpans = this.classifier.GetClassificationSpans(nextLine.Extent);

                    // If there are no spans, or the last span isn't a comment
                    // (multi-line issues?), or if the comment starts in a different
                    // place, it's not a candidate.
                    var nextComment = (nextSpans != null) ? nextSpans.LastOrDefault() : null;

                    if (nextComment == null ||
                        !nextComment.ClassificationType.IsOfType("comment") ||
                        nextComment.Span.Start - nextLine.Start != commentStartColumn)
                    {
                        needNewLine = true;
                    }
                    else
                    {
                        // We've got a matching comment, so inject there!
                        wrapPoint = nextComment.Span.Start + 3;  // TODO: fix padding!
                    }
                }

                // Find the last whitespace before limit and wrap...
                var text = line.GetText();
                var lastIndex = text.LastIndexOfAny(Whitespace, LineLimit - 1);
                var toWrap = text.Substring(lastIndex + 1);
                // TODO: find the non-whitespace end of the line...

                if (buffer.CheckEditAccess())
                {
                    var edit = buffer.CreateEdit();
                    // Use ".Position" so that the locations aren't shifted as
                    // we make changes to the new snapshot!
                    edit.Delete(line.Start.Position + lastIndex, toWrap.Length + 1);

                    if (needNewLine)
                    {
                        // TODO: Do we need to add line-break to the current line too?
                        edit.Insert(line.EndIncludingLineBreak.Position,
                            string.Concat("// ", toWrap, line.GetLineBreakText()));
                    }
                    else
                    {
                        edit.Insert(wrapPoint.Position,
                            string.Concat(toWrap, " "));
                    }
                    edit.Apply();
                }

                // TODO: check to see if the caret/selection needs to be updated,
                // because we might have just moved the text behind it to the
                // next line... (no longer needed... this should trigger an
                // additional change...
            }
        }
    }
}
