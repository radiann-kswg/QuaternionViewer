using UnityEngine;

namespace QuaternionViewer.Core
{
    /// <summary>
    /// 四元数の指数・対数写像、補間、軸角・オイラー角変換 (仕様書 3章)。
    /// 角度はすべてラジアン。
    /// </summary>
    /// <remarks>
    /// Unity 組み込みの Quaternion は内部が不可視で、Slerp の中身も exp/log も
    /// 見せられない。本クラスは数学的定義そのままを実装し、内部量 (半角 θ/2、
    /// 内積 Ω、|q| の誤差) を UI・グラフへ開放することを唯一の目的とする (仕様書 6.2)。
    /// </remarks>
    public static class QuatMath
    {
        /// <summary>|u| がこの値を下回るとき sin|u|/|u| をテイラー展開で退避する (仕様書 3.1)。</summary>
        public const float TaylorThreshold = 1e-4f;

        /// <summary>sin Ω がこの値を下回るとき Slerp の 0/0 を退避する (仕様書 3.2)。</summary>
        public const float SinOmegaEpsilon = 1e-6f;

        /// <summary>1 - |sin(pitch)| がこの値を下回るとき ZXY 分解をジンバルロックとして扱う。</summary>
        public const float GimbalLockThreshold = 1e-6f;

        /// <summary>
        /// 指数写像 exp(ũ) = (cos|u|, sin|u|/|u|·u)、ただし ũ = (0, u) (仕様書 3.1)。
        /// </summary>
        /// <remarks>
        /// |u| → 0 で 0/0 が生じるが、これは除去可能特異点であり
        /// sin|u|/|u| → 1 - |u|²/6 のテイラー展開で退避できる。
        /// 真の特異点ではない ―― この別は Ch.5 の解説対象である (仕様書 5.5)。
        /// </remarks>
        public static Quat Exp(Vector3 u)
        {
            float len = u.magnitude;
            float sinc = len < TaylorThreshold
                ? 1f - len * len / 6f
                : Mathf.Sin(len) / len;
            return Quat.FromScalarVector(Mathf.Cos(len), u * sinc);
        }

        /// <summary>
        /// 対数写像 log(q) = (θ/2)·n、ただし θ = 2·atan2(|v|, w) (仕様書 3.1)。
        /// 純虚四元数のベクトル部を返す。
        /// </summary>
        /// <remarks>
        /// <para>
        /// |v| → 0 は除去可能特異点。atan2(|v|, w)/|v| → 1/w となるため v/w へ退避する。
        /// </para>
        /// <para>
        /// ただし w &lt; 0 かつ |v| → 0 (すなわち q ≈ -1) は事情が異なり、
        /// 「任意の軸まわりの 2π 回転」を意味するため log は多価となる ―― これは
        /// 除去可能ではない真の縮退である。ここでは NaN を出さない v/w を返すに留める。
        /// <see cref="Slerp"/> は最短経路補正により w ≥ 0 を保証するため、
        /// 補正が有効な限りこの枝には入らない。
        /// </para>
        /// </remarks>
        public static Vector3 Log(Quat q)
        {
            Vector3 v = q.V;
            float vLen = v.magnitude;

            if (vLen < TaylorThreshold)
            {
                if (Mathf.Abs(q.w) < Quat.Epsilon) return Vector3.zero;
                return v / q.w;
            }

            float halfAngle = Mathf.Atan2(vLen, q.w);
            return v * (halfAngle / vLen);
        }

        /// <summary>
        /// 球面線形補間 (仕様書 3.2)。Ω は 4次元内積から定まる q0, q1 のなす角。
        /// </summary>
        /// <param name="shortestPath">
        /// ⟨q0, q1⟩ &lt; 0 のとき q1 ← -q1 とする最短経路補正。
        /// <c>false</c> にすると二重被覆が実害として現れ、遠回り (&gt;180°) を回り出す (仕様書 5.5)。
        /// </param>
        /// <param name="omega">補間の内部量 Ω。グラフ・UI へ開放するため出力する (仕様書 6.2)。</param>
        public static Quat Slerp(Quat q0, Quat q1, float t, bool shortestPath, out float omega)
        {
            float dot = Quat.Dot(q0, q1);

            if (shortestPath && dot < 0f)
            {
                q1 = -q1;
                dot = -dot;
            }

            dot = Mathf.Clamp(dot, -1f, 1f);
            omega = Mathf.Acos(dot);

            // Ω ∈ [0, π] ゆえ sin Ω ≥ 0。両端で 0 に落ちるが、性質は正反対である。
            float sinOmega = Mathf.Sin(omega);
            if (sinOmega < SinOmegaEpsilon)
            {
                if (dot > 0f)
                {
                    // Ω → 0: 除去可能特異点。sin((1-t)Ω)/sinΩ → 1-t、sin(tΩ)/sinΩ → t
                    // となり線形補間に一致する。
                    return (q0 * (1f - t) + q1 * t).Normalized;
                }

                // Ω → π: q1 ≈ -q0。両者を通る大円が一意に定まらない真の特異点であり、
                // 除去可能特異点とは性質が異なる (仕様書 5.5 副題)。
                // 補間経路を選べないため q0 を返す。最短経路補正が有効なら到達しない。
                return q0;
            }

            float a = Mathf.Sin((1f - t) * omega) / sinOmega;
            float b = Mathf.Sin(t * omega) / sinOmega;
            return q0 * a + q1 * b;
        }

