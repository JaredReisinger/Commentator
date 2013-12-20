using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;

namespace Commontator_UnitTests
{
    public class SimpleLine : ITextSnapshotLine
    {
        string text;

        public SimpleLine(SimpleSnapshot snapshot, int lineNumber, int start, string text)
        {
            this.Snapshot = snapshot;
            this.text = text;

            this.LineNumber = lineNumber;

            this.LengthIncludingLineBreak = this.text.Length;
            this.Length = this.LengthIncludingLineBreak;
            this.LineBreakLength = 0;

            if (this.Length >= 1 && text[this.Length - 1] == '\n')
            {
                this.Length--;
                this.LineBreakLength++;
            }

            if (this.Length >= 1 + this.LineBreakLength && text[this.Length - 1 - this.LineBreakLength] == '\r')
            {
                this.Length--;
                this.LineBreakLength++;
            }

            this.Start = new SnapshotPoint(this.Snapshot, start);
            this.EndIncludingLineBreak = new SnapshotPoint(this.Snapshot, start + this.LengthIncludingLineBreak);
            this.End = new SnapshotPoint(this.Snapshot, start + this.Length);
            this.Extent = new SnapshotSpan(this.Start, this.End);
            this.ExtentIncludingLineBreak = new SnapshotSpan(this.Start, this.EndIncludingLineBreak);
        }

        public ITextSnapshot Snapshot { get; private set; }

        public int LineNumber { get; private set; }

        public int Length { get; private set; }

        public int LengthIncludingLineBreak { get; private set; }

        public int LineBreakLength { get; private set; }

        public SnapshotPoint Start { get; private set; }

        public SnapshotPoint End { get; private set; }

        public SnapshotPoint EndIncludingLineBreak { get; private set; }

        public SnapshotSpan Extent { get; private set; }

        public SnapshotSpan ExtentIncludingLineBreak { get; private set; }

        public string GetLineBreakText()
        {
            return this.text.Substring(this.Length, this.LineBreakLength);
        }

        public string GetText()
        {
            return this.text.Substring(0, this.Length);
        }

        public string GetTextIncludingLineBreak()
        {
            return this.text.Substring(0, this.LengthIncludingLineBreak);
        }
    }
}
