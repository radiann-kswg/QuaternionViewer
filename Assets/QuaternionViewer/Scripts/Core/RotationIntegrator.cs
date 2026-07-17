using UnityEngine;

namespace QuaternionViewer.Core
{
    /// <summary>角速度をどの系で与えるか。Unity の Space.World / Space.Self に対応する (仕様書 3.3)。</summary>
    public enum AngularVelocitySpace
    {
        /// <summary>ワールド系: q̇ = ½ ω̃ ⊗ q。</summary>
        World,

        /// <summary>ボディ系: q̇ = ½ q ⊗ ω̃。</summary>
        Body,
    }

    /// <summary>数値積分の方式 (仕様書 5.6)。</summary>
    public enum IntegratorMethod
    {
        /// <summary>Euler法。1次。|q| の漂流が目に見えて速い。</summary>
        Euler,

        /// <summary>古典的 Runge-Kutta法。4次。</summary>
        RK4,
    }

    /// <summary>
    /// 回転姿勢の時間発展 q̇ = ½ω̃q を数値積分する (仕様書 3.3, 5.6)。
    /// </summary>
    /// <remarks>
    /// 数値解は S³ 上に留まらず |q| が 1 から漂流する。これは球面上の微分方程式を
    /// 接空間の直線で近似することの必然であり、毎フレームの正規化が要る理由そのものである。
    /// <see cref="Step"/> の正規化を切ると漂流が発散していく様子を見せられる (仕様書 5.6)。
    /// </remarks>
    public static class RotationIntegrator
    {
        /// <summary>
        /// 導関数 q̇ を返す。ワールド系なら ½ ω̃ ⊗ q、ボディ系なら ½ q ⊗ ω̃ (仕様書 3.3)。
        /// </summary>
        public static Quat Derivative(Quat q, Vector3 omega, AngularVelocitySpace space)
        {
            Quat w = Quat.Pure(omega);
            Quat dq = space == AngularVelocitySpace.World ? w * q : q * w;
            return dq * 0.5f;
        }

        /// <summary>
        /// 1ステップ積分する。
        /// </summary>
        /// <param name="normalize">
        /// 結果を正規化するか。<c>false</c> にすると |q| の漂流がそのまま蓄積する (仕様書 5.6)。
        /// </param>
        public static Quat Step(
            Quat q,
            Vector3 omega,
            float dt,
            AngularVelocitySpace space,
            IntegratorMethod method,
            bool normalize = true)
        {
            Quat result = method == IntegratorMethod.RK4
                ? StepRK4(q, omega, dt, space)
                : StepEuler(q, omega, dt, space);

            return normalize ? result.Normalized : result;
        }

        /// <summary>Euler法: q + h·q̇。</summary>
        private static Quat StepEuler(Quat q, Vector3 omega, float dt, AngularVelocitySpace space) =>
            q + Derivative(q, omega, space) * dt;

        /// <summary>
        /// 古典的 RK4。ω は定数のため、各段では q のみを進めて評価する。
        /// </summary>
        private static Quat StepRK4(Quat q, Vector3 omega, float dt, AngularVelocitySpace space)
        {
            Quat k1 = Derivative(q, omega, space);
            Quat k2 = Derivative(q + k1 * (dt * 0.5f), omega, space);
            Quat k3 = Derivative(q + k2 * (dt * 0.5f), omega, space);
            Quat k4 = Derivative(q + k3 * dt, omega, space);
            return q + (k1 + k2 * 2f + k3 * 2f + k4) * (dt / 6f);
        }

        /// <summary>
        /// ω が定数のときの厳密解 q(t) = exp(½ω̃t) ⊗ q0 (仕様書 3.3)。
        /// </summary>
        /// <remarks>
        /// これは同時に「Slerp の正体は指数写像であり、角速度一定の測地線である」
        /// ことの表明でもある。数値解との差を見せる基準として使う (仕様書 5.6)。
        /// </remarks>
        public static Quat Exact(Quat q0, Vector3 omega, float t, AngularVelocitySpace space)
        {
            Quat e = QuatMath.Exp(omega * (0.5f * t));
            return space == AngularVelocitySpace.World ? e * q0 : q0 * e;
        }
    }
}
