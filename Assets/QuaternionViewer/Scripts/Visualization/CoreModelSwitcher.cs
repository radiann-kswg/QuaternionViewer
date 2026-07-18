using UnityEngine;

namespace QuaternionViewer.Visualization
{
    /// <summary>
    /// 内核の標本モデル切替 (仕様書 4.1 の内核候補: Dice / NineBall / OctantSphere / Knight)。
    /// Core 直下に並べた標本の子オブジェクトを、常に1体だけアクティブにする。
    /// </summary>
    /// <remarks>
    /// 標本はいずれも一辺/直径/高さ 1m・中心原点・TRS恒等で統一されており、
    /// 入れ替えても姿勢の読みが変わらない。出目のあるサイコロが既定 (仕様書 4.1)、
    /// OctantSphere は S² 八分円、NineBall は有向ストライプ、Knight は「正面」の演示担当。
    /// </remarks>
    [ExecuteAlways]
    public class CoreModelSwitcher : MonoBehaviour
    {
        [Tooltip("切替対象の標本モデル (Core 直下の子)")]
        public Transform[] models;

        [SerializeField]
        [Tooltip("アクティブな標本のインデックス")]
        private int activeIndex;

        public int Count => models != null ? models.Length : 0;

        public int ActiveIndex
        {
            get => activeIndex;
            set
            {
                activeIndex = Count > 0 ? Mathf.Clamp(value, 0, Count - 1) : 0;
                Apply();
            }
        }

        /// <summary>UI 表示用のモデル名。</summary>
        public string GetModelName(int index) =>
            models != null && index >= 0 && index < models.Length && models[index] != null
                ? models[index].name
                : "?";

        private void OnEnable() => Apply();

        private void OnValidate() => Apply();

        private void Apply()
        {
            if (models == null) return;
            for (int i = 0; i < models.Length; i++)
            {
                if (models[i] == null) continue;
                bool active = i == activeIndex;
                if (models[i].gameObject.activeSelf != active)
                {
                    models[i].gameObject.SetActive(active);
                }
            }
        }
    }
}
