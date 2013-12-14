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
            // TODO: Only kick in for single changes...
            // Gather *all* the consecutive matching lines and re-wrap?
            foreach (var change in e.Changes)
            {
                // TODO: only kick in for single line?
                var firstLine = e.After.GetLineNumberFromPosition(change.NewPosition);
                var lastLine = e.After.GetLineNumberFromPosition(change.NewEnd);

                for (var lineNumber = firstLine; lineNumber <= lastLine; lineNumber++)
                {
                    var line = e.After.GetLineFromLineNumber(lineNumber);
                    var snapshot = line.Snapshot;
                    var buffer = snapshot.TextBuffer;

                    // Get the line comment info for the previous, current, and next
                    // lines...
                    LineCommentInfo prevInfo = null;
                    LineCommentInfo info = null;
                    LineCommentInfo nextInfo = null;

                    info = LineCommentInfo.FromLine(line, this.classifier);

                    if (info == null)
                    {
                        continue;
                    }

                    if (line.LineNumber > 0)
                    {
                        prevInfo = LineCommentInfo.FromLine(
                            snapshot.GetLineFromLineNumber(line.LineNumber - 1),
                            this.classifier);
                    }

                    if (line.LineNumber + 1 < snapshot.LineCount)
                    {
                        nextInfo = LineCommentInfo.FromLine(
                            snapshot.GetLineFromLineNumber(line.LineNumber + 1),
                            this.classifier);
                    }

                    // TODO: Check to see if we need to back-fill to the previous line?

                    // Check to see if we need to wrap to the next line.
                    if (info.Line.Length < LineLimit ||
                        info.ContentSpan.Start - info.Line.Start >= LineLimit)
                    {
                        continue;
                    }

                    // Figure out what text we'll be wrapping to the next line...
                    // Find the last whitespace before limit and wrap...
                    var text = info.ContentSpan.GetText();
                    var contentStartColumn = info.ContentSpan.Start - info.Line.Start;
                    var wrapIndex = text.LastIndexOfAny(Whitespace, LineLimit - contentStartColumn - 1);
                    var wrapPoint = info.ContentSpan.Start + wrapIndex;
                    var toWrap = text.Substring(wrapIndex + 1); // TODO: trim extra whitespace?

                    if (!buffer.CheckEditAccess())
                    {
                        continue;
                    }

                    using (var edit = buffer.CreateEdit())
                    {
                        edit.Delete(Span.FromBounds(wrapPoint, info.ContentSpan.End));

                        // If we need to wrap, check to see if the next line looks like
                        // a continuation of the current line.
                        if (info.Matches(nextInfo))
                        {
                            var postWordWhitespace = " ";

                            if (toWrap.EndsWith(".") || toWrap.EndsWith("!") || toWrap.EndsWith("?"))
                            {
                                postWordWhitespace = "  ";
                            }

                            edit.Insert(
                                nextInfo.ContentSpan.Start.Position,
                                string.Concat(toWrap, postWordWhitespace));
                        }
                        else
                        {
                            // we need a new line!
                            var leadingWhitespace = string.Empty;

                            if (info.MarkerSpan.Start > info.Line.Start)
                            {
                                leadingWhitespace = new string(' ', info.MarkerSpan.Start - info.Line.Start);
                            }

                            var postMarkerWhitespace = new SnapshotSpan(info.MarkerSpan.End, info.ContentSpan.Start).GetText();

                            edit.Insert(
                                info.Line.End.Position,
                                string.Concat("\r\n", leadingWhitespace, info.MarkerSpan.GetText(), postMarkerWhitespace, toWrap));
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
}
