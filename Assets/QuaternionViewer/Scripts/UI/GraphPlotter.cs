using QuaternionViewer.Visualization;
using UnityEngine;
using UnityEngine.UIElements;

namespace QuaternionViewer.UI
{
    /// <summary>
    /// 角速度グラフ |ω|(t) (仕様書 5.5)。UI Toolkit の Painter2D によるカスタム描画 (0章の決定)。
    /// Slerp のみ水平線 (角速度一定) になる ―― それがこの計器の見せ場である。
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(UIDocument))]
    public class GraphPlotter : MonoBehaviour
    {
        public InterpRace race;

        [Tooltip("グラフの横サンプル数")]
        public int samples = 64;

        private UIDocument _doc;
        private GraphElement _graph;
        private Label _title;

        /// <summary>Painter2D 描画面。</summary>
        private class GraphElement : VisualElement
        {
            public float[][] Curves;
            public float TMarker;

            public GraphElement()
            {
                generateVisualContent += OnGenerateVisualContent;
                pickingMode = PickingMode.Ignore;
            }

            private void OnGenerateVisualContent(MeshGenerationContext ctx)
            {
                var p = ctx.painter2D;
                float w = contentRect.width;
                float h = contentRect.height;
                if (w < 10f || h < 10f) return;

                // グリッド (端末風の薄い罫線)
                p.strokeColor = new Color(0.23f, 0.91f, 0.85f, 0.12f);
                p.lineWidth = 1f;
                for (int i = 0; i <= 4; i++)
                {
                    float y = h * i / 4f;
                    p.BeginPath();
                    p.MoveTo(new Vector2(0f, y));
                    p.LineTo(new Vector2(w, y));
                    p.Stroke();
                }
                for (int i = 0; i <= 4; i++)
                {
                    float x = w * i / 4f;
                    p.BeginPath();
                    p.MoveTo(new Vector2(x, 0f));
                    p.LineTo(new Vector2(x, h));
                    p.Stroke();
                }

                if (Curves == null) return;

                // 縦軸スケール: 全曲線の最大値
                float max = 1e-4f;
                foreach (float[] c in Curves)
                {
                    if (c == null) continue;
                    foreach (float v in c) max = Mathf.Max(max, v);
                }
                max *= 1.1f;

                for (int ci = 0; ci < Curves.Length; ci++)
                {
                    float[] c = Curves[ci];
                    if (c == null || c.Length < 2) continue;
                    p.strokeColor = InterpRace.CurveColors[ci];
                    p.lineWidth = 2f;
                    p.BeginPath();
                    for (int i = 0; i < c.Length; i++)
                    {
                        var pt = new Vector2(
                            w * i / (c.Length - 1),
                            h * (1f - c[i] / max));
                        if (i == 0) p.MoveTo(pt);
                        else p.LineTo(pt);
                    }
                    p.Stroke();
                }

                // 現在時刻カーソル
                p.strokeColor = new Color(0.84f, 0.90f, 0.93f, 0.55f);
                p.lineWidth = 1f;
                p.BeginPath();
                p.MoveTo(new Vector2(w * TMarker, 0f));
                p.LineTo(new Vector2(w * TMarker, h));
                p.Stroke();
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
            _graph = null;
        }

        private void Build()
        {
            VisualElement root = _doc != null ? _doc.rootVisualElement : null;
            if (root == null) return;
            root.Clear();

            var panel = new VisualElement();
            panel.style.position = Position.Absolute;
            panel.style.bottom = 12f;
            panel.style.right = 12f;
            panel.style.width = 300f;
            panel.style.paddingTop = 8f;
            panel.style.paddingBottom = 6f;
            panel.style.paddingLeft = 10f;
            panel.style.paddingRight = 10f;
            HudStyle.Frame(panel);
            root.Add(panel);

            _title = new Label("ANGULAR SPEED |ω|(t)");
            HudStyle.Header(_title);
            panel.Add(_title);

            _graph = new GraphElement();
            _graph.style.height = 96f;
            _graph.style.marginTop = 4f;
            _graph.style.backgroundColor = new Color(0f, 0f, 0f, 0.25f);
            panel.Add(_graph);

            // 凡例
            var legend = new VisualElement();
            legend.style.flexDirection = FlexDirection.Row;
            legend.style.marginTop = 3f;
            panel.Add(legend);
            for (int i = 0; i < 3; i++)
            {
                var l = new Label("■ " + InterpRace.CurveNames[i]);
                HudStyle.ApplyLatinFont(l);
                l.style.color = InterpRace.CurveColors[i];
                l.style.fontSize = 12;
                l.style.marginRight = 10f;
                legend.Add(l);
            }
        }

        private void Update()
        {
            if (_graph == null || race == null) return;
            _graph.Curves = new[]
            {
                race.SampleSpeeds(0, samples),
                race.SampleSpeeds(1, samples),
                race.SampleSpeeds(2, samples),
            };
            _graph.TMarker = race.t;
            _graph.MarkDirtyRepaint();
        }
    }
}
