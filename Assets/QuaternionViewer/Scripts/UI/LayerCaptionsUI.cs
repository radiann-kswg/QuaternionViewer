using QuaternionViewer.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace QuaternionViewer.UI
{
    /// <summary>
    /// 三層構造の吹き出しキャプション (内核 / 中殻 / 外殻)。
    /// 各層の実体へ引き出し線を伸ばし、画面下段の枠付きボックスで層情報を示す。
    /// </summary>
    /// <remarks>
    /// 中殻は「実空間の方向球面 S² ―― 回転軸 n の住処」、外殻は「回転空間 RP³ の模型
    /// ―― 四元数 q そのものの住処」であり、両者は別の数学的空間 (仕様書 4.2, 4.3)。
    /// 外殻は球の内部が表示面 (軌跡・対蹠ワープ・三体比較) のため同心にせず脇へ置く ――
    /// この吹き出しはその役割分担を画面上で自明にするためのもの。
    /// </remarks>
    [ExecuteAlways]
    [RequireComponent(typeof(UIDocument))]
    public class LayerCaptionsUI : MonoBehaviour
    {
        [Tooltip("内核 (Core) のTransform")]
        public Transform core;

        [Tooltip("中殻 (Globe) のTransform")]
        public Transform globe;

        [Tooltip("外殻 (RotationSpaceBall) のTransform")]
        public Transform ball;

        public float coreRadius = 0.55f;
        public float globeRadius = 1.5f;
        public float ballRadius = 1f;

        [Tooltip("吹き出しボックスの中心X (画面幅比)")]
        public float[] boxCentersRatio = { 0.175f, 0.46f, 0.79f };

        [Tooltip("吹き出しボックス上端の画面下からの距離")]
        public float boxBottomOffset = 86f;

        private UIDocument _doc;
        private LeaderLineLayer _lines;
        private readonly VisualElement[] _boxes = new VisualElement[3];

        /// <summary>引き出し線の描画面 (Painter2D)。</summary>
        private class LeaderLineLayer : VisualElement
        {
            public readonly Vector2[] Anchors = new Vector2[3];
            public readonly Vector2[] Corners = new Vector2[3];
            public bool[] Visible = new bool[3];

            public LeaderLineLayer()
            {
                generateVisualContent += Draw;
                pickingMode = PickingMode.Ignore;
                style.position = Position.Absolute;
                style.left = 0f;
                style.top = 0f;
                style.right = 0f;
                style.bottom = 0f;
            }

            private void Draw(MeshGenerationContext ctx)
            {
                var p = ctx.painter2D;
                p.strokeColor = HudStyle.AccentDim;
                p.lineWidth = 1f;
                for (int i = 0; i < 3; i++)
                {
                    if (!Visible[i]) continue;
                    p.BeginPath();
                    p.MoveTo(Anchors[i]);
                    p.LineTo(Corners[i]);
                    p.Stroke();

                    // アンカー側の点
                    p.BeginPath();
                    p.Arc(Anchors[i], 2.5f, 0f, Angle.Degrees(360f));
                    p.Stroke();
                }
            }
        }

        private void OnEnable()
        {
            _doc = GetComponent<UIDocument>();
            Build();
        }

        private void OnDisable()
        {
            if (_doc != null && _doc.rootVisualElement != null) _doc.rootVisualElement.Clear();
            _lines = null;
        }

        private VisualElement MakeCallout(VisualElement root, string main, string sub)
        {
            var box = new VisualElement();
            box.style.position = Position.Absolute;
            box.style.paddingTop = 5f;
            box.style.paddingBottom = 5f;
            box.style.paddingLeft = 8f;
            box.style.paddingRight = 8f;
            box.style.maxWidth = 320f;
            // left座標を中心に据えるため、自身の幅の -50% だけ平行移動する
            box.style.translate = new Translate(Length.Percent(-50f), 0f);
            box.pickingMode = PickingMode.Ignore;
            HudStyle.Frame(box);

            var l1 = new Label(main);
            l1.style.color = HudStyle.Accent;
            l1.style.fontSize = 16;
            l1.style.letterSpacing = 1.5f;
            l1.style.unityFontStyleAndWeight = FontStyle.Bold;
            l1.style.unityTextAlign = TextAnchor.MiddleCenter;
            l1.pickingMode = PickingMode.Ignore;
            box.Add(l1);

            var l2 = new Label(sub);
            l2.style.color = HudStyle.TextDim;
            l2.style.fontSize = 12;
            l2.style.unityTextAlign = TextAnchor.MiddleCenter;
            l2.style.whiteSpace = WhiteSpace.Normal;
            l2.pickingMode = PickingMode.Ignore;
            box.Add(l2);

            root.Add(box);
            return box;
        }

        private void Build()
        {
            VisualElement root = _doc != null ? _doc.rootVisualElement : null;
            if (root == null) return;
            root.Clear();
            HudStyle.ApplyFont(root);

            _lines = new LeaderLineLayer();
            root.Add(_lines);

            _boxes[0] = MakeCallout(root, "内核 ── 回転された結果",
                "標本の姿勢が q の作用そのもの (出目で読む)");
            _boxes[1] = MakeCallout(root, "中殻 ── S²球儀",
                "回転軸 n と角 θ の住処 (地球儀の実体)");
            _boxes[2] = MakeCallout(root, "外殻 ── 回転空間ボール",
                "q が点として住む空間。中心=無回転 / 表面=180° (RP³)");
        }

        private void Update()
        {
            if (_lines == null || _doc == null || _doc.rootVisualElement == null) return;
            var cam = Camera.main;
            var rootVe = _doc.rootVisualElement;
            var panel = rootVe.panel;
            if (cam == null || panel == null) return;

            float pw = rootVe.resolvedStyle.width;
            float ph = rootVe.resolvedStyle.height;
            if (float.IsNaN(pw) || pw < 10f) return;

            float boxTop = ph - boxBottomOffset;

            // 引き出し線のアンカー: 内核=標本の下端 / 中殻=球儀の下縁 / 外殻=ボールの下端
            Place(0, core, coreRadius, cam, panel, pw, boxTop);
            Place(1, globe, globeRadius, cam, panel, pw, boxTop);
            Place(2, ball, ballRadius, cam, panel, pw, boxTop);
            _lines.MarkDirtyRepaint();
        }

        private void Place(
            int index, Transform target, float radius, Camera cam, IPanel panel, float panelWidth, float boxTop)
        {
            VisualElement box = _boxes[index];
            if (box == null) return;

            if (target == null || !target.gameObject.activeInHierarchy)
            {
                box.style.display = DisplayStyle.None;
                _lines.Visible[index] = false;
                return;
            }

            float centerX = panelWidth * boxCentersRatio[index];
            box.style.display = DisplayStyle.Flex;
            box.style.left = centerX;
            box.style.top = boxTop;

            Vector3 world = target.position + Vector3.down * (radius + 0.06f);
            Vector2 anchor = RuntimePanelUtils.CameraTransformWorldToPanel(panel, world, cam);

            _lines.Visible[index] = true;
            _lines.Anchors[index] = anchor;
            _lines.Corners[index] = new Vector2(centerX, boxTop);
        }
    }
}
