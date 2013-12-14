using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Spudnoggin.Commontator.AutoWrap
{
    public enum CommentStyle
    {
        SingleLine,
        DocComment,
        CodeComment,
        // Should this be a 'Flags' enum because of the multi-line values?
        // A comment an start and end, or continue and end on the same line.
        MultiLineStart,
        MultiLineContinuation,
        MultiLineEnd,
    }

    [DebuggerDisplay("(@{Line.LineNumber}, {Span.Start-Line.Start} {CommentStyles}) {Span.GetText()}")]
    class LineCommentInfo
    {
        readonly char[] Whitespace = new char[] { ' ', '\t' };
        readonly char[] FormatMarkers = new char[] { '+', '-', '!', '?' };

        private LineCommentInfo(ITextSnapshotLine line, SnapshotSpan span)
        {
            this.Line = line;
            this.Span = span;

            // Is there a reliable way to detect we're inside a multi-line comment?
            // for now, we assume single-line...
            var text = span.GetText();

            if (text.StartsWith("////") || text.StartsWith("''''"))
            {
                // A code-comment, probably *shouldn't* be wrapped!
                this.Style = CommentStyle.CodeComment;
                this.MarkerSpan = new SnapshotSpan(span.Start, 4);
            }
            else if (text.StartsWith("///") || text.StartsWith("'''"))
            {
                this.Style = CommentStyle.DocComment;
                this.MarkerSpan = new SnapshotSpan(span.Start, 3);
            }
            else if (text.StartsWith("//") || text.StartsWith("''"))
            {
                this.Style = CommentStyle.SingleLine;

                // Look for special leading characters (+, -, !, ?) commonly
                // used in other comment-formatters, and consider them a part
                // of the marker...
                var extra = text.Skip(2).TakeWhile(c => FormatMarkers.Contains(c)).Count();

                this.MarkerSpan = new SnapshotSpan(span.Start, 2 + extra);
            }
            else if (text.StartsWith("/*"))
            {
                this.Style = CommentStyle.MultiLineStart;

                // Look for special leading characters (+, -, !, ?) commonly
                // used in other comment-formatters, and consider them a part
                // of the marker...
                var extra = text.Skip(2).TakeWhile(c => FormatMarkers.Contains(c)).Count();

                this.MarkerSpan = new SnapshotSpan(span.Start, 2 + extra);
            }
            else if ((text.StartsWith("/") || text.StartsWith("'")) &&
                    (span.Start > line.Start) &&
                    ((span.Start - 1).GetChar() == span.Start.GetChar()))
            {
                // Weird case where the classifier gives a bogus result when typing
                // the "//" at the beginning of the line... perhaps we should *not*
                // wrap in this case?

                // Treat like "//" or "''" comment... but fix the span up!
                span = new SnapshotSpan(span.Start - 1, span.End);
                this.Span = span;
                text = span.GetText();

                this.Style = CommentStyle.SingleLine;

                // Look for special leading characters (+, -, !, ?) commonly
                // used in other comment-formatters, and consider them a part
                // of the marker...
                var extra = text.Skip(2).TakeWhile(c => FormatMarkers.Contains(c)).Count();

                this.MarkerSpan = new SnapshotSpan(span.Start, 2 + extra);
            }
            else
            {
                // We must be a continuation?
                this.Style = CommentStyle.MultiLineContinuation;
                this.MarkerSpan = new SnapshotSpan(span.Start, 0);
            }

            // Determine the post-marker whitespace (if any).
            var space = text.Skip(this.MarkerSpan.Length).TakeWhile(c => Whitespace.Contains(c)).Count();
            this.ContentSpan = new SnapshotSpan(span.Start + this.MarkerSpan.Length + space, span.End);

            ////var commentStartColumn = span.Span.Start - line.Start;
        }

        public static LineCommentInfo FromLine(ITextSnapshotLine line, IClassifier classifier)
        {
            // Check if the line ends with a comment...
            var spans = classifier.GetClassificationSpans(line.Extent);

            // If there are no spans, or the last span isn't a comment
            // (multi-line issues?) return no info.
            var span = (spans != null) ? spans.LastOrDefault() : null;

            if (span == null ||
                !span.ClassificationType.IsOfType("comment"))
            {
                return null;
            }

            var info = new LineCommentInfo(line, span.Span);
            return info;
        }

        /// <summary>
        /// Returns whether another comment looks like it's a part of the same
        /// block as the current one.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Matches(LineCommentInfo other)
        {
            if ((other != null) &&
                (this.Style == other.Style) &&
                (this.MarkerSpan.Length == other.MarkerSpan.Length) &&
                (this.MarkerSpan.Start - this.Line.Start == other.MarkerSpan.Start - other.Line.Start) &&
                string.Equals(this.MarkerSpan.GetText(), other.MarkerSpan.GetText()))
            {
                // The leading info appears to match.  If there's content in
                // both, we also want to make sure it starts in the same column.
                // If either line has blank content, should we consider it a
                // match?  (No, if we want to handle "paragraph breaks"...)
                // Actually, just because of this, we require matching content
                // starts.  If it doesn't, we create a new line that *does*
                // match, and all will be well.
                if ((this.ContentSpan.IsEmpty == other.ContentSpan.IsEmpty) &&
                    (this.ContentSpan.Start - this.MarkerSpan.End == other.ContentSpan.Start - other.MarkerSpan.End))
                {
                    return true;
                }
            }

            return false;

        }

        public ITextSnapshotLine Line { get; private set; }
        public SnapshotSpan Span { get; private set; }
        public CommentStyle Style { get; private set; }
        public SnapshotSpan MarkerSpan { get; private set; }
        public SnapshotSpan ContentSpan { get; private set; }
        public int MyProperty { get; private set; }
    }
}
