using QuaternionViewer.Core;
using UnityEngine;

namespace QuaternionViewer.Visualization
{
    /// <summary>
    /// 中殻: 軸角表現の幾何学的写像 (仕様書 4.2)。
    /// ワールド固定の S² グリッドに、回転軸の極・地標ピン・掃き円弧を重ねる。
    /// </summary>
    /// <remarks>
    /// <para>回転軸 n は S² 上の一点、すなわち緯度経度で指せる ―― ゆえに軸は「地軸」として球を貫く。</para>
    /// <para>
    /// 地標ピンは世界固定の基準点 p0 = (0,0,-1) とその像 q·p0 を結ぶ軸周りの小円弧を掃く。
    /// p0 ∥ n の退化では小円が縮むが、これは正しさではなく視認性の問題である (仕様書 4.2)。
    /// 角度ゲージ (常に最大半径で θ を読む計器) は次段で追加する。
    /// </para>
    /// </remarks>
    [ExecuteAlways]
    public class AxisAngleGlobe : MonoBehaviour
    {
        public RotationSource source;

        [Header("形状")]
        public float radius = 1.5f;
        public int segments = 96;
        [Tooltip("赤道の片側あたりの緯線本数")]
        public int latitudeCount = 2;
        [Tooltip("経線 (大円) の本数")]
        public int longitudeCount = 6;
        public float gridWidth = 0.006f;
        public float arcWidth = 0.02f;

        /// <summary>世界固定の基準点 p0。既定はカメラに正対する赤道上の点 (0,0,-1) (仕様書 4.2)。</summary>
        public Vector3 basePoint = new Vector3(0f, 0f, -1f);

        private Transform _generated;
        private Transform _polePlus;
        private Transform _poleMinus;
        private Transform _pinBase;
        private Transform _pinImage;
        private LineRenderer _sweepArc;
        private LineRenderer _axisLine;

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
            _generated = WireGeometry.CreateContainer(transform, "__globe");

            Material gridMat = WireGeometry.CreateUnlitMaterial(new Color(0.45f, 0.45f, 0.5f));
            Material poleMat = WireGeometry.CreateUnlitMaterial(new Color(0.95f, 0.95f, 1f));
            Material poleMinusMat = WireGeometry.CreateUnlitMaterial(new Color(0.35f, 0.35f, 0.45f));
            Material arcMat = WireGeometry.CreateUnlitMaterial(new Color(1f, 0.8f, 0.15f));
            Material pinBaseMat = WireGeometry.CreateUnlitMaterial(new Color(0.2f, 1f, 0.6f));
            Material pinImageMat = WireGeometry.CreateUnlitMaterial(new Color(1f, 0.45f, 0.2f));

            WireGeometry.BuildWireSphere(
                _generated, gridMat, radius, gridWidth, latitudeCount, longitudeCount, segments);

            // 極: 回転軸 n が球面を貫く2点 (n と -n)。q に応じて動く
            _polePlus = WireGeometry.CreateMarker(_generated, "Pole+n", poleMat, 0.09f);
            _poleMinus = WireGeometry.CreateMarker(_generated, "Pole-n", poleMinusMat, 0.09f);

            // 地軸: 極同士を結ぶ直線
            _axisLine = WireGeometry.CreateLine(_generated, "AxisLine", poleMat, gridWidth * 1.5f, false);

            // 地標ピン: 世界固定の p0 と、その像 q·p0
            _pinBase = WireGeometry.CreateMarker(_generated, "PinBase", pinBaseMat, 0.07f);
            _pinImage = WireGeometry.CreateMarker(_generated, "PinImage", pinImageMat, 0.07f);

            // 掃き円弧: p0 から q·p0 までを軸周りに掃く小円弧
            _sweepArc = WireGeometry.CreateLine(_generated, "SweepArc", arcMat, arcWidth, false);
        }

        private void Update()
        {
            if (_generated == null) Rebuild();
            if (source == null) return;

            Quat q = source.Pose;
            QuatMath.ToAxisAngle(q, out Vector3 axis, out float theta);

            _polePlus.localPosition = axis * radius;
            _poleMinus.localPosition = -axis * radius;
            _axisLine.positionCount = 2;
            _axisLine.SetPosition(0, -axis * (radius * 1.1f));
            _axisLine.SetPosition(1, axis * (radius * 1.1f));

            Vector3 p0 = basePoint.normalized;
            _pinBase.localPosition = p0 * radius;
            _pinImage.localPosition = q.Rotate(p0) * radius;

            // 小円弧: s ∈ [0,1] で FromAxisAngle(n, sθ)·p0 を掃く
            int count = Mathf.Max(2, Mathf.CeilToInt(segments * theta / (2f * Mathf.PI)) + 1);
            var pts = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                float s = i / (float)(count - 1);
                pts[i] = QuatMath.FromAxisAngle(axis, s * theta).Rotate(p0) * radius;
            }

            WireGeometry.SetPositions(_sweepArc, pts);
        }
    }
}
