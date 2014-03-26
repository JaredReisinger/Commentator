using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Classification.Fakes;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Fakes;
using Spudnoggin.Commentator;
using Spudnoggin.Commentator.AutoWrap;

namespace Commentator_UnitTests
{
    [TestClass]
    public class AutoWrapperTest
    {
        [TestMethod]
        public void AutoWrapper_Flatten_Simple()
        {
            Assert.AreEqual("a b", Flatten("a", "b"));
            Assert.AreEqual("a b c", Flatten("a", "b", "c"));
            Assert.AreEqual("a b c", Flatten("a b", "c"));
            Assert.AreEqual("a b c", Flatten("a", "b c"));
        }

        [TestMethod]
        public void AutoWrapper_Flatten_DoubleSpaceAfterPunctuation()
        {
            Assert.AreEqual("a.  b", Flatten("a.", "b"));
            Assert.AreEqual("a!  b", Flatten("a!", "b"));
            Assert.AreEqual("a?  b", Flatten("a?", "b"));
        }

        [TestMethod]
        public void AutoWrapper_Flatten_TrackOffset()
        {
            int caretContentIndex = 1;
            int caretContentOffset = 2;
            Assert.AreEqual("aaaaa bbbbb", Flatten(10, caretContentIndex, ref caretContentOffset, "aaaaa", "bbbbb"));
            Assert.AreEqual(8, caretContentOffset);
        }

        [TestMethod]
        public void AutoWrapper_Wrap_Simple()
        {
            AssertSequence(Rewrap("aaaaa bbbbb", 5), "aaaaa", "bbbbb");
            AssertSequence(Rewrap("aa bb cc dd", 5), "aa bb", "cc dd");
            AssertSequence(Rewrap("aaa bbb ccc", 5), "aaa", "bbb", "ccc");
        }

        [TestMethod]
        public void AutoWrapper_Wrap_OverlongLinesDontClip()
        {
            AssertSequence(Rewrap("123456", 5), "123456");
            AssertSequence(Rewrap("123456", 5), "123456");
            AssertSequence(Rewrap("aaaaa 123456", 5), "aaaaa", "123456");
            AssertSequence(Rewrap("123456 bbbbb", 5), "123456", "bbbbb");
            AssertSequence(Rewrap("aaaaa 123456 bbbbb", 5), "aaaaa", "123456", "bbbbb");
            AssertSequence(Rewrap("aaaaa 1234567890 bbbbb", 5), "aaaaa", "1234567890", "bbbbb");
        }

        [TestMethod]
        public void AutoWrapper_Wrap_TrackOffset()
        {
            int caretLineOffset;
            int caretPositionOffset;

            AssertSequence(
                Rewrap("aaaaa bbbbb", 5, 8, out caretLineOffset, out caretPositionOffset),
                "aaaaa",
                "bbbbb");

            Assert.AreEqual(1, caretLineOffset);
            Assert.AreEqual(2, caretPositionOffset);
        }

        [TestMethod]
        public void AutoWrapper_ShouldWrapPrelim_ShouldNotWrapWhenDiasbled()
        {
            Assert.IsFalse(ShouldWrapPrelim(optionsAutoWrapEnabled: false));
        }

        [TestMethod]
        public void AutoWrapper_ShouldWrapPrelim_ShouldNotWrapNonEditableBuffer()
        {
            var buffer = new SimpleBuffer(editable: false);
            Assert.IsFalse(ShouldWrapPrelim(buffer: buffer));
        }

        [TestMethod]
        public void AutoWrapper_ShouldWrapPrelim_ShouldNotWrapMultipleChanges()
        {
            var buffer = new SimpleBuffer(editable: true);
            var changes = new StubINormalizedTextChangeCollection();
            changes.CountGet = () => 2;
            Assert.IsFalse(ShouldWrapPrelim(buffer: buffer, changes: changes));
        }

        [TestMethod]
        public void AutoWrapper_ShouldWrapPrelim_ShouldNotWrapNonCaretChange()
        {
            var snapshot = new SimpleSnapshot(
                "// Caret is >|< here.",    // "|" at pos 13, len 21+2
                "// Change is >|< here.");  // "|" at pos 37
            var changes = new StubINormalizedTextChangeCollection();
            changes.CountGet = () => 1;
            changes.ItemGetInt32 = i =>
            {
                Assert.AreEqual(0, i);
                var change = new StubITextChange();
                change.NewPositionGet = () => 37;
                change.NewEndGet = () => 38;
                return change;
            };

            var mappingPoint = new StubIMappingPoint();
            mappingPoint.GetPointITextSnapshotPositionAffinity = (s, p) =>
                {
                    return new SnapshotPoint(s, 13);
                };

            var caretPosition = new CaretPosition(
                new VirtualSnapshotPoint(snapshot, 13),
                mappingPoint,
                PositionAffinity.Successor);

            Assert.IsFalse(ShouldWrapPrelim(
                buffer: snapshot.TextBuffer,
                changes: changes,
                snapshot: snapshot,
                caretPosition: caretPosition));
        }

