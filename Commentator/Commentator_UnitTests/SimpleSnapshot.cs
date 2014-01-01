using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;

namespace Commentator_UnitTests
{
    public class SimpleSnapshot : ITextSnapshot
    {
        const string LineEnd = "\r\n";
        private List<SimpleLine> lines = new List<SimpleLine>();
        private string text;

        public SimpleSnapshot(params string[] lines)
        {
            this.text = string.Join(LineEnd, lines);

            var lineNumber = 0;
            var offset = 0;
            foreach (var line in lines)
            {
                var tempLine = line;

                if (line != lines.Last())
                {
                    tempLine = string.Concat(line, LineEnd);
                }

                this.lines.Add(new SimpleLine(this, lineNumber, offset, tempLine));

                offset += tempLine.Length;
                lineNumber++;
            }
        }

        public int Length { get { return this.text.Length; } }

        public int LineCount { get { return this.lines.Count; } }

        public IEnumerable<ITextSnapshotLine> Lines { get { return this.lines; } }

        public char this[int position] { get { return this.text[position]; } }

        public ITextSnapshotLine GetLineFromLineNumber(int lineNumber)
        {
            return this.lines[lineNumber];
        }

        public ITextSnapshotLine GetLineFromPosition(int position)
        {
            foreach (var line in this.lines)
            {
                if (position < line.EndIncludingLineBreak.Position)
                {
                    return line;
                }
            }

            return null;
        }

        public int GetLineNumberFromPosition(int position)
        {
            foreach (var line in this.lines)
            {
                if (position < line.EndIncludingLineBreak.Position)
                {
                    return line.LineNumber;
                }
            }

            return -1;
        }

        public string GetText()
        {
            return this.text;
        }

        public string GetText(int startIndex, int length)
        {
            return this.text.Substring(startIndex, length);
        }

        public string GetText(Span span)
        {
            return this.GetText(span.Start, span.Length);
        }

        #region ITextSnapshot NotImplemented

        public Microsoft.VisualStudio.Utilities.IContentType ContentType
        {
            get { throw new NotImplementedException(); }
        }

        public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            throw new NotImplementedException();
        }

        public ITrackingPoint CreateTrackingPoint(int position, PointTrackingMode trackingMode, TrackingFidelityMode trackingFidelity)
        {
            throw new NotImplementedException();
        }

        public ITrackingPoint CreateTrackingPoint(int position, PointTrackingMode trackingMode)
        {
            throw new NotImplementedException();
        }

        public ITrackingSpan CreateTrackingSpan(int start, int length, SpanTrackingMode trackingMode, TrackingFidelityMode trackingFidelity)
        {
            throw new NotImplementedException();
        }

        public ITrackingSpan CreateTrackingSpan(int start, int length, SpanTrackingMode trackingMode)
        {
            throw new NotImplementedException();
        }

        public ITrackingSpan CreateTrackingSpan(Span span, SpanTrackingMode trackingMode, TrackingFidelityMode trackingFidelity)
        {
            throw new NotImplementedException();
        }

        public ITrackingSpan CreateTrackingSpan(Span span, SpanTrackingMode trackingMode)
        {
            throw new NotImplementedException();
        }

        public ITextBuffer TextBuffer
        {
            get { throw new NotImplementedException(); }
        }

        public char[] ToCharArray(int startIndex, int length)
        {
            throw new NotImplementedException();
        }

        public ITextVersion Version
        {
            get { throw new NotImplementedException(); }
        }

        public void Write(System.IO.TextWriter writer)
        {
            throw new NotImplementedException();
        }

        public void Write(System.IO.TextWriter writer, Span span)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
