using QuaternionViewer.Core;
using QuaternionViewer.Visualization;
using UnityEngine;
using UnityEngine.UIElements;

namespace QuaternionViewer.UI
{
    /// <summary>
    /// 情報パネル (仕様書 4.4)。q の数値+バー、軸角、オイラー角 (ZXY)、回転行列 (切替表示) を常時出す。
    /// </summary>
    /// <remarks>
    /// <para>単独利用ゆえ情報密度を優先する (仕様書 0章)。UXML/USS を使わず C# だけで組み、
    /// 内部量 (半角、|q| の誤差、det E) をそのまま画面へ開放する ―― 自前実装を採った
    /// 唯一の目的がこれである (仕様書 6.2)。</para>
    /// <para>表示する q は生の値であり、正準化しない (仕様書 3.6-I。Ch.2 で -q を見せるため)。</para>
    /// </remarks>
    [ExecuteAlways]
    [RequireComponent(typeof(UIDocument))]
    public class QuaternionReadout : MonoBehaviour
    {
        public RotationSource source;

        [Tooltip("回転行列セクションの初期表示状態")]
        public bool showMatrix = true;

        private static readonly Color PanelBg = new Color(0.05f, 0.06f, 0.09f, 0.88f);
        private static readonly Color TextMain = new Color(0.88f, 0.89f, 0.92f);
        private static readonly Color TextDim = new Color(0.55f, 0.57f, 0.63f);
        private static readonly Color TrackBg = new Color(0.18f, 0.19f, 0.24f);
        private static readonly Color ColW = new Color(0.91f, 0.76f, 0.35f);
        private static readonly Color ColX = new Color(0.88f, 0.33f, 0.33f);
        private static readonly Color ColY = new Color(0.35f, 0.78f, 0.35f);
        private static readonly Color ColZ = new Color(0.33f, 0.53f, 0.91f);

        private UIDocument _doc;
        private Label _qLabel;
        private Label[] _compValues;
        private VisualElement[] _compFills;
        private Label _axisAngleLabel;
        private Label _halfAngleLabel;
        private Label _eulerLabel;
        private Label _driftLabel;
        private Label[] _matrixCells;
        private VisualElement _matrixBox;
        private Button _matrixToggle;

        private void OnEnable()
        {
            _doc = GetComponent<UIDocument>();
            Build();
        }

        private void OnDisable()
        {
            if (_doc != null && _doc.rootVisualElement != null) _doc.rootVisualElement.Clear();
        }

        // ---- 構築 ----------------------------------------------------------

        private static Label MakeLabel(VisualElement parent, Color color, int size)
        {
            var l = new Label();
            l.style.color = color;
            l.style.fontSize = size;
            l.style.marginTop = 1f;
            l.style.marginBottom = 1f;
            l.style.unityTextAlign = TextAnchor.MiddleLeft;
            parent.Add(l);
            return l;
        }

        private void Build()
        {
            VisualElement root = _doc.rootVisualElement;
            if (root == null) return;
            root.Clear();

            var panel = new VisualElement();
            panel.style.position = Position.Absolute;
            panel.style.top = 12f;
            panel.style.left = 12f;
            panel.style.width = 320f;
            panel.style.backgroundColor = PanelBg;
            panel.style.paddingTop = 8f;
            panel.style.paddingBottom = 8f;
            panel.style.paddingLeft = 10f;
            panel.style.paddingRight = 10f;
            panel.style.borderTopLeftRadius = 6f;
            panel.style.borderTopRightRadius = 6f;
            panel.style.borderBottomLeftRadius = 6f;
            panel.style.borderBottomRightRadius = 6f;
            root.Add(panel);

            var title = MakeLabel(panel, TextDim, 10);
            title.text = "QUATERNION READOUT — q = (w, x, y, z)";

            _qLabel = MakeLabel(panel, TextMain, 13);

            // 成分バー: w, x, y, z の順 (表示は数学記法に合わせ w 先頭。仕様書 4.4)
            _compValues = new Label[4];
            _compFills = new VisualElement[4];
            string[] names = { "w", "x", "y", "z" };
            Color[] colors = { ColW, ColX, ColY, ColZ };
            for (int i = 0; i < 4; i++)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.marginTop = 1f;
                panel.Add(row);

                var name = new Label(names[i]);
                name.style.width = 14f;
                name.style.color = colors[i];
                name.style.fontSize = 11;
                row.Add(name);

                var track = new VisualElement();
                track.style.flexGrow = 1f;
                track.style.height = 7f;
                track.style.backgroundColor = TrackBg;
                track.style.marginLeft = 4f;
                track.style.marginRight = 6f;
                row.Add(track);

                var center = new VisualElement();
                center.style.position = Position.Absolute;
                center.style.left = Length.Percent(50f);
                center.style.width = 1f;
                center.style.height = Length.Percent(100f);
                center.style.backgroundColor = TextDim;
                track.Add(center);

                var fill = new VisualElement();
                fill.style.position = Position.Absolute;
                fill.style.height = Length.Percent(100f);
                fill.style.backgroundColor = colors[i];
                track.Add(fill);
                _compFills[i] = fill;

                var value = new Label();
                value.style.width = 58f;
                value.style.color = TextMain;
                value.style.fontSize = 11;
                value.style.unityTextAlign = TextAnchor.MiddleRight;
                row.Add(value);
                _compValues[i] = value;
            }

