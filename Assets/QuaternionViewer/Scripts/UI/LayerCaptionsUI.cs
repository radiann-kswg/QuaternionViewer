using UnityEngine;
using UnityEngine.UIElements;

namespace QuaternionViewer.UI
{
    /// <summary>
    /// 三層構造のレイヤーキャプション。中殻 (S² Globe) と外殻 (Rotation Space Ball) の
    /// 足元に説明ラベルを出し、「球が2つある」画面の役割分担を自明にする。
    /// </summary>
    /// <remarks>
    /// 中殻は「実空間の方向球面 S² ―― 回転軸 n の住処」、外殻は「回転空間 RP³ の模型
    /// ―― 四元数 q そのものの住処」であり、両者は別の数学的空間である (仕様書 4.2, 4.3)。
    /// ラベル位置は毎フレーム WorldToPanel で追従するため、配置替えにも強い。
    /// </remarks>
    [ExecuteAlways]
    [RequireComponent(typeof(UIDocument))]
    public class LayerCaptionsUI : MonoBehaviour
    {
        [Tooltip("中殻 (Globe) のTransform")]
        public Transform globe;

        [Tooltip("外殻 (RotationSpaceBall) のTransform")]
        public Transform ball;

        public float globeRadius = 1.5f;
        public float ballRadius = 1f;

        private static readonly Color TextMain = new Color(0.82f, 0.84f, 0.9f);
        private static readonly Color TextSub = new Color(0.55f, 0.58f, 0.66f);
        private const float LabelWidth = 300f;

        private UIDocument _doc;
        private VisualElement _globeCaption;
        private VisualElement _ballCaption;

        private void OnEnable()
        {
            _doc = GetComponent<UIDocument>();
            Build();
        }

        private void OnDisable()
        {
            if (_doc != null && _doc.rootVisualElement != null) _doc.rootVisualElement.Clear();
            _globeCaption = null;
            _ballCaption = null;
        }

        private static VisualElement MakeCaption(VisualElement root, string main, string sub)
        {
            var box = new VisualElement();
            box.style.position = Position.Absolute;
            box.style.width = LabelWidth;
            box.pickingMode = PickingMode.Ignore;

            var l1 = new Label(main);
            l1.style.color = TextMain;
            l1.style.fontSize = 12;
            l1.style.unityTextAlign = TextAnchor.MiddleCenter;
            l1.pickingMode = PickingMode.Ignore;
            box.Add(l1);

            var l2 = new Label(sub);
            l2.style.color = TextSub;
            l2.style.fontSize = 10;
            l2.style.unityTextAlign = TextAnchor.MiddleCenter;
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

            _globeCaption = MakeCaption(root,
                "CORE + S² GLOBE",
                "axis n & angle θ — the axis lives here");
            _ballCaption = MakeCaption(root,
                "ROTATION SPACE BALL",
                "q as a point — center = identity, surface = 180° (RP³)");
        }

        private void Update()
        {
            if (_globeCaption == null || _doc == null || _doc.rootVisualElement == null) return;
            var cam = Camera.main;
            var panel = _doc.rootVisualElement.panel;
            if (cam == null || panel == null) return;

            Place(_globeCaption, globe, globeRadius, cam, panel);
            Place(_ballCaption, ball, ballRadius, cam, panel);
        }

        private static void Place(
            VisualElement caption, Transform target, float radius, Camera cam, IPanel panel)
        {
            if (caption == null) return;
            if (target == null)
            {
                caption.style.display = DisplayStyle.None;
                return;
            }

            Vector3 world = target.position + Vector3.down * (radius + 0.25f);
            Vector2 p = RuntimePanelUtils.CameraTransformWorldToPanel(panel, world, cam);
            caption.style.display = DisplayStyle.Flex;
            caption.style.left = p.x - LabelWidth * 0.5f;
            caption.style.top = p.y;
        }
    }
}
