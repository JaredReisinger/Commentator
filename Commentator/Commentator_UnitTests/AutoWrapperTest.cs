using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Classification.Fakes;
using Microsoft.VisualStudio.Text.Fakes;
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
        public void AutoWrapper_Wrap_BAD_OverlongLinesClip()
        {
            AssertSequence(Rewrap("123456", 5), "12345", "6");
        }

        [TestMethod]
        public void AutoWrapper_Wrap_TODO_OverlongLinesDontClip()
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
