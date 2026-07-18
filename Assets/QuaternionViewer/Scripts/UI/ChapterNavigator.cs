using System.Collections.Generic;
using QuaternionViewer.Chapters;
using UnityEngine;

namespace QuaternionViewer.UI
{
    /// <summary>
    /// 章切替 (仕様書 6.1 の必須 UI 機構)。章 (<see cref="ChapterBase"/>) の一覧を保持し、
    /// 切替時に <see cref="GuideController"/> の購読を張り替える。ボタン描画は GuideBarUI が担う。
    /// </summary>
    [ExecuteAlways]
    public class ChapterNavigator : MonoBehaviour
    {
        [Tooltip("章の並び (Ch.1 → Ch.6)")]
        public List<ChapterBase> chapters = new List<ChapterBase>();

        public GuideController controller;

        [SerializeField]
        [Tooltip("現在の章 index")]
        private int index;

        public int CurrentIndex => index;

        public int Count => chapters.Count;

        public ChapterBase Current =>
            chapters.Count > 0 ? chapters[Mathf.Clamp(index, 0, chapters.Count - 1)] : null;

        private void OnEnable() => SwitchTo(index, true);

        public void NextChapter() => SwitchTo(index + 1, false);

        public void PrevChapter() => SwitchTo(index - 1, false);

        /// <summary>指定章へ切り替える (端は周回 ―― Ch.6 の次は Ch.1)。force で同章でも再適用する。</summary>
        public void SwitchTo(int target, bool force)
        {
            if (chapters.Count == 0) return;
            int wrapped = ((target % chapters.Count) + chapters.Count) % chapters.Count;
            if (!force && wrapped == index) return;
            index = wrapped;
            if (controller != null) controller.SetChapter(Current);
        }
    }
}
