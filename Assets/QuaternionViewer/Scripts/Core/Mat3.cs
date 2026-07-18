using System.Globalization;
using UnityEngine;

namespace QuaternionViewer.Core
{
    /// <summary>
    /// 3×3 行列。行優先格納の readonly struct (仕様書 3.6-A)。
    /// </summary>
    /// <remarks>
    /// 回転行列表示 (仕様書 4.4) とオイラー角速度ヤコビアン (同 3.6-F) のための最小型。
    /// 積・転置・行列式・トレースのみを持ち、逆行列や分解は意図的に持たない ――
    /// 本プロジェクトで必要になる量だけを、定義そのままの式で公開する (仕様書 6.2)。
    /// </remarks>
    public readonly struct Mat3
    {
        public readonly float m00, m01, m02;
        public readonly float m10, m11, m12;
        public readonly float m20, m21, m22;

        public Mat3(
            float m00, float m01, float m02,
            float m10, float m11, float m12,
            float m20, float m21, float m22)
        {
            this.m00 = m00; this.m01 = m01; this.m02 = m02;
            this.m10 = m10; this.m11 = m11; this.m12 = m12;
            this.m20 = m20; this.m21 = m21; this.m22 = m22;
        }

        public static Mat3 Identity => new Mat3(
            1f, 0f, 0f,
            0f, 1f, 0f,
            0f, 0f, 1f);

        /// <summary>列ベクトル c0, c1, c2 から構成する。ヤコビアン E の組立てに使う (仕様書 3.6-F)。</summary>
        public static Mat3 FromColumns(Vector3 c0, Vector3 c1, Vector3 c2) => new Mat3(
            c0.x, c1.x, c2.x,
            c0.y, c1.y, c2.y,
            c0.z, c1.z, c2.z);

        public Vector3 Column0 => new Vector3(m00, m10, m20);
        public Vector3 Column1 => new Vector3(m01, m11, m21);
        public Vector3 Column2 => new Vector3(m02, m12, m22);

        public Vector3 Row0 => new Vector3(m00, m01, m02);
        public Vector3 Row1 => new Vector3(m10, m11, m12);
        public Vector3 Row2 => new Vector3(m20, m21, m22);

        /// <summary>列ベクトル規約の作用 v' = M v (仕様書 3.6-A)。</summary>
        public static Vector3 operator *(Mat3 m, Vector3 v) => new Vector3(
            m.m00 * v.x + m.m01 * v.y + m.m02 * v.z,
            m.m10 * v.x + m.m11 * v.y + m.m12 * v.z,
            m.m20 * v.x + m.m21 * v.y + m.m22 * v.z);

        public static Mat3 operator *(Mat3 a, Mat3 b) => new Mat3(
            a.m00 * b.m00 + a.m01 * b.m10 + a.m02 * b.m20,
            a.m00 * b.m01 + a.m01 * b.m11 + a.m02 * b.m21,
            a.m00 * b.m02 + a.m01 * b.m12 + a.m02 * b.m22,
            a.m10 * b.m00 + a.m11 * b.m10 + a.m12 * b.m20,
            a.m10 * b.m01 + a.m11 * b.m11 + a.m12 * b.m21,
            a.m10 * b.m02 + a.m11 * b.m12 + a.m12 * b.m22,
            a.m20 * b.m00 + a.m21 * b.m10 + a.m22 * b.m20,
            a.m20 * b.m01 + a.m21 * b.m11 + a.m22 * b.m21,
            a.m20 * b.m02 + a.m21 * b.m12 + a.m22 * b.m22);

        public Mat3 Transposed => new Mat3(
            m00, m10, m20,
            m01, m11, m21,
            m02, m12, m22);

        /// <summary>
        /// 行列式。回転行列なら +1、ヤコビアン E なら cos(pitch) ――
        /// これが 0 へ落ちる瞬間がジンバルロックである (仕様書 3.4, 3.6-F)。
        /// </summary>
        public float Determinant =>
            m00 * (m11 * m22 - m12 * m21)
            - m01 * (m10 * m22 - m12 * m20)
            + m02 * (m10 * m21 - m11 * m20);

        public float Trace => m00 + m11 + m22;

        public override string ToString() => ToString("F5");

        public string ToString(string format) => string.Format(
            CultureInfo.InvariantCulture,
            "[{0}, {1}, {2}; {3}, {4}, {5}; {6}, {7}, {8}]",
            m00.ToString(format, CultureInfo.InvariantCulture),
            m01.ToString(format, CultureInfo.InvariantCulture),
            m02.ToString(format, CultureInfo.InvariantCulture),
            m10.ToString(format, CultureInfo.InvariantCulture),
            m11.ToString(format, CultureInfo.InvariantCulture),
            m12.ToString(format, CultureInfo.InvariantCulture),
            m20.ToString(format, CultureInfo.InvariantCulture),
            m21.ToString(format, CultureInfo.InvariantCulture),
            m22.ToString(format, CultureInfo.InvariantCulture));
    }
}