        /// <summary>Ω を必要としない呼び出し向けの <see cref="Slerp"/>。</summary>
        public static Quat Slerp(Quat q0, Quat q1, float t, bool shortestPath = true) =>
            Slerp(q0, q1, t, shortestPath, out _);

        /// <summary>
        /// 指数写像による等価な Slerp: q0 ⊗ exp(t·log(q0* ⊗ q1)) (仕様書 3.2)。
        /// </summary>
        /// <remarks>
        /// <see cref="Slerp"/> と数学的に等価。Slerp の正体が指数写像であることを
        /// 示すために別実装として持つ (仕様書 5.6)。両者の一致はテストで担保する。
        /// </remarks>
        public static Quat SlerpViaExp(Quat q0, Quat q1, float t, bool shortestPath = true)
        {
            if (shortestPath && Quat.Dot(q0, q1) < 0f) q1 = -q1;
            return q0 * Exp(t * Log(q0.Conjugate * q1));
        }

        /// <summary>
        /// 正規化線形補間。Slerp と違い角速度は一定にならない (仕様書 5.5)。
        /// </summary>
        public static Quat Nlerp(Quat q0, Quat q1, float t, bool shortestPath = true)
        {
            if (shortestPath && Quat.Dot(q0, q1) < 0f) q1 = -q1;
            return (q0 * (1f - t) + q1 * t).Normalized;
        }

        /// <summary>
        /// 軸角表現から構成する: q = (cos(θ/2), sin(θ/2)·n) (仕様書 2.4)。
        /// </summary>
        public static Quat FromAxisAngle(Vector3 axis, float radians)
        {
            Vector3 n = axis.normalized;
            // Vector3.normalized は零ベクトルに対し零ベクトルを返す。軸が定まらないため恒等回転とする。
            if (n.sqrMagnitude < 0.5f) return Quat.Identity;

            float half = radians * 0.5f;
            return Quat.FromScalarVector(Mathf.Cos(half), n * Mathf.Sin(half));
        }

        /// <summary>
        /// 軸角表現へ分解する。θ = 2·atan2(|v|, w) ∈ [0, 2π] (仕様書 3.1)。
        /// </summary>
        /// <remarks>|v| → 0 では軸が定まらない。θ = 0 ゆえ任意でよく、慣例として +X を返す。</remarks>
        public static void ToAxisAngle(Quat q, out Vector3 axis, out float radians)
        {
            Vector3 v = q.V;
            float vLen = v.magnitude;
            radians = 2f * Mathf.Atan2(vLen, q.w);
            axis = vLen < Quat.Epsilon ? Vector3.right : v / vLen;
        }

        /// <summary>
        /// 半角 θ/2 = atan2(|v|, w)。w = cos(θ/2) と |v| = sin(θ/2) の対比に使う (仕様書 5.1, 6.2)。
        /// </summary>
        public static float HalfAngle(Quat q) => Mathf.Atan2(q.V.magnitude, q.w);

        /// <summary>
        /// オイラー角 (ZXY) から構成する。Unity の Quaternion.Euler に一致する (仕様書 2.5)。
        /// </summary>
        /// <param name="radians">x = pitch, y = yaw, z = roll。Unity の Euler 引数順と同じ。</param>
        /// <remarks>
        /// Quaternion.Euler(x, y, z) は Z → X → Y の順に適用する。行列では
        /// R = Ry(yaw)·Rx(pitch)·Rz(roll) であり、合成は適用順の逆順に並ぶ。
        /// </remarks>
        public static Quat FromEuler(Vector3 radians)
        {
            Quat qy = FromAxisAngle(Vector3.up, radians.y);
            Quat qx = FromAxisAngle(Vector3.right, radians.x);
            Quat qz = FromAxisAngle(Vector3.forward, radians.z);
            return qy * qx * qz;
        }

        /// <summary>
        /// オイラー角 (ZXY) へ分解する。戻り値は x = pitch, y = yaw, z = roll (ラジアン)。
        /// </summary>
        /// <remarks>
        /// 回転行列の要素で書くと R12 = 2(yz - wx) より pitch = asin(-R12)、
        /// roll = atan2(R10, R11)、yaw = atan2(R02, R22)。
        /// </remarks>
        public static Vector3 ToEuler(Quat q)
        {
            float sinPitch = Mathf.Clamp(2f * (q.w * q.x - q.y * q.z), -1f, 1f);
            float pitch = Mathf.Asin(sinPitch);

            if (1f - Mathf.Abs(sinPitch) < GimbalLockThreshold)
            {
                // ジンバルロック。pitch = ±90° で yaw と roll は差 (または和) しか定まらず、
                // 自由度が 3 から 2 に落ちる。これは ZXY という写像の座標特異点であって、
                // q そのものは SO(3) 上の点として何ら特異ではない (仕様書 3.4, 5.4)。
                // 慣例に従い roll = 0 に固定し、残る自由度を yaw に寄せる。
                float r00 = 1f - 2f * (q.y * q.y + q.z * q.z);
                float r01 = 2f * (q.x * q.y - q.w * q.z);
                float lockedYaw = sinPitch > 0f
                    ? Mathf.Atan2(r01, r00)
                    : Mathf.Atan2(-r01, r00);
                return new Vector3(pitch, lockedYaw, 0f);
            }

            float roll = Mathf.Atan2(2f * (q.x * q.y + q.w * q.z), 1f - 2f * (q.x * q.x + q.z * q.z));
            float yaw = Mathf.Atan2(2f * (q.x * q.z + q.w * q.y), 1f - 2f * (q.x * q.x + q.y * q.y));
            return new Vector3(pitch, yaw, roll);
        }
    }
}
