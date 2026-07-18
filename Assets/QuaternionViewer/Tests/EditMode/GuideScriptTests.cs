using System.Collections.Generic;
using NUnit.Framework;
using QuaternionViewer.Chapters;
using QuaternionViewer.Visualization;
using UnityEngine;

namespace QuaternionViewer.Tests
{
    /// <summary>
    /// 台本 Markdown パーサ (GuideScript) の検証。
    /// 台本の書式規則 (GuideScript の remarks) を主張ではなくテストで担保する (仕様書 6.3 の方針)。
    /// </summary>
    public class GuideScriptTests
    {
        private const string Fixture = @"# Ch.T テスト章

> 前書きは台本外として無視される。

## ◆ 一つ目
@posture 1 2 0.5 120
@demos Mirrors|Graph
@ball RotationVector
@camera SpaceBall
@highlight HalfAngle
@action flipSign
@action clearTrail
@focus pole+ pole-

### 直感
直感1行目
2行目

### 数理
数理本文

### 話者ノート
ノート

## ○ 二つ目
@unknown hoge
@euler 90 30 10

### 直感
二つ目の直感

### 謎ノ節
無視される本文
";

        private static List<GuideBeat> ParseFixture(out string title, out List<string> warnings)
        {
            return GuideScript.Parse(Fixture, out title, out warnings);
        }

        [Test]
        public void Parse_ReturnsChapterTitleAndBeats()
        {
            List<GuideBeat> beats = ParseFixture(out string title, out _);
            Assert.That(title, Is.EqualTo("Ch.T テスト章"));
            Assert.That(beats.Count, Is.EqualTo(2));
            Assert.That(beats[0].title, Is.EqualTo("一つ目"));
            Assert.That(beats[1].title, Is.EqualTo("二つ目"));
        }

        [Test]
        public void Parse_ReadsCoreMarkers()
        {
            List<GuideBeat> beats = ParseFixture(out _, out _);
            Assert.That(beats[0].core, Is.True, "◆ は核心");
            Assert.That(beats[1].core, Is.False, "○ は発展");
        }

        [Test]
        public void Parse_ReadsPostureAxisAndAngle()
        {
            GuideBeat beat = ParseFixture(out _, out _)[0];
            Assert.That(beat.setPosture, Is.True);
            Assert.That(beat.axis.x, Is.EqualTo(1f).Within(1e-6f));
            Assert.That(beat.axis.y, Is.EqualTo(2f).Within(1e-6f));
            Assert.That(beat.axis.z, Is.EqualTo(0.5f).Within(1e-6f));
            Assert.That(beat.angleDeg, Is.EqualTo(120f).Within(1e-6f));
        }

        [Test]
        public void Parse_ReadsCombinedDemoFlags()
        {
            GuideBeat beat = ParseFixture(out _, out _)[0];
            Assert.That(beat.setDemos, Is.True);
            Assert.That(beat.demos, Is.EqualTo(DemoFlags.Mirrors | DemoFlags.Graph));
        }

        [Test]
        public void Parse_ReadsBallCameraAndHighlight()
        {
            GuideBeat beat = ParseFixture(out _, out _)[0];
            Assert.That(beat.setBallModel, Is.True);
            Assert.That(beat.ballModel, Is.EqualTo(BallModel.RotationVector));
            Assert.That(beat.setCamera, Is.True);
            Assert.That(beat.camera, Is.EqualTo(CameraFraming.SpaceBall));
            Assert.That(beat.setHighlight, Is.True);
            Assert.That(beat.highlight, Is.EqualTo(ReadoutHighlight.HalfAngle));
        }

        [Test]
        public void Parse_ReadsActionsInWrittenOrder()
        {
            GuideBeat beat = ParseFixture(out _, out _)[0];
            Assert.That(beat.actions, Is.EqualTo(new[] { "flipSign", "clearTrail" }));
        }

        [Test]
        public void Parse_ReadsEulerPosture()
        {
            GuideBeat second = ParseFixture(out _, out _)[1];
            Assert.That(second.setEulerPosture, Is.True);
            Assert.That(second.eulerDeg.x, Is.EqualTo(90f).Within(1e-6f));
            Assert.That(second.eulerDeg.y, Is.EqualTo(30f).Within(1e-6f));
            Assert.That(second.eulerDeg.z, Is.EqualTo(10f).Within(1e-6f));
        }

        [Test]
        public void Parse_ReadsFocusTargets()
        {
            GuideBeat beat = ParseFixture(out _, out _)[0];
            Assert.That(beat.focus, Is.EqualTo(new[] { "pole+", "pole-" }));
        }

        [Test]
        public void Parse_FocusIsTransient_SecondBeatHasNone()
        {
            GuideBeat second = ParseFixture(out _, out _)[1];
            Assert.That(second.focus, Is.Empty, "@focus は指差し ―― 宣言の無いビートでは空 (自動消灯)");
        }

        [Test]
        public void Parse_JoinsBodyLinesWithinSection()
        {
            GuideBeat beat = ParseFixture(out _, out _)[0];
            Assert.That(beat.intuition, Is.EqualTo("直感1行目\n2行目"));
            Assert.That(beat.math, Is.EqualTo("数理本文"));
            Assert.That(beat.presenterNote, Is.EqualTo("ノート"));
        }

        [Test]
        public void Parse_DirectivesDoNotLeakIntoNextBeat()
        {
            GuideBeat second = ParseFixture(out _, out _)[1];
            Assert.That(second.setPosture, Is.False);
            Assert.That(second.setDemos, Is.False);
            Assert.That(second.setBallModel, Is.False);
            Assert.That(second.actions, Is.Empty);
            Assert.That(second.intuition, Is.EqualTo("二つ目の直感"));
        }

        [Test]
        public void Parse_WarnsOnUnknownDirectiveAndSection()
        {
            ParseFixture(out _, out List<string> warnings);
            Assert.That(warnings.Count, Is.EqualTo(2));
            Assert.That(warnings[0], Does.Contain("@unknown"));
            Assert.That(warnings[1], Does.Contain("謎ノ節"));
        }

        [Test]
        public void Parse_EmptyTextYieldsNoBeats()
        {
            List<GuideBeat> beats = GuideScript.Parse("", out string title, out List<string> warnings);
            Assert.That(beats, Is.Empty);
            Assert.That(title, Is.Empty);
            Assert.That(warnings, Is.Empty);
        }
    }
}