            _axisAngleLabel = MakeLabel(panel, TextMain, 12);
            _axisAngleLabel.style.marginTop = 6f;
            _halfAngleLabel = MakeLabel(panel, TextDim, 11);
            _eulerLabel = MakeLabel(panel, TextMain, 12);
            _driftLabel = MakeLabel(panel, TextDim, 11);

            // 回転行列 R(q) (切替表示。仕様書 4.4)
            _matrixToggle = new Button(() =>
            {
                showMatrix = !showMatrix;
                ApplyMatrixVisibility();
            });
            _matrixToggle.style.marginTop = 6f;
            _matrixToggle.style.fontSize = 11;
            _matrixToggle.style.color = TextMain;
            _matrixToggle.style.backgroundColor = TrackBg;
            _matrixToggle.style.unityTextAlign = TextAnchor.MiddleLeft;
            panel.Add(_matrixToggle);

            _matrixBox = new VisualElement();
            _matrixBox.style.marginTop = 2f;
            panel.Add(_matrixBox);

            _matrixCells = new Label[9];
            for (int r = 0; r < 3; r++)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                _matrixBox.Add(row);
                for (int c = 0; c < 3; c++)
                {
                    var cell = new Label();
                    cell.style.width = Length.Percent(33.3f);
                    cell.style.color = TextMain;
                    cell.style.fontSize = 11;
                    cell.style.unityTextAlign = TextAnchor.MiddleRight;
                    row.Add(cell);
                    _matrixCells[r * 3 + c] = cell;
                }
            }

            ApplyMatrixVisibility();
        }

        private void ApplyMatrixVisibility()
        {
            _matrixBox.style.display = showMatrix ? DisplayStyle.Flex : DisplayStyle.None;
            _matrixToggle.text = showMatrix ? "▾ R(q)" : "▸ R(q)";
        }

        // ---- 更新 ----------------------------------------------------------

        private static void SetBar(VisualElement fill, float v)
        {
            float half = 50f;
            float w = Mathf.Clamp01(Mathf.Abs(v)) * half;
            fill.style.width = Length.Percent(w);
            fill.style.left = Length.Percent(v >= 0f ? half : half - w);
        }

        private void Update()
        {
            if (source == null || _qLabel == null) return;

            Quat q = source.Pose;
            QuatMath.ToAxisAngle(q, out Vector3 n, out float theta);
            Vector3 euler = QuatMath.ToEuler(q);
            Mat3 m = QuatMath.ToMatrix(q);
            float half = QuatMath.HalfAngle(q);

            // q は生の値のまま表示する (正準化しない。仕様書 3.6-I)
            _qLabel.text = $"q = ({q.w:F4}, {q.x:F4}, {q.y:F4}, {q.z:F4})";

            float[] comps = { q.w, q.x, q.y, q.z };
            for (int i = 0; i < 4; i++)
            {
                _compValues[i].text = comps[i].ToString("+0.0000;-0.0000");
                SetBar(_compFills[i], comps[i]);
            }

            _axisAngleLabel.text =
                $"θ = {theta * Mathf.Rad2Deg:F1}°  ({theta:F4} rad)   n = ({n.x:F2}, {n.y:F2}, {n.z:F2})";
            _halfAngleLabel.text =
                $"cos(θ/2) = w = {Mathf.Cos(half):F4}    sin(θ/2) = |v| = {Mathf.Sin(half):F4}";

            float detE = Mathf.Cos(euler.x); // det E = cos(pitch) (仕様書 3.4, 3.6-F)
            _eulerLabel.text =
                $"Euler ZXY = (p {euler.x * Mathf.Rad2Deg:F1}°, y {euler.y * Mathf.Rad2Deg:F1}°, r {euler.z * Mathf.Rad2Deg:F1}°)";
            _driftLabel.text =
                $"det E = cos(p) = {detE:F4}      |q| − 1 = {q.Norm - 1f:+0.000000;-0.000000}";

            float[] cells =
            {
                m.m00, m.m01, m.m02,
                m.m10, m.m11, m.m12,
                m.m20, m.m21, m.m22,
            };
            for (int i = 0; i < 9; i++)
            {
                _matrixCells[i].text = cells[i].ToString("+0.000;-0.000");
            }
        }
    }
}
