using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuaternionViewer.Chapters
{
    /// <summary>
    /// 章 = 順序付きビート列 (仕様書 6.1 / section-guide §2)。
    /// 台本 TextAsset を読み、現在ビートと Next/Prev/JumpTo を提供する。
    /// 儀への適用は <see cref="GuideController"/> が <see cref="BeatChanged"/> を購読して行う。
    /// </summary>
    [ExecuteAlways]
    public abstract class ChapterBase : MonoBehaviour
    {
        [Tooltip("台本 Markdown (TextAsset)。未指定なら DefaultScriptResource を Resources から読む")]
        public TextAsset scriptAsset;

        /// <summary>Resources 内の既定台本パス (拡張子なし)。例: "Guide/ch1"。</summary>
        protected abstract string DefaultScriptResource { get; }

        private readonly List<GuideBeat> _beats = new List<GuideBeat>();
        private string _chapterTitle = "";
        private int _index;
        private int _revision;

        public string ChapterTitle => _chapterTitle;

        public IReadOnlyList<GuideBeat> Beats => _beats;

        public int CurrentIndex => _index;

        public GuideBeat Current => _beats.Count > 0 ? _beats[Mathf.Clamp(_index, 0, _beats.Count - 1)] : null;

        /// <summary>台本の再読込・ビート移動のたびに増える改訂番号 (UI のポーリング用)。</summary>
        public int Revision => _revision;

        /// <summary>ビート移動時に現在ビートを渡す。適用器 (GuideController) が購読する。</summary>
        public event Action<GuideBeat> BeatChanged;

        protected virtual void OnEnable()
        {
            Reload();
        }

        /// <summary>台本を読み直す。台本 Markdown を編集した後にも呼べる。</summary>
        public void Reload()
        {
            _beats.Clear();
            TextAsset asset = scriptAsset != null ? scriptAsset : Resources.Load<TextAsset>(DefaultScriptResource);
            if (asset == null)
            {
                Debug.LogWarning($"[{name}] 台本が見つからない: Resources/{DefaultScriptResource}", this);
                _revision++;
                return;
            }

            _beats.AddRange(GuideScript.Parse(asset.text, out _chapterTitle, out List<string> warnings));
            foreach (string w in warnings)
            {
                Debug.LogWarning($"[{name}] 台本: {w}", this);
            }

            _index = Mathf.Clamp(_index, 0, Mathf.Max(0, _beats.Count - 1));
            _revision++;
        }

        public void Next() => JumpTo(_index + 1);

        public void Prev() => JumpTo(_index - 1);

        /// <summary>指定ビートへ移動する (範囲外はクランプ・周回しない)。進捗ドットからの任意ジャンプにも使う。</summary>
        public void JumpTo(int index)
        {
            if (_beats.Count == 0) return;
            int clamped = Mathf.Clamp(index, 0, _beats.Count - 1);
            if (clamped == _index) return;
            _index = clamped;
            _revision++;
            BeatChanged?.Invoke(Current);
        }

        /// <summary>移動なしで現在ビートを改めて適用させる (章の入場時・自由探索からの復帰用)。</summary>
        public void Reapply()
        {
            if (Current != null) BeatChanged?.Invoke(Current);
        }
    }
}
