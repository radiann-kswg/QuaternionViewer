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
            panel.style.paddingTop = 6f;
            panel.style.paddingBottom = 6f;
            panel.style.paddingLeft = 6f;
            panel.style.paddingRight = 6f;
            HudStyle.Frame(panel);
            root.Add(panel);

            var title = new Label("CORE MODEL");
            HudStyle.Header(title);
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
                HudStyle.Button(b);
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
                HudStyle.SetButtonActive(_buttons[i], i == switcher.ActiveIndex);
            }
        }
    }
}
