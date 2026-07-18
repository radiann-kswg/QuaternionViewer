using QuaternionViewer.Core;
using UnityEngine;

namespace QuaternionViewer.Visualization
{
    /// <summary>
    /// 内核: 回転された結果 (仕様書 4.1)。
    /// <see cref="RotationSource"/> の姿勢をサイコロ+ボディ軸へ適用する。
    /// </summary>
    /// <remarks>
    /// 出目があることが本質で、姿勢の違いが一意に読める。ボディ座標軸 (RGB = X/Y/Z) は
    /// 子の BodyAxes が担い、本コンポーネントごと回すことで「体に張り付いた軸」になる。
    /// </remarks>
    [ExecuteAlways]
    public class DiceRig : MonoBehaviour
    {
        public RotationSource source;

        [Tooltip("姿勢を適用する対象。未指定なら自身")]
        public Transform target;

        private void Reset() => target = transform;

        private void LateUpdate()
        {
            if (source == null) return;
            Transform t = target != null ? target : transform;
            t.rotation = source.Pose.ToUnity();
        }
    }
}
