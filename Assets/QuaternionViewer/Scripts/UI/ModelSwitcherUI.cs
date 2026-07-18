using QuaternionViewer.Visualization;
using UnityEngine;
using UnityEngine.UIElements;

namespace QuaternionViewer.UI
{
    /// <summary>
    /// 内核の標本モデル切替ボタン列 (画面右上)。<see cref="CoreModelSwitcher"/> の操作面。
    /// </summary>
    /// <remarks>
    /// <see cref="QuaternionReadout"/> と同じく UXML/USS を使わず C# だけで組む。
    /// 情報パネル (左上) と領域が重ならないよう右上へ置く。
    /// </remarks>
    [ExecuteAlways]
    [RequireComponent(typeof(UIDocument))]
    public class ModelSwitcherUI : MonoBehaviour
    {
        public CoreModelSwitcher switcher;

        private static readonly Color PanelBg = new Color(0.05f, 0.06f, 0.09f, 0.88f);
        private static readonly Color TextMain = new Color(0.88f, 0.89f, 0.92f);
        private static readonly Color TextDim = new Color(0.55f, 0.57f, 0.63f);
        private static readonly Color ButtonBg = new Color(0.18f, 0.19f, 0.24f);
        private static readonly Color ButtonActive = new Color(0.25f, 0.42f, 0.65f);

        private UIDocument _doc;
        private Button[] _buttons;
        private int _builtCount = -1;

        private void OnEnable()
        {
            _doc = GetComponent<UIDocument>();
            Build();
        }

        private void OnDisable()
        {
            if (_doc != null && _doc.rootVisualElement != null) _doc.rootVisualElement.Clear();
            _buttons = null;
            _builtCount = -1;
        }

        private void Build()
        {
            VisualElement root = _doc != null ? _doc.rootVisualElement : null;
            if (root == null) return;
            root.Clear();

            int count = switcher != null ? switcher.Count : 0;
            _builtCount = count;
            _buttons = new Button[count];

            var panel = new VisualElement();
            panel.style.position = Position.Absolute;
            panel.style.top = 12f;
            panel.style.right = 12f;
            panel.style.backgroundColor = PanelBg;
            panel.style.paddingTop = 6f;
            panel.style.paddingBottom = 6f;
            panel.style.paddingLeft = 8f;
            panel.style.paddingRight = 8f;
            panel.style.borderTopLeftRadius = 6f;
            panel.style.borderTopRightRadius = 6f;
            panel.style.borderBottomLeftRadius = 6f;
            panel.style.borderBottomRightRadius = 6f;
            root.Add(panel);

            var title = new Label("CORE MODEL");
            title.style.color = TextDim;
            title.style.fontSize = 10;
            title.style.marginBottom = 3f;
            title.style.unityTextAlign = TextAnchor.MiddleRight;
            panel.Add(title);

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            panel.Add(row);

            for (int i = 0; i < count; i++)
            {
                int index = i; // クロージャ用
                var b = new Button(() =>
                {
                    if (switcher != null) switcher.ActiveIndex = index;
                });
                b.text = switcher.GetModelName(i);
                b.style.fontSize = 11;
                b.style.color = TextMain;
                b.style.backgroundColor = ButtonBg;
                b.style.marginLeft = 2f;
                b.style.marginRight = 2f;
                b.style.paddingLeft = 6f;
                b.style.paddingRight = 6f;
                row.Add(b);
                _buttons[i] = b;
            }
        }

        private void Update()
        {
            if (switcher == null) return;
            if (_builtCount != switcher.Count) Build();
            if (_buttons == null) return;

            for (int i = 0; i < _buttons.Length; i++)
            {
                if (_buttons[i] == null) continue;
                _buttons[i].style.backgroundColor =
                    i == switcher.ActiveIndex ? ButtonActive : ButtonBg;
            }
        }
    }
}
