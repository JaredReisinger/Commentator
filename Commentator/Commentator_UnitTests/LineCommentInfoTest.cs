﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Classification.Fakes;
using Microsoft.VisualStudio.Text.Fakes;
using Spudnoggin.Commentator.AutoWrap;

namespace Commentator_UnitTests
{
    [TestClass]
    public class LineCommentInfoTest
    {
        StubIClassificationType commentClass;
        StubIClassifier wholeLineCommentClassifier;
        SimpleEditorOptions defaultOptions;

        public LineCommentInfoTest()
        {
            this.commentClass = new StubIClassificationType();
            this.commentClass.ClassificationGet = () => "comment";
            this.commentClass.IsOfTypeString = s => string.Equals(s, "comment");

            this.wholeLineCommentClassifier = new StubIClassifier();
            this.wholeLineCommentClassifier.GetClassificationSpansSnapshotSpan = s =>
            {
                // Find the "//"... Note: doesn't look for "''" (VB) or "/*"
                // (multi-line).  That might need to change when we start
                // supporting more comment styles.
                var markerStart = s.GetText().IndexOf("//");
                Assert.IsTrue(markerStart >= 0);
                var commentSpan = new SnapshotSpan(s.Start + markerStart, s.End);
                var span = new ClassificationSpan(commentSpan, this.commentClass);

                var list = new List<ClassificationSpan>();
                list.Add(span);
                return list;
            };

            this.defaultOptions = new SimpleEditorOptions();
            this.defaultOptions.SetOptionValue("Tabs/ConvertTabsToSpaces", true);
            this.defaultOptions.SetOptionValue("Tabs/TabSize", 4);
        }

        [TestMethod]
        public void LineCommentInfo_NoClassificationsMeansNullInfo()
        {
            var line = new StubITextSnapshotLine();
            var classifier = new StubIClassifier();
            var info = LineCommentInfo.FromLine(line, this.defaultOptions, classifier);
            Assert.IsNull(info);
        }

        [TestMethod]
        public void LineCommentInfo_SimpleSingleLineComment()
        {
            var snapshot = new SimpleSnapshot(
                "// this is a comment");

            var info = LineCommentInfo.FromLine(
                snapshot.GetLineFromLineNumber(0),
                this.defaultOptions,
                this.wholeLineCommentClassifier);

            Assert.IsNotNull(info);
            Assert.IsTrue(info.CommentOnly);
            Assert.AreEqual(info.Line.Extent, info.CommentSpan);
            Assert.AreEqual(0, info.MarkerSpan.Start.Position);
            Assert.AreEqual(2, info.MarkerSpan.End.Position);
            Assert.AreEqual(2, info.MarkerSpan.Length);
            Assert.AreEqual(0, info.MarkerColumnStart);
            Assert.AreEqual(CommentStyle.SingleLine, info.Style);
            Assert.AreEqual(3, info.ContentSpan.Start.Position);
            Assert.AreEqual(3, info.ContentColumnStart);
            Assert.AreEqual(info.Line.End.Position, info.ContentSpan.End.Position);
        }

        [TestMethod]
        public void LineCommentInfo_LeadingTabIsFourSpaces()
        {
            var snapshot = new SimpleSnapshot(
                "\t// this is a comment");

            var info = LineCommentInfo.FromLine(
                snapshot.GetLineFromLineNumber(0),
                this.defaultOptions,
                this.wholeLineCommentClassifier);

            Assert.IsNotNull(info);
            Assert.IsTrue(info.CommentOnly);
            Assert.AreEqual(1, info.CommentSpan.Start.Position);
            Assert.AreEqual(1, info.MarkerSpan.Start.Position);
            Assert.AreEqual(4, info.MarkerColumnStart);
            Assert.AreEqual(4, info.ContentSpan.Start.Position);
            Assert.AreEqual(7, info.ContentColumnStart);
        }

        [TestMethod]
        public void LineCommentInfo_LeadingEightSpaceTabIsEightSpaces()
        {
            var snapshot = new SimpleSnapshot(
                "\t// this is a comment");

            var eightSpaceTab = new SimpleEditorOptions();
            eightSpaceTab.Parent = this.defaultOptions;
            eightSpaceTab.SetOptionValue("Tabs/TabSize", 8);

            var info = LineCommentInfo.FromLine(
                snapshot.GetLineFromLineNumber(0),
                eightSpaceTab,
                this.wholeLineCommentClassifier);

            Assert.IsNotNull(info);
            Assert.IsTrue(info.CommentOnly);
            Assert.AreEqual(1, info.CommentSpan.Start.Position);
            Assert.AreEqual(1, info.MarkerSpan.Start.Position);
            Assert.AreEqual(8, info.MarkerColumnStart);
            Assert.AreEqual(4, info.ContentSpan.Start.Position);
            Assert.AreEqual(11, info.ContentColumnStart);
        }

        [TestMethod]
        public void LineCommentInfo_SimpleSingleLineCommentsMatch()
        {
            var snapshot = new SimpleSnapshot(
                "// this is a comment",
                "// that continues to a second line");

            var info0 = LineCommentInfo.FromLine(
                snapshot.GetLineFromLineNumber(0),
                this.defaultOptions,
                this.wholeLineCommentClassifier);
            
            var info1 = LineCommentInfo.FromLine(
                snapshot.GetLineFromLineNumber(1),
                this.defaultOptions,
                this.wholeLineCommentClassifier);

            Assert.IsNotNull(info0);
            Assert.IsNotNull(info1);
            Assert.IsTrue(info0.Matches(info1));
        }

        [TestMethod]
        public void LineCommentInfo_MixedWhitespaceSingleLineCommentsMatch()
        {
            var snapshot = new SimpleSnapshot(
                "    // this is a comment",
                "\t// that continues to a second line");

            var info0 = LineCommentInfo.FromLine(
                snapshot.GetLineFromLineNumber(0),
                this.defaultOptions,
                this.wholeLineCommentClassifier);

            var info1 = LineCommentInfo.FromLine(
                snapshot.GetLineFromLineNumber(1),
                this.defaultOptions,
                this.wholeLineCommentClassifier);

            Assert.IsNotNull(info0);
            Assert.IsNotNull(info1);
            Assert.IsTrue(info0.Matches(info1));
        }
    }
}