        private static string Flatten(params string[] lines)
        {
            int offset = 0;
            return Flatten(20, 0, ref offset, lines);
        }

        private static string Flatten(int wrapLength, int caretContentIndex, ref int caretContentOffset, params string[] lines)
        {
            var flatten = typeof(AutoWrapper).GetMethod(
                "FlattenCommentParagraph",
                BindingFlags.NonPublic | BindingFlags.Static,
                System.Type.DefaultBinder,
                new Type[] { typeof(List<string>), typeof(int), typeof(int), typeof(int).MakeByRefType() },
                null);

            var parameters = new object[] { new List<string>(lines), wrapLength, caretContentIndex, caretContentOffset };
            var result = flatten.Invoke(null, parameters);
            caretContentOffset = (int)parameters[3];

            return ((StringBuilder)result).ToString();
        }

        private static List<string> Rewrap(string text, int wrapLength = 20)
        {
            int caretLineOffset;
            int caretPositionOffset;
            return Rewrap(text, wrapLength, 0, out caretLineOffset, out caretPositionOffset);
        }

        private static bool ShouldWrapPrelim(
            bool optionsAutoWrapEnabled = true,
            int optionsAvoidWrappingBeforeLine = 0,
            INormalizedTextChangeCollection changes = null,
            ITextSnapshot snapshot = null,
            ITextBuffer buffer = null,
            CaretPosition caretPosition = default(CaretPosition))
        {
            SnapshotPoint caretPoint;
            int firstLine;

            return ShouldWrapPrelim(
                optionsAutoWrapEnabled,
                optionsAvoidWrappingBeforeLine,
                changes,
                snapshot,
                buffer,
                caretPosition,
                out caretPoint,
                out firstLine);
        }

        private static bool ShouldWrapPrelim(
            out SnapshotPoint caretPoint,
            out int firstLine,
            bool optionsAutoWrapEnabled = true,
            int optionsAvoidWrappingBeforeLine = 0,
            INormalizedTextChangeCollection changes = null,
            ITextSnapshot snapshot = null,
            ITextBuffer buffer = null,
            CaretPosition caretPosition = default(CaretPosition))
        {
            return ShouldWrapPrelim(
                optionsAutoWrapEnabled,
                optionsAvoidWrappingBeforeLine,
                changes,
                snapshot,
                buffer,
                caretPosition,
                out caretPoint,
                out firstLine);
        }

        private static bool ShouldWrapPrelim(
            bool optionsAutoWrapEnabled,
            int optionsAvoidWrappingBeforeLine,
            INormalizedTextChangeCollection changes,
            ITextSnapshot snapshot,
            ITextBuffer buffer,
            CaretPosition caretPosition,
            out SnapshotPoint caretPoint,
            out int firstLine)
        {
            var wrap = typeof(AutoWrapper).GetMethod(
                "ShouldWrapPrelim",
                BindingFlags.NonPublic | BindingFlags.Static);

            caretPoint = default(SnapshotPoint);
            firstLine = -1;

            var parameters = new object[] {
                optionsAutoWrapEnabled,
                optionsAvoidWrappingBeforeLine,
                changes,
                snapshot,
                buffer,
                caretPosition,
                caretPoint,
                firstLine,
            };

            var result = wrap.Invoke(null, parameters);

            caretPoint = (SnapshotPoint)parameters[6];
            firstLine = (int)parameters[7];

            return (bool)result;
        }

        private static bool ShouldWrap(
            GeneralOptions options,
            ITextChange change,
            ITextSnapshotLine line,
            LineCommentInfo info,
            out int commentWrapLength)
        {
            var wrap = typeof(AutoWrapper).GetMethod(
                "ShouldWrap",
                BindingFlags.NonPublic | BindingFlags.Static);

            commentWrapLength = -1;

            var parameters = new object[] {
                options,
                change,
                line,
                info,
                commentWrapLength,
            };

            var result = wrap.Invoke(null, parameters);

            commentWrapLength = (int)parameters[4];

            return (bool)result;
        }

        private static List<string> Rewrap(string text, int wrapLength, int caretContentOffset, out int caretLineOffset, out int caretPositionOffset)
        {
            var wrap = typeof(AutoWrapper).GetMethod(
                "RewrapCommentParagraph",
                BindingFlags.NonPublic | BindingFlags.Static);

            caretLineOffset = 0;
            caretPositionOffset = 0;

            var parameters = new object[] {
                new StringBuilder(text),
                wrapLength,
                caretContentOffset,
                caretLineOffset,
                caretPositionOffset,
            };

            var result = wrap.Invoke(null, parameters);
            caretLineOffset = (int)parameters[3];
            caretPositionOffset = (int)parameters[4];

            return (List<string>)result;
        }

        public static void AssertSequence<T>(IList<T> actual, params T[] expected)
        {
            Assert.AreEqual(expected.Length, actual.Count, "sequence length");

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(
                    expected[i],
                    actual[i],
                    string.Format("got {0}, expected {1}, on item {2}", actual[i], expected[i], i));
            }
        }
    }
}
