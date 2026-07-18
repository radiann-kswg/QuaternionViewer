using QuaternionViewer.Core;
using UnityEngine;

namespace QuaternionViewer.Visualization
{
    /// <summary>
    /// 3重ジンバルリング (仕様書 5.4)。外環=yaw / 中環=pitch / 内環=roll。
    /// pitch → ±90° で det E = cos(pitch) → 0 に連動し、軸が揃う外環と内環を赤くハイライトする。
    /// </summary>
    /// <remarks>
    /// 各環の姿勢は <see cref="QuatMath.GimbalStages"/> (仕様書 3.6-G) の累積回転そのまま。
    /// ロックの実体は「roll 軸 ẑ が pitch 90° で ∓ŷ へ倒れ、yaw 軸と平行になる」(仕様書 3.6-F) ――
    /// これは ZXY という写像の座標特異点であり、同じ姿勢を保つ四元数側の内核には何も起きない。
    /// </remarks>
    [ExecuteAlways]
    public class GimbalRig : MonoBehaviour
    {
        public RotationSource source;

        public float outerRadius = 1.15f;
        public float ringGap = 0.15f;
        public int segments = 96;
        public float ringWidth = 0.03f;

        [Tooltip("|cos(pitch)| がこの値を下回り始めたら赤ハイライトを開始")]
        public float warnStart = 0.35f;

        [Tooltip("|cos(pitch)| がこの値で完全な赤")]
        public float warnFull = 0.03f;

        private static readonly Color OuterBase = new Color(0.80f, 0.85f, 0.90f);
        private static readonly Color MiddleBase = new Color(0.58f, 0.66f, 0.74f);
        private static readonly Color InnerBase = new Color(0.44f, 0.52f, 0.60f);
        private static readonly Color LockRed = new Color(1.00f, 0.18f, 0.14f);

        private Transform _generated;
        private Transform _outer;
        private Transform _middle;
        private Transform _inner;
        private Material _outerMat;
        private Material _middleMat;
        private Material _innerMat;

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

        private Transform MakeRing(
            string name, float radius, Material mat, Vector3 axisA, Vector3 axisB, Vector3 stubDir)
        {
            var container = WireGeometry.CreateContainer(_generated, name);

            var ring = WireGeometry.CreateLine(container, "Ring", mat, ringWidth, true);
            WireGeometry.SetPositions(ring,
                WireGeometry.Circle(Vector3.zero, axisA, axisB, radius, segments));

            // ピボット軸受のスタブ (この環が「何の軸で回るか」を見せる)
            foreach (float s in new[] { 1f, -1f })
            {
                var stub = WireGeometry.CreateLine(container, s > 0 ? "Stub+" : "Stub-", mat,
                    ringWidth * 1.4f, false);
                WireGeometry.SetPositions(stub, new[]
                {
                    stubDir * (s * radius),
                    stubDir * (s * (radius + ringGap * 0.85f)),
                });
            }

            return container;
        }

        private void Rebuild()
        {
            Clear();
            _generated = WireGeometry.CreateContainer(transform, "__gimbal");

            _outerMat = WireGeometry.CreateUnlitMaterial(OuterBase);
            _middleMat = WireGeometry.CreateUnlitMaterial(MiddleBase);
            _innerMat = WireGeometry.CreateUnlitMaterial(InnerBase);

            // 静止時: 外環=XY面(法線Z)・yaw軸受±Y / 中環=XZ面(法線Y)・pitch軸受±X / 内環=YZ面(法線X)・roll軸受±Z
            _outer = MakeRing("Outer(yaw)", outerRadius, _outerMat,
                Vector3.right, Vector3.up, Vector3.up);
            _middle = MakeRing("Middle(pitch)", outerRadius - ringGap, _middleMat,
                Vector3.right, Vector3.forward, Vector3.right);
            _inner = MakeRing("Inner(roll)", outerRadius - 2f * ringGap, _innerMat,
                Vector3.up, Vector3.forward, Vector3.forward);
        }

        private static void SetColor(Material m, Color c)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            else m.color = c;
        }

        private void Update()
        {
            if (_generated == null) Rebuild();
            if (source == null) return;

            Vector3 euler = QuatMath.ToEuler(source.Pose);
            QuatMath.GimbalStages(euler, out Quat outer, out Quat middle, out Quat inner);

            _outer.localRotation = outer.ToUnity();
            _middle.localRotation = middle.ToUnity();
            _inner.localRotation = inner.ToUnity();

            // det E = cos(pitch) の絶対値が落ちるほど、軸が揃う外環・内環を赤へ (仕様書 5.4)
            float absCos = Mathf.Abs(Mathf.Cos(euler.x));
            float t = Mathf.InverseLerp(warnStart, warnFull, absCos);
            SetColor(_outerMat, Color.Lerp(OuterBase, LockRed, t));
            SetColor(_innerMat, Color.Lerp(InnerBase, LockRed, t));
        }
    }
}
