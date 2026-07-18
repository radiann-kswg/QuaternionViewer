using System.Collections.Generic;
using QuaternionViewer.Core;
using UnityEngine;

namespace QuaternionViewer.Visualization
{
    /// <summary>外殻ボールの模型 (仕様書 4.3)。</summary>
    public enum BallModel
    {
        /// <summary>四元数ベクトル部模型 (既定): p = sin(θ/2)·n。q と -q が中心対称に現れる。</summary>
        VectorPart,

        /// <summary>回転ベクトル模型: p = θn/π (θ ≤ π へ折り返し)。等角速度運動が放射直線になる。</summary>
        RotationVector,
    }

    /// <summary>
    /// 外殻: 回転空間そのもの (仕様書 4.3)。
    /// 半径1の中身入りボールに現在姿勢の点と軌跡を描き、表面到達時は対蹠点からワープさせる。
    /// </summary>
    /// <remarks>
    /// <para>中心 = 無回転、表面 = 180°回転。表面上の対蹠点は同一の回転であり、
    /// この貼り合わせで現れるのが RP³ ―― 対蹠ワープはその実体である。</para>
    /// <para>w の符号は色で表す (w ≥ 0 暖色 / w &lt; 0 寒色)。q と -q は中心対称の位置に反転色で現れる。</para>
    /// <para>正準化 (θ &gt; π の折り返し) は回転ベクトル模型の表示にのみ用いる。
    /// Core は畳まない、という決定 (仕様書 3.6-I) の「表示層が選ぶ」側の実装である。</para>
    /// </remarks>
    [ExecuteAlways]
    public class RotationSpaceBall : MonoBehaviour
    {
        public RotationSource source;

        [Header("模型 (仕様書 4.3。既定はベクトル部模型)")]
        public BallModel model = BallModel.VectorPart;

        [Header("形状")]
        public float radius = 1f;
        public int segments = 64;
        public float gridWidth = 0.004f;
        public float trailWidth = 0.012f;

        [Header("軌跡")]
        public int trailCapacity = 512;
        [Tooltip("この距離を超えるジャンプを対蹠ワープとみなし、軌跡を切る (半径比)")]
        public float warpThresholdRatio = 1.0f;
        [Tooltip("この距離未満の移動は軌跡に追加しない")]
        public float minStep = 0.005f;

        /// <summary>w ≥ 0 の暖色。</summary>
        public Color warmColor = new Color(1f, 0.55f, 0.1f);

        /// <summary>w &lt; 0 の寒色。</summary>
        public Color coolColor = new Color(0.15f, 0.65f, 1f);

        private Transform _generated;
        private Transform _marker;
        private Transform _antipodeGhost;
        private Material _markerMat;
        private Material _ghostMat;
        private Material _trailMat;
        private readonly List<LineRenderer> _trailSegments = new List<LineRenderer>();
        private readonly List<Vector3> _currentPoints = new List<Vector3>();
        private int _totalPoints;
        private bool _hasLast;
        private Vector3 _lastPoint;

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
            _trailSegments.Clear();
            _currentPoints.Clear();
            _totalPoints = 0;
            _hasLast = false;
        }

        private void Rebuild()
        {
            Clear();
            _generated = WireGeometry.CreateContainer(transform, "__ball");

            // 中殻の S² グリッド (中間灰) と見分けがつくよう、外殻は寒色寄りにする
            Material gridMat = WireGeometry.CreateUnlitMaterial(new Color(0.30f, 0.42f, 0.62f));
            _markerMat = WireGeometry.CreateUnlitMaterial(warmColor);
            _ghostMat = WireGeometry.CreateUnlitMaterial(coolColor);
            _trailMat = WireGeometry.CreateUnlitMaterial(new Color(0.9f, 0.9f, 0.95f));

            WireGeometry.BuildWireSphere(_generated, gridMat, radius, gridWidth, 1, 4, segments);

            // 中心 = 無回転 の目印
            WireGeometry.CreateMarker(
                _generated, "Center", WireGeometry.CreateUnlitMaterial(new Color(0.7f, 0.7f, 0.75f)), 0.03f);

            _marker = WireGeometry.CreateMarker(_generated, "Pose", _markerMat, 0.09f);

            // 対蹠ゴースト: 同一回転 -q の位置 (中心対称・反転色)。二重被覆の常時演示
            _antipodeGhost = WireGeometry.CreateMarker(_generated, "PoseAntipode", _ghostMat, 0.055f);
        }

        /// <summary>模型写像 (仕様書 4.3)。</summary>
        private Vector3 MapToBall(Quat q)
        {
            if (model == BallModel.VectorPart)
            {
                return q.V; // |p| = sin(θ/2) ≤ 1、無スケールで単位球に収まる
            }

            // 回転ベクトル模型: 表示層の判断で θ ≤ π へ折り返す (仕様書 3.6-I)
            Vector3 r = QuatMath.ToRotationVector(q);
            float theta = r.magnitude;
            if (theta > Mathf.PI)
            {
                r = r * ((theta - 2f * Mathf.PI) / theta); // 同一回転の -(2π-θ)·n 側
            }

            return r / Mathf.PI;
        }

        private void Update()
        {
            if (_generated == null) Rebuild();
            if (source == null) return;

            Quat q = source.Pose;
            Vector3 p = MapToBall(q) * radius;

            _marker.localPosition = p;
            _antipodeGhost.localPosition = model == BallModel.VectorPart ? -p : p;
            _antipodeGhost.gameObject.SetActive(model == BallModel.VectorPart);

            // w の符号は色で表す (仕様書 4.3)
            Color c = q.w >= 0f ? warmColor : coolColor;
            Color inv = q.w >= 0f ? coolColor : warmColor;
            if (_markerMat.HasProperty("_BaseColor")) _markerMat.SetColor("_BaseColor", c);
            else _markerMat.color = c;
            if (_ghostMat.HasProperty("_BaseColor")) _ghostMat.SetColor("_BaseColor", inv);
            else _ghostMat.color = inv;

            AppendTrailPoint(p);
        }

        private void AppendTrailPoint(Vector3 p)
        {
            if (_totalPoints >= trailCapacity) return;

            if (_hasLast)
            {
                float step = (p - _lastPoint).magnitude;
                if (step < minStep) return;

                // 表面到達 → 対蹠点から現れる (RP³ の貼り合わせ)。軌跡はここで切る
                if (step > warpThresholdRatio * radius) StartNewSegment();
            }

            if (_trailSegments.Count == 0) StartNewSegment();

            _currentPoints.Add(p);
            _totalPoints++;
            WireGeometry.SetPositions(_trailSegments[_trailSegments.Count - 1], _currentPoints.ToArray());

            _lastPoint = p;
            _hasLast = true;
        }

        private void StartNewSegment()
        {
            _currentPoints.Clear();
            _trailSegments.Add(WireGeometry.CreateLine(
                _generated, $"Trail{_trailSegments.Count}", _trailMat, trailWidth, false));
        }

        /// <summary>軌跡を消す (章切替やリセットで使う)。</summary>
        public void ClearTrail()
        {
            foreach (LineRenderer lr in _trailSegments)
            {
                if (lr == null) continue;
                if (Application.isPlaying) Destroy(lr.gameObject);
                else DestroyImmediate(lr.gameObject);
            }

            _trailSegments.Clear();
            _currentPoints.Clear();
            _totalPoints = 0;
            _hasLast = false;
        }
    }
}
