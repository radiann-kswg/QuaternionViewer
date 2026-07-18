using QuaternionViewer.Chapters;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace QuaternionViewer.UI
{
    /// <summary>
    /// 解説バー (section-guide §3) ―― 画面下部のステップ送り UI。
    /// 章送り (‹‹ ››) / 章ラベル / ビート進捗 (●=核心・○=発展、クリックでジャンプ) / Prev/Next /
    /// 〔直感〕常時表示 / 〔数理〕折りたたみ (MATH トグル)。
    /// </summary>
    /// <remarks>
    /// 章の一覧と切替は <see cref="ChapterNavigator"/> が持ち、本 UI はその表示と操作面。
    /// navigator 未設定時は <see cref="chapter"/> 単章で動く。
    /// キーボード (Play 中): ← → = ビート送り / ↑ ↓ = 章送り (仕様書 7章)。
    /// 話者ノート窓・自由探索トグルはフック増強フェーズで追加する。
    /// </remarks>
    [ExecuteAlways]
    [RequireComponent(typeof(UIDocument))]
    public class GuideBarUI : MonoBehaviour
    {
        [Tooltip("章切替 (未設定なら chapter 単章)")]
        public ChapterNavigator navigator;

        [Tooltip("navigator 未設定時に使う単章")]
        public ChapterBase chapter;

        private UIDocument _doc;
        private Label _chapterLabel;
        private Label _countLabel;
        private Label _intuition;
        private Label _mathLabel;
        private VisualElement _mathBox;
        private VisualElement _dots;
        private Button _bMath;
        private bool _mathOpen;
        private int _seenRevision = -1;
        private ChapterBase _seenChapter;

        private ChapterBase Active =>
            navigator != null && navigator.Current != null ? navigator.Current : chapter;

        private void OnEnable()
        {
            _doc = GetComponent<UIDocument>();
            Build();
        }

        private void OnDisable()
        {
            if (_doc != null && _doc.rootVisualElement != null) _doc.rootVisualElement.Clear();
            _seenRevision = -1;
            _seenChapter = null;
        }

        private void Build()
        {
            VisualElement root = _doc != null ? _doc.rootVisualElement : null;
            if (root == null) return;
            root.Clear();

            var panel = new VisualElement();
            panel.style.position = Position.Absolute;
            panel.style.left = 12f;
            panel.style.right = 12f;
            panel.style.bottom = 10f;
            panel.style.paddingTop = 7f;
            panel.style.paddingBottom = 6f;
            panel.style.paddingLeft = 10f;
            panel.style.paddingRight = 10f;
            HudStyle.Frame(panel);
            root.Add(panel);

            // ── 見出し行: 章送り + 章ラベル + 進捗 + ビート送り + MATH ──────
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.marginBottom = 4f;
            panel.Add(header);

            var bPrevCh = new Button(() => { if (navigator != null) navigator.PrevChapter(); }) { text = "<<" };
            HudStyle.Button(bPrevCh, 22f);
            header.Add(bPrevCh);

            _chapterLabel = new Label("");
            _chapterLabel.style.color = HudStyle.Accent;
            _chapterLabel.style.fontSize = 14;
            _chapterLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _chapterLabel.style.marginLeft = 4f;
            _chapterLabel.style.marginRight = 4f;
            header.Add(_chapterLabel);

            var bNextCh = new Button(() => { if (navigator != null) navigator.NextChapter(); }) { text = ">>" };
            HudStyle.Button(bNextCh, 22f);
            header.Add(bNextCh);

            var spacer = new VisualElement();
            spacer.style.flexGrow = 1f;
            header.Add(spacer);

            _dots = new VisualElement();
            _dots.style.flexDirection = FlexDirection.Row;
            _dots.style.alignItems = Align.Center;
            header.Add(_dots);

            _countLabel = new Label("");
            HudStyle.ApplyLatinFont(_countLabel);
            _countLabel.style.color = HudStyle.TextDim;
            _countLabel.style.fontSize = 12;
            _countLabel.style.marginLeft = 6f;
            _countLabel.style.marginRight = 6f;
            header.Add(_countLabel);

            var bPrev = new Button(() => Active?.Prev()) { text = "<" };
            var bNext = new Button(() => Active?.Next()) { text = ">" };
            _bMath = new Button(ToggleMath) { text = "MATH" };
            foreach (Button b in new[] { bPrev, bNext, _bMath })
            {
                HudStyle.Button(b, 22f);
                header.Add(b);
            }

            // ── 〔数理〕折りたたみ (既定は畳む) ─────────────────────────
            _mathBox = new VisualElement();
            _mathBox.style.display = DisplayStyle.None;
            _mathBox.style.backgroundColor = HudStyle.Track;
            _mathBox.style.paddingTop = 5f;
            _mathBox.style.paddingBottom = 5f;
            _mathBox.style.paddingLeft = 8f;
            _mathBox.style.paddingRight = 8f;
            _mathBox.style.marginBottom = 4f;
            _mathBox.style.borderLeftWidth = 2f;
            _mathBox.style.borderLeftColor = HudStyle.Accent;
            panel.Add(_mathBox);

            _mathLabel = new Label("");
            _mathLabel.style.color = HudStyle.TextMain;
            _mathLabel.style.fontSize = 13;
            _mathLabel.style.whiteSpace = WhiteSpace.Normal;
            _mathBox.Add(_mathLabel);

            // ── 〔直感〕常時表示 ─────────────────────────────────────
            _intuition = new Label("");
            _intuition.style.color = HudStyle.TextMain;
            _intuition.style.fontSize = 14;
            _intuition.style.whiteSpace = WhiteSpace.Normal;
            panel.Add(_intuition);

            // ── 自由探索の操作ヒント (ArcballController, 仕様書 7章) ────
            var hint = new Label("L-DRAG ROTATE / R-DRAG ORBIT / WHEEL ZOOM / [R] RESET");
            HudStyle.ApplyLatinFont(hint);
            hint.style.color = HudStyle.TextDim;
            hint.style.fontSize = 10;
            hint.style.marginTop = 3f;
            hint.style.alignSelf = Align.FlexEnd;
            panel.Add(hint);

            _seenRevision = -1;
            _seenChapter = null;
        }

        private void ToggleMath()
        {
            _mathOpen = !_mathOpen;
            if (_mathBox != null) _mathBox.style.display = _mathOpen ? DisplayStyle.Flex : DisplayStyle.None;
            if (_bMath != null) HudStyle.SetButtonActive(_bMath, _mathOpen);
        }

        private void Update()
        {
            ChapterBase active = Active;
            if (active == null || _chapterLabel == null) return;

            // 講義・自習共用のキーボード送り。← → = ビート / ↑ ↓ = 章 (Play モードのみ)
            if (Application.isPlaying && Keyboard.current != null)
            {
                Keyboard kb = Keyboard.current;
                if (kb.rightArrowKey.wasPressedThisFrame) active.Next();
                if (kb.leftArrowKey.wasPressedThisFrame) active.Prev();
                if (navigator != null)
                {
                    if (kb.downArrowKey.wasPressedThisFrame) navigator.NextChapter();
                    if (kb.upArrowKey.wasPressedThisFrame) navigator.PrevChapter();
                }
            }

            active = Active; // 章送りで替わった可能性
            if (active != _seenChapter || active.Revision != _seenRevision) Refresh(active);
        }

        private void Refresh(ChapterBase active)
        {
            _seenChapter = active;
            _seenRevision = active.Revision;

            GuideBeat beat = active.Current;
            int count = active.Beats.Count;
            int index = active.CurrentIndex;

            _chapterLabel.text = count > 0 && beat != null
                ? $"{active.ChapterTitle} ― {beat.title}"
                : active.ChapterTitle;
            _countLabel.text = count > 0 ? $"{index + 1} / {count}" : "- / -";
            _intuition.text = beat != null ? beat.intuition : "(台本なし)";
            _mathLabel.text = beat != null && beat.math.Length > 0 ? beat.math : "(このビートに数理層はない)";

            _dots.Clear();
            for (int i = 0; i < count; i++)
            {
                int target = i;
                var dot = new Button(() => active.JumpTo(target))
                {
                    text = active.Beats[i].core ? "●" : "○",
                };
                dot.style.backgroundColor = Color.clear;
                dot.style.borderTopWidth = 0f;
                dot.style.borderBottomWidth = 0f;
                dot.style.borderLeftWidth = 0f;
                dot.style.borderRightWidth = 0f;
                dot.style.marginLeft = 1f;
                dot.style.marginRight = 1f;
                dot.style.paddingLeft = 2f;
                dot.style.paddingRight = 2f;
                dot.style.paddingTop = 0f;
                dot.style.paddingBottom = 0f;
                dot.style.fontSize = 12;
                dot.style.color = i == index ? HudStyle.Accent : HudStyle.TextDim;
                _dots.Add(dot);
            }
        }
    }
}
