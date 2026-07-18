using QuaternionViewer.Core;
using UnityEngine;

namespace QuaternionViewer.Visualization
{
    /// <summary>
    /// 三体同時走行 (仕様書 5.5): Slerp / Nlerp / オイラー角補間の軌道を外殻ボール内に描く。
    /// </summary>
    /// <remarks>
    /// 3曲線の全経路を静的に描き、現在時刻 t のマーカーが3体並走する。
    /// 角速度 |ω(t)| の計測は <see cref="QuatMath.AngularSpeed"/> (仕様書 3.6-H) で、
    /// Slerp のみ一定になる ―― グラフ (<c>GraphPlotter</c>) がその計器である。
    /// </remarks>
    [ExecuteAlways]
    public class InterpRace : MonoBehaviour
    {
        public RotationSpaceBall ball;

        [Header("補間の両端 (軸角指定)")]
        public Vector3 startAxis = new Vector3(0f, 1f, 0f);
        public float startAngleDeg = 10f;
        public Vector3 endAxis = new Vector3(1f, 2f, 0.5f);
        public float endAngleDeg = 170f;

        [Header("走行")]
        [Range(0f, 1f)]
        [Tooltip("現在時刻 (エディタではこのスライダで走査)")]
        public float t = 0.35f;
        public bool run = true;
        public float duration = 5f;

        [Header("表示")]
        public int pathSamples = 96;
        public float pathWidth = 0.01f;

        /// <summary>曲線色 (Slerp / Nlerp / Euler)。GraphPlotter の凡例と共有する。</summary>
        public static readonly Color[] CurveColors =
        {
            new Color(0.23f, 0.91f, 0.85f), // Slerp: teal
            new Color(1.00f, 0.64f, 0.24f), // Nlerp: orange
            new Color(0.88f, 0.33f, 0.56f), // Euler: magenta
        };

        public static readonly string[] CurveNames = { "SLERP", "NLERP", "EULER" };

        private Transform _generated;
        private readonly LineRenderer[] _paths = new LineRenderer[3];
        private readonly Transform[] _markers = new Transform[3];

        private Quat Q0 => QuatMath.FromAxisAngle(startAxis, startAngleDeg * Mathf.Deg2Rad);
        private Quat Q1 => QuatMath.FromAxisAngle(endAxis, endAngleDeg * Mathf.Deg2Rad);

        /// <summary>曲線 index (0=Slerp, 1=Nlerp, 2=Euler) の時刻 s における姿勢。</summary>
        public Quat Evaluate(int index, float s)
        {
            Quat q0 = Q0;
            Quat q1 = Q1;
            switch (index)
            {
                case 0: return QuatMath.Slerp(q0, q1, s);
                case 1: return QuatMath.Nlerp(q0, q1, s);
                default:
                    Vector3 e = QuatMath.EulerInterp(QuatMath.ToEuler(q0), QuatMath.ToEuler(q1), s);
                    return QuatMath.FromEuler(e);
            }
        }

        /// <summary>曲線 index の角速度サンプル列。GraphPlotter が読む (仕様書 5.5 グラフ)。</summary>
        public float[] SampleSpeeds(int index, int count)
        {
            var speeds = new float[count];
            float dt = 1f / count;
            for (int i = 0; i < count; i++)
            {
                Quat a = Evaluate(index, i * dt);
                Quat b = Evaluate(index, (i + 1) * dt);
                speeds[i] = QuatMath.AngularSpeed(a, b, dt);
            }

            return speeds;
        }

        private void OnEnable() => Rebuild();

        private void OnDisable() => Clear();

        private void Clear()
        {
            if (_generated != null)
            {
                if (Application.isPlaying) Destroy(_generated.gameObject);
                else DestroyImmediate(_generated.gameObject);
            }

            _generated = null;
        }

        private void Rebuild()
        {
            Clear();
            if (ball == null) return;
            _generated = WireGeometry.CreateContainer(ball.transform, "__race");

            for (int i = 0; i < 3; i++)
            {
                Material m = WireGeometry.CreateUnlitMaterial(CurveColors[i]);
                _paths[i] = WireGeometry.CreateLine(_generated, "Path" + CurveNames[i], m, pathWidth, false);
                _markers[i] = WireGeometry.CreateMarker(_generated, "Mark" + CurveNames[i], m, 0.07f);
            }
        }

        private void Update()
        {
            if (ball == null) return;
            if (_generated == null) Rebuild();

            if (run && Application.isPlaying)
            {
                t = Mathf.Repeat(t + Time.deltaTime / Mathf.Max(0.1f, duration), 1f);
            }

            for (int i = 0; i < 3; i++)
            {
                var pts = new Vector3[pathSamples + 1];
                for (int s = 0; s <= pathSamples; s++)
                {
                    pts[s] = ball.MapPoint(Evaluate(i, s / (float)pathSamples));
                }

                WireGeometry.SetPositions(_paths[i], pts);
                _markers[i].localPosition = ball.MapPoint(Evaluate(i, t));
            }
        }
    }
}
