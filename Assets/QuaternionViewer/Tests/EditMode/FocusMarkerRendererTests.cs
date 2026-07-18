using System;
using System.Collections.Generic;
using NUnit.Framework;
using QuaternionViewer.Visualization;

namespace QuaternionViewer.Tests
{
    /// <summary>フォーカスマーカーの既定エイリアス表の健全性検証。</summary>
    public class FocusMarkerRendererTests
    {
        [Test]
        public void DefaultAliases_NamesAreUniqueCaseInsensitive()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach ((string name, string objectName, float radius) a in FocusMarkerRenderer.DefaultAliases)
            {
                Assert.That(seen.Add(a.name), Is.True, $"重複エイリアス: {a.name}");
                Assert.That(a.objectName, Is.Not.Empty);
                Assert.That(a.radius, Is.GreaterThan(0f));
            }
        }

        [Test]
        public void DefaultAliases_CoverChapterOneScript()
        {
            // Resources/Guide/ch1.md の @focus が使う名前は既定表に載っていること
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach ((string name, string, float) a in FocusMarkerRenderer.DefaultAliases) names.Add(a.Item1);
            foreach (string required in new[] { "pole+", "pole-", "pin", "pinImage", "mirrors" })
            {
                Assert.That(names.Contains(required), Is.True, $"未登録: {required}");
            }
        }
    }
}
