using System.Collections.Generic;
using UnityEngine;

namespace QuaternionViewer.Visualization
{
    /// <summary>
    /// フォーカスマーカー ―― 解説ビートの @focus が指す「見よ」の名所に、カメラ正対の脈動リングを重ねる層。
    /// 台本 (section-guide §4) に散在する「〜を見よ」を、話者の指差しに頼らず画面で示す。
    /// </summary>
    /// <remarks>
    /// 対象は名前 (エイリアス) で宣言し、シーン内 GameObject を毎フレーム解決する ――
    /// 中殻・外殻の名所 (__globe/__ball 配下) は実行時に再生成されるため、Transform 参照を保持しない。
    /// マーカーは demos と違い「状態」ではなく「指差し」であり、ビートを離れると
    /// <see cref="SetTargets"/> で自動消灯される (GuideController が毎ビート呼ぶ)。
    /// 生成物は WireGeometry の規約に従いすべて非保存 (シーンファイルを汚さない)。
    /// </remarks>
    [ExecuteAlways]
    public class FocusMarkerRenderer : MonoBehaviour
    {
        /// <summary>台本名 → シーン内 GameObject 名の対応。radius はリングの実寸半径。</summary>
        [System.Serializable]
        public class Alias
        {
            public string name;
            public string objectName;
            public float radius = 0.15f;
        }

        [Tooltip("リングの線幅 (ワールド単位)")]
        public float lineWidth = 0.014f;

        [Tooltip("脈動の周波数 (Hz)")]
        public float pulseHz = 1.1f;

        [Tooltip("脈動の振幅 (半径比)")]
        [Range(0f, 0.5f)]
        public float pulseAmount = 0.12f;

        [Tooltip("エイリアス表 (空なら既定表で埋める)")]
        public List<Alias> aliases = new List<Alias>();

        /// <summary>HudStyle.Accent と同値 (可視化層から UI 層へ依存させないための複製)。</summary>
        private static readonly Color AccentColor = new Color(0.23f, 0.91f, 0.85f);

        /// <summary>既定エイリアス表。台本 (Resources/Guide/*.md) の @focus はこの名前を使う。</summary>
        public static readonly (string name, string objectName, float radius)[] DefaultAliases =
        {
            ("pole+", "Pole+n", 0.17f),
            ("pole-", "Pole-n", 0.17f),
            ("pin", "PinBase", 0.13f),
            ("pinImage", "PinImage", 0.13f),
            ("arc", "SweepArc", 0.22f),
            ("ballPose", "Pose", 0.17f),
            ("ballAntipode", "PoseAntipode", 0.17f),
            ("core", "Core", 0.95f),
            ("globe", "Globe", 1.72f),
            ("ball", "RotationSpaceBall", 1.18f),
            ("mirrors", "HalfAngleMirrors", 1.05f),
            ("gimbal", "GimbalRig", 1.35f),
        };

        private static readonly Vector3[] UnitCircle =
            WireGeometry.Circle(Vector3.zero, Vector3.right, Vector3.up, 1f, 48);

        private readonly List<string> _targets = new List<string>();
        private readonly List<LineRenderer> _rings = new List<LineRenderer>();
        private readonly HashSet<string> _warned = new HashSet<string>();
        private Transform _container;
        private Material _mat;

        /// <summary>このビートで指す対象を丸ごと入れ替える (null / 空 = 全消灯)。</summary>
        public void SetTargets(IReadOnlyList<string> names)
        {
            _targets.Clear();
            if (names != null)
            {
                for (int i = 0; i < names.Count; i++) _targets.Add(names[i]);
            }
        }

        private void OnEnable()
        {
            if (aliases.Count == 0)
            {
                foreach ((string name, string objectName, float radius) a in DefaultAliases)
                {
                    aliases.Add(new Alias { name = a.name, objectName = a.objectName, radius = a.radius });
                }
            }
        }

        private void OnDisable()
        {
            if (_container != null) DestroyImmediate(_container.gameObject);
            _container = null;
            _rings.Clear();
        }

        private void LateUpdate()
        {
            if (_container == null)
            {
                _rings.Clear();
                _container = WireGeometry.CreateContainer(transform, "__focus");
            }

            if (_mat == null) _mat = WireGeometry.CreateUnlitMaterial(AccentColor);

            Camera cam = Camera.main;
            float pulse = 1f + pulseAmount * Mathf.Sin(2f * Mathf.PI * pulseHz * Time.realtimeSinceStartup);

            int used = 0;
            foreach (string targetName in _targets)
            {
                Alias alias = FindAlias(targetName);
                if (alias == null)
                {
                    WarnOnce($"[FocusMarker] 未知の @focus 対象 '{targetName}'");
                    continue;
                }

                GameObject go = GameObject.Find(alias.objectName);
                if (go == null) continue; // 非アクティブ or 未生成 ―― 次フレームで再解決する

                LineRenderer ring = GetRing(used++);
                Transform rt = ring.transform;
                rt.position = go.transform.position;
                if (cam != null)
                {
                    rt.rotation = Quaternion.LookRotation(rt.position - cam.transform.position, cam.transform.up);
                }

                rt.localScale = Vector3.one * (alias.radius * pulse);
                ring.widthMultiplier = lineWidth;
                if (!ring.gameObject.activeSelf) ring.gameObject.SetActive(true);
            }

            for (int i = used; i < _rings.Count; i++)
            {
                if (_rings[i] != null && _rings[i].gameObject.activeSelf) _rings[i].gameObject.SetActive(false);
            }
        }

        private Alias FindAlias(string targetName)
        {
            foreach (Alias a in aliases)
            {
                if (string.Equals(a.name, targetName, System.StringComparison.OrdinalIgnoreCase)) return a;
            }

            return null;
        }

        private LineRenderer GetRing(int index)
        {
            while (_rings.Count <= index)
            {
                LineRenderer lr = WireGeometry.CreateLine(_container, $"ring{_rings.Count}", _mat, lineWidth, true);
                lr.transform.SetParent(_container, false);
                WireGeometry.SetPositions(lr, UnitCircle);
                _rings.Add(lr);
            }

            return _rings[index];
        }

        private void WarnOnce(string message)
        {
            if (_warned.Add(message)) Debug.LogWarning(message, this);
        }
    }
}
