using QuaternionViewer.Core;
using UnityEngine;

namespace QuaternionViewer.Visualization
{
    /// <summary>
    /// ω ドライバ (仕様書 5.6) ―― dq/dt = ½ω̃⊗q を毎フレーム数値積分して RotationSource を駆動する。
    /// world/body・Euler/RK4・正規化 on/off を切り替えられる、Ch.6 の実験装置。
    /// </summary>
    /// <remarks>
    /// 正規化を切ると離散化誤差で |q| が漂流する ―― GraphPlotter の NormDrift モードがその計器。
    /// 姿勢指示 (@posture/@euler) のあるビートへ移ると GuideController が run を切る (姿勢が優先)。
    /// </remarks>
    [ExecuteAlways]
    public class OmegaDriver : MonoBehaviour
    {
        public RotationSource source;

        [Tooltip("角速度 ω (度/秒)。軸を外した値だと world/body の運動差が見えやすい")]
        public Vector3 omegaDegPerSec = new Vector3(25f, 70f, 15f);

        public AngularVelocitySpace space = AngularVelocitySpace.World;

        [Tooltip("積分器。Euler は漂流が速く教材向き、RK4 は抑えるがゼロにはならない (spec 5.6)")]
        public IntegratorMethod method = IntegratorMethod.Euler;

        [Tooltip("毎ステップ正規化するか。切ると |q| が漂流していく")]
        public bool normalize = true;

        [Tooltip("駆動中か (@action omegaOn / omegaOff)")]
        public bool run;

        private void Update()
        {
            if (!run || source == null || !Application.isPlaying) return;
            source.driveFromInspector = false;
            source.spin = false;
            source.Pose = RotationIntegrator.Step(
                source.Pose,
                omegaDegPerSec * Mathf.Deg2Rad,
                Time.deltaTime,
                space,
                method,
                normalize);
        }
    }
}
