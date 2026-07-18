using QuaternionViewer.Visualization;
using UnityEngine;
using UnityEngine.UIElements;

namespace QuaternionViewer.UI
{
    /// <summary>
    /// 章演出デモの ON/OFF と外殻の模型切替トグル (画面右上・CORE MODEL の下)。
    /// 章切替機構 (ChapterNavigator) が入るまでの操作面。
    /// </summary>
    /// <remarks>
    /// MIRRORS = 半角演示 (Ch.1) / GIMBAL = 3重リング (Ch.4) / INTERP = 三体比較+グラフ (Ch.5)。
    /// BALL MODEL は仕様書 4.3 の「ベクトル部模型 ⇄ 回転ベクトル模型」トグル (切替時に軌跡をクリア)。
    /// </remarks>
    [ExecuteAlways]
    [RequireComponent(typeof(UIDocument))]
    public class DemoTogglesUI : MonoBehaviour
    {
        public HalfAngleMirrors mirrors;
        public GimbalRig gimbal;
        public InterpRace race;
        public GraphPlotter graph;
        public RotationSpaceBall ball;

        private UIDocument _doc;
        private Button _bMirrors;
        private Button _bGimbal;
        private Button _bRace;
        private Button _bModel;

        private void OnEnable()
        {
            _doc = GetComponent<UIDocument>();
            Build();
        }

        private void OnDisable()
        {
            if (_doc != null && _doc.rootVisualElement != null) _doc.rootVisualElement.Clear();
        }

        private static void ToggleGo(Component c)
        {
            if (c != null) c.gameObject.SetActive(!c.gameObject.activeSelf);
        }

        private void Build()
        {
            VisualElement root = _doc != null ? _doc.rootVisualElement : null;
            if (root == null) return;
            root.Clear();

            var panel = new VisualElement();
            panel.style.position = Position.Absolute;
            panel.style.top = 84f; // CORE MODEL (下端≈70) と外殻の上縁 (≈192) の間に収める
            panel.style.right = 12f;
            panel.style.paddingTop = 6f;
            panel.style.paddingBottom = 6f;
            panel.style.paddingLeft = 6f;
            panel.style.paddingRight = 6f;
            HudStyle.Frame(panel);
            root.Add(panel);

            var title = new Label("DEMOS / BALL MODEL");
            HudStyle.Header(title);
            title.style.marginBottom = 3f;
            title.style.unityTextAlign = TextAnchor.MiddleRight;
            panel.Add(title);

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            panel.Add(row);

            _bMirrors = new Button(() => ToggleGo(mirrors)) { text = "MIRRORS" };
            _bGimbal = new Button(() => ToggleGo(gimbal)) { text = "GIMBAL" };
            _bRace = new Button(() =>
            {
                ToggleGo(race);
                if (graph != null && race != null)
                    graph.gameObject.SetActive(race.gameObject.activeSelf);
            }) { text = "INTERP" };
            _bModel = new Button(() =>
            {
                if (ball == null) return;
                ball.model = ball.model == BallModel.VectorPart
                    ? BallModel.RotationVector
                    : BallModel.VectorPart;
                ball.ClearTrail();
            });

            foreach (Button b in new[] { _bMirrors, _bGimbal, _bRace, _bModel })
            {
                HudStyle.Button(b);
                row.Add(b);
            }
        }

        private void Update()
        {
            if (_bMirrors == null) return;
            HudStyle.SetButtonActive(_bMirrors, mirrors != null && mirrors.gameObject.activeSelf);
            HudStyle.SetButtonActive(_bGimbal, gimbal != null && gimbal.gameObject.activeSelf);
            HudStyle.SetButtonActive(_bRace, race != null && race.gameObject.activeSelf);
            if (ball != null)
            {
                _bModel.text = ball.model == BallModel.VectorPart ? "BALL: sin(θ/2)n" : "BALL: θn/π";
            }
        }
    }
}
