using QuaternionViewer.Core;
using UnityEngine;

namespace QuaternionViewer.Visualization
{
    /// <summary>
    /// 現在姿勢 q の保持・配布点 (仕様書 6.1)。
    /// 章コントローラや入力 (アークボール) が書き込み、可視化層 (内核・中殻・外殻) が読む。
    /// </summary>
    /// <remarks>
    /// v1 骨組み段階では、インスペクタの軸角スライダから駆動して各層の追従を確認できる。
    /// アークボール (仕様書 3.5, 7章) が入った時点で <see cref="driveFromInspector"/> を切る。
    /// </remarks>
    [ExecuteAlways]
    public class RotationSource : MonoBehaviour
    {
        [Header("インスペクタ駆動 (骨組み確認用)")]
        public bool driveFromInspector = true;

        [Tooltip("回転軸 n (正規化は内部で行う)")]
        public Vector3 axis = Vector3.up;

        [Tooltip("回転角 θ (度)")]
        [Range(-360f, 360f)]
        public float angleDeg;

        [Header("自動回転 (Playモードのみ)")]
        public bool spin;
        public float spinSpeedDegPerSec = 45f;

        private Quat _pose = Quat.Identity;

        /// <summary>
        /// 現在姿勢。ドメインリロード後の default(Quat) = (0,0,0,0) を
        /// 恒等回転として読み替え、NaN の伝播を防ぐ。
        /// </summary>
        public Quat Pose
        {
            get => _pose.SqrNorm < 0.5f ? Quat.Identity : _pose;
            set => _pose = value;
        }

        private void Update()
        {
            if (spin && Application.isPlaying)
            {
                angleDeg = Mathf.Repeat(angleDeg + 360f + spinSpeedDegPerSec * Time.deltaTime, 720f) - 360f;
            }

            if (driveFromInspector)
            {
                Pose = QuatMath.FromAxisAngle(axis, angleDeg * Mathf.Deg2Rad);
            }
        }
    }
}
