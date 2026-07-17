using System;
using System.Globalization;
using UnityEngine;

namespace QuaternionViewer.Core
{
    /// <summary>
    /// 自前の四元数型。ハミルトン積 (ij = k, jk = i, ki = j) に従う (仕様書 2.3)。
    /// 格納順は Unity に合わせて (x, y, z, w)。角度はすべてラジアン。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 座標系は Unity 左手系 Y-up、回転の正の向きは左ねじ (仕様書 2.1-2.2)。
    /// にもかかわらず、本型には handedness 由来の符号反転が一切現れない。
    /// 四元数代数は基底の handedness に依らず、左ねじに見えるのは基底が左手系である
    /// ことの帰結にすぎないためである。この一致は EditMode テストで Unity の
    /// Quaternion と突き合わせて担保する (仕様書 6.3)。
    /// </para>
    /// <para>
    /// 姿勢を表すのは単位四元数のみ (仕様書 2.3)。非単位の入力に対する演算結果は
    /// 保証しないが、NaN を出さないことは保証する。
    /// </para>
    /// </remarks>
    public readonly struct Quat : IEquatable<Quat>
    {
        /// <summary>ノルムがこの値を下回る四元数は退化として扱う。</summary>
        public const float Epsilon = 1e-6f;

        public readonly float x;
        public readonly float y;
        public readonly float z;
        public readonly float w;

        public Quat(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        /// <summary>スカラー部 w とベクトル部 v から構成する。数学記法 q = (w, v) に対応。</summary>
        public static Quat FromScalarVector(float w, Vector3 v) => new Quat(v.x, v.y, v.z, w);

        /// <summary>純虚四元数 (0, v) を構成する。回転作用 q ⊗ ṽ ⊗ q* の ṽ にあたる。</summary>
        public static Quat Pure(Vector3 v) => new Quat(v.x, v.y, v.z, 0f);

        /// <summary>恒等回転 (0, 0, 0, 1)。</summary>
        public static Quat Identity => new Quat(0f, 0f, 0f, 1f);

        /// <summary>ベクトル部 v。軸角表現では sin(θ/2)·n にあたる。</summary>
        public Vector3 V => new Vector3(x, y, z);

        public float SqrNorm => x * x + y * y + z * z + w * w;

        /// <summary>|q|。単位四元数からの漂流 |q| - 1 を見せるために公開する (仕様書 5.6, 6.2)。</summary>
        public float Norm => Mathf.Sqrt(SqrNorm);

        /// <summary>
        /// 正規化した四元数。ノルムが <see cref="Epsilon"/> 未満のときは
        /// NaN を返さず恒等回転へ退避する (仕様書 6.3「NaN を出さない」)。
        /// </summary>
        public Quat Normalized
        {
            get
            {
                float n = Norm;
                if (n < Epsilon) return Identity;
                float inv = 1f / n;
                return new Quat(x * inv, y * inv, z * inv, w * inv);
            }
        }

        /// <summary>共役 q* = (w, -v)。</summary>
        public Quat Conjugate => new Quat(-x, -y, -z, w);

        /// <summary>
        /// 逆元 q⁻¹ = q* / |q|²。単位四元数では共役に一致する (仕様書 2.4)。
        /// </summary>
        public Quat Inverse
        {
            get
            {
                float sqr = SqrNorm;
                if (sqr < Epsilon * Epsilon) return Identity;
                float inv = 1f / sqr;
                return new Quat(-x * inv, -y * inv, -z * inv, w * inv);
            }
        }

        /// <summary>4次元内積 ⟨q0, q1⟩。Slerp の cos Ω にあたる (仕様書 3.2)。</summary>
        public static float Dot(Quat a, Quat b) => a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;

        /// <summary>
        /// ハミルトン積 a ⊗ b。a * b は「b を先に、a を後に」適用する合成であり、
        /// Unity の <c>a * b</c> と一致する (仕様書 2.4)。
        /// </summary>
        public static Quat operator *(Quat a, Quat b) => new Quat(
            a.w * b.x + a.x * b.w + a.y * b.z - a.z * b.y,
            a.w * b.y + a.y * b.w + a.z * b.x - a.x * b.z,
            a.w * b.z + a.z * b.w + a.x * b.y - a.y * b.x,
            a.w * b.w - a.x * b.x - a.y * b.y - a.z * b.z);

        public static Quat operator +(Quat a, Quat b) => new Quat(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w);

        public static Quat operator -(Quat a, Quat b) => new Quat(a.x - b.x, a.y - b.y, a.z - b.z, a.w - b.w);

        /// <summary>符号反転 -q。q と -q は同一の姿勢を指す (二重被覆, 仕様書 5.2)。</summary>
        public static Quat operator -(Quat q) => new Quat(-q.x, -q.y, -q.z, -q.w);

        public static Quat operator *(Quat q, float s) => new Quat(q.x * s, q.y * s, q.z * s, q.w * s);

        public static Quat operator *(float s, Quat q) => q * s;

        public static Vector3 operator *(Quat q, Vector3 v) => q.Rotate(v);

        /// <summary>
        /// 回転作用 v' = q ⊗ ṽ ⊗ q*、ただし ṽ = (0, v) (仕様書 2.4)。
        /// </summary>
        /// <remarks>
        /// 定義式そのままのサンドイッチ積で実装する。展開した高速形も存在するが、
        /// 本プロジェクトの目的は式を見せることであり、速度ではない (仕様書 6.2)。
        /// </remarks>
        public Vector3 Rotate(Vector3 v) => (this * Pure(v) * Conjugate).V;

        /// <summary>
        /// Unity の Quaternion へ変換する。Unity 側への受け渡しはここに集約する (仕様書 6.2)。
        /// </summary>
        public Quaternion ToUnity() => new Quaternion(x, y, z, w);

        /// <summary>Unity の Quaternion から変換する。<see cref="ToUnity"/> の対。</summary>
        public static Quat FromUnity(Quaternion q) => new Quat(q.x, q.y, q.z, q.w);

        public bool Equals(Quat other) =>
            x.Equals(other.x) && y.Equals(other.y) && z.Equals(other.z) && w.Equals(other.w);

        public override bool Equals(object obj) => obj is Quat other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(x, y, z, w);

        public override string ToString() => ToString("F5");

        /// <summary>
        /// 表示は数学記法に合わせ (w, x, y, z) 順で出力する (仕様書 4.4)。
        /// 格納順 (x, y, z, w) とは異なることに注意。
        /// </summary>
        public string ToString(string format) => string.Format(
            CultureInfo.InvariantCulture,
            "({0}, {1}, {2}, {3})",
            w.ToString(format, CultureInfo.InvariantCulture),
            x.ToString(format, CultureInfo.InvariantCulture),
            y.ToString(format, CultureInfo.InvariantCulture),
            z.ToString(format, CultureInfo.InvariantCulture));
    }
}
