using QuaternionViewer.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace QuaternionViewer.Visualization
{
    /// <summary>
    /// 半角演示: θ/2 で交わる二枚の鏡 (仕様書 5.1)。
    /// 「回転は二つの鏡映の合成」であり、鏡を θ/2 で交わらせると像が θ 回る ―― 半角の根拠の実体。
    /// </summary>
    /// <remarks>
    /// 鏡面ペアは <see cref="QuatMath.ReflectionPair"/> (仕様書 3.6-E)。
    /// 点 v → 鏡1の像 → 鏡2の像 の経路を折れ線で描き、最終像が q·v に一致することを見せる。
    /// </remarks>
    [ExecuteAlways]
    public class HalfAngleMirrors : MonoBehaviour
    {
        public RotationSource source;

        [Tooltip("鏡板の一辺サイズ")]
        public float mirrorSize = 1.7f;

        [Tooltip("被鏡映点 v の方向 (既定は基準点 p0 と同じ)")]
        public Vector3 probeDirection = new Vector3(0f, 0f, -1f);

        [Tooltip("被鏡映点の半径")]
        public float probeRadius = 1.2f;

        /// <summary>m1 の向きを定めるゼロ基準 (仕様書 4.2 の ĝ0 と同じ既定)。</summary>
        public Vector3 gaugeZero = new Vector3(0f, 0f, -1f);

        private Transform _generated;
        private Transform _mirror1;
        private Transform _mirror2;
        private Transform _markV;
        private Transform _markV1;
        private Transform _markV2;
        private LineRenderer _path;

        private static Material MakeGlass(Color c)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            var m = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            m.SetFloat("_Surface", 1f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.SetInt("_Cull", (int)CullMode.Off);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetColor("_BaseColor", c);
            return m;
        }

        private Transform MakeMirror(string name, Color c)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            go.hideFlags = HideFlags.DontSave;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(_generated, false);
            go.transform.localScale = new Vector3(mirrorSize, mirrorSize * 1.35f, 1f);
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = MakeGlass(c);
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go.transform;
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
            _generated = WireGeometry.CreateContainer(transform, "__mirrors");

            _mirror1 = MakeMirror("Mirror1", new Color(0.23f, 0.91f, 0.85f, 0.22f));
            _mirror2 = MakeMirror("Mirror2", new Color(1.00f, 0.64f, 0.24f, 0.22f));

            _markV = WireGeometry.CreateMarker(_generated, "V",
                WireGeometry.CreateUnlitMaterial(new Color(0.92f, 0.94f, 0.97f)), 0.075f);
            _markV1 = WireGeometry.CreateMarker(_generated, "V-mirror1",
                WireGeometry.CreateUnlitMaterial(new Color(0.23f, 0.91f, 0.85f)), 0.075f);
            _markV2 = WireGeometry.CreateMarker(_generated, "V-mirror2",
                WireGeometry.CreateUnlitMaterial(new Color(1.00f, 0.64f, 0.24f)), 0.075f);

            _path = WireGeometry.CreateLine(_generated, "Path",
                WireGeometry.CreateUnlitMaterial(new Color(0.85f, 0.88f, 0.92f)), 0.012f, false);
        }

        private void Update()
        {
            if (_generated == null) Rebuild();
            if (source == null) return;

            Quat q = source.Pose;
            QuatMath.ToAxisAngle(q, out Vector3 n, out float theta);
            QuatMath.ReflectionPair(n, theta, gaugeZero, out Vector3 m1, out Vector3 m2);

            // 鏡板: 法線 m、面は回転軸 n を含む (LookRotation: forward=法線, up=軸)
            _mirror1.localRotation = Quaternion.LookRotation(m1, n);
            _mirror2.localRotation = Quaternion.LookRotation(m2, n);

            // 経路 v → S1(v) → S2(S1(v))。最終像は q·v に一致する (3.6-E テスト済)
            Vector3 v = probeDirection.normalized * probeRadius;
            Vector3 v1 = QuatMath.Reflect(v, m1);
            Vector3 v2 = QuatMath.Reflect(v1, m2);

            _markV.localPosition = v;
            _markV1.localPosition = v1;
            _markV2.localPosition = v2;
            WireGeometry.SetPositions(_path, new[] { v, v1, v2 });
        }
    }
}
