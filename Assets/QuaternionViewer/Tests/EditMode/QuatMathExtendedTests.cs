using NUnit.Framework;
using QuaternionViewer.Core;
using UnityEngine;

namespace QuaternionViewer.Tests
{
    /// <summary>
    /// 内部数学ライブラリ 3.6 A〜I の検証 (仕様書 3.6)。
    /// </summary>
    /// <remarks>
    /// <see cref="QuatMathTests"/> と同じ方針で、定義の主張を Unity との突き合わせと
    /// 数値健全性 (NaN を出さない) で担保する。
    /// </remarks>
    public class QuatMathExtendedTests
    {
        private const float Tol = 1e-4f;

        private static readonly Vector3[] Axes =
        {
            Vector3.right,
            Vector3.up,
            Vector3.forward,
            new Vector3(1f, 1f, 0f).normalized,
            new Vector3(1f, 2f, 3f).normalized,
            new Vector3(-2f, 1f, -0.5f).normalized,
        };

        private static readonly float[] AnglesDeg =
        {
            0f, 0.001f, 1f, 30f, 45f, 90f, 120f, 179f, 180f, 200f, 270f, 359f,
        };

        private static readonly Vector3[] EulerDeg =
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(30f, 45f, 60f),
            new Vector3(-20f, 130f, 200f),
            new Vector3(10f, -160f, 75f),
            new Vector3(89.9f, 30f, 45f),
            new Vector3(90f, 30f, 45f),
            new Vector3(-90f, 30f, 45f),
        };

        private static readonly Vector3[] Vectors =
        {
            Vector3.right,
            Vector3.up,
            Vector3.forward,
            new Vector3(1f, 2f, 3f),
            new Vector3(-0.5f, 0.25f, -2f),
        };

        // ---- ヘルパ -------------------------------------------------------

        private static void AssertVectorEqual(Vector3 expected, Vector3 actual, float tol, string context)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(tol), $"{context}: x");
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(tol), $"{context}: y");
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(tol), $"{context}: z");
        }

        private static void AssertVectorEqual(Vector3 expected, Vector3 actual, string context) =>
            AssertVectorEqual(expected, actual, Tol, context);

        private static void AssertSameRotation(Quat expected, Quat actual, string context)
        {
            float absDot = Mathf.Abs(Quat.Dot(expected.Normalized, actual.Normalized));
            Assert.That(absDot, Is.EqualTo(1f).Within(Tol), $"{context}: |⟨q0,q1⟩| が 1 でない");
        }

        private static void AssertMatrixEqual(Mat3 expected, Mat3 actual, string context)
        {
            AssertVectorEqual(expected.Row0, actual.Row0, $"{context}: row0");
            AssertVectorEqual(expected.Row1, actual.Row1, $"{context}: row1");
            AssertVectorEqual(expected.Row2, actual.Row2, $"{context}: row2");
        }

        private static Quat[] SampleRotations()
        {
            var list = new System.Collections.Generic.List<Quat>();
            foreach (Vector3 axis in Axes)
            foreach (float deg in AnglesDeg)
                list.Add(QuatMath.FromAxisAngle(axis, deg * Mathf.Deg2Rad));
            return list.ToArray();
        }

        // ---- A. 回転行列 ToMatrix / Mat3 ----------------------------------

        [Test]
        public void ToMatrix_MatchesUnityMatrix()
        {
            foreach (Quat q in SampleRotations())
            {
                Matrix4x4 u = Matrix4x4.Rotate(q.ToUnity());
                Mat3 m = QuatMath.ToMatrix(q);
                AssertVectorEqual(new Vector3(u.m00, u.m01, u.m02), m.Row0, $"q={q}: row0");
                AssertVectorEqual(new Vector3(u.m10, u.m11, u.m12), m.Row1, $"q={q}: row1");
                AssertVectorEqual(new Vector3(u.m20, u.m21, u.m22), m.Row2, $"q={q}: row2");
            }
        }

        [Test]
        public void ToMatrix_ActionMatchesSandwichProduct()
        {
            foreach (Quat q in SampleRotations())
            foreach (Vector3 v in Vectors)
            {
                AssertVectorEqual(q.Rotate(v), QuatMath.ToMatrix(q) * v, $"q={q}, v={v}");
            }
        }

        [Test]
        public void ToMatrix_IsSpecialOrthogonal()
        {
            foreach (Quat q in SampleRotations())
            {
                Mat3 r = QuatMath.ToMatrix(q);
                AssertMatrixEqual(Mat3.Identity, r.Transposed * r, $"q={q}: RᵀR");
                Assert.That(r.Determinant, Is.EqualTo(1f).Within(Tol), $"q={q}: det R");
            }
        }

        [Test]
        public void Mat3_ProductAndTransposeAreConsistent()
        {
            Mat3 a = QuatMath.ToMatrix(QuatMath.FromAxisAngle(Axes[3], 0.7f));
            Mat3 b = QuatMath.ToMatrix(QuatMath.FromAxisAngle(Axes[4], -1.2f));
            foreach (Vector3 v in Vectors)
            {
                // (AB)v = A(Bv)
                AssertVectorEqual(a * (b * v), (a * b) * v, $"assoc v={v}");
            }

            // (AB)ᵀ = BᵀAᵀ
            AssertMatrixEqual((a * b).Transposed, b.Transposed * a.Transposed, "transpose of product");
        }

        // ---- B. FromToRotation --------------------------------------------

        [Test]
        public void FromToRotation_MapsFromOntoTo()
        {
            foreach (Vector3 a in Axes)
            foreach (Vector3 b in Axes)
            {
                Quat q = QuatMath.FromToRotation(a, b);
                Assert.That(q.Norm, Is.EqualTo(1f).Within(Tol), $"a={a}, b={b}: |q|");
                AssertVectorEqual(b, q.Rotate(a), 1e-3f, $"a={a}, b={b}: q·a");
            }
        }

        [Test]
        public void FromToRotation_MatchesUnity()
        {
            foreach (Vector3 a in Axes)
            foreach (Vector3 b in Axes)
            {
                if (Vector3.Dot(a, b) < -1f + 1e-5f) continue; // 対蹠は軸任意のため比較しない
                Quat expected = Quat.FromUnity(Quaternion.FromToRotation(a, b));
                AssertSameRotation(expected, QuatMath.FromToRotation(a, b), $"a={a}, b={b}");
            }
        }

        [Test]
        public void FromToRotation_AntipodalDegeneracyIsHandled()
        {
            foreach (Vector3 a in Axes)
            {
                Quat q = QuatMath.FromToRotation(a, -a);
                Assert.That(q.Norm, Is.EqualTo(1f).Within(Tol), $"a={a}: |q|");
                Assert.That(Mathf.Abs(q.w), Is.LessThan(Tol), $"a={a}: 180°回転ゆえ w = 0");
                AssertVectorEqual(-a, q.Rotate(a), 1e-3f, $"a={a}: q·a = -a");
            }
        }

        // ---- C. 姿勢間距離 Angle ------------------------------------------

        [Test]
        public void Angle_MatchesUnityQuaternionAngle()
        {
            Quat[] samples = SampleRotations();
            for (int i = 0; i < samples.Length; i += 5)
            for (int j = 0; j < samples.Length; j += 7)
            {
                float expected = Quaternion.Angle(samples[i].ToUnity(), samples[j].ToUnity()) * Mathf.Deg2Rad;
                float actual = QuatMath.Angle(samples[i], samples[j]);
                Assert.That(actual, Is.EqualTo(expected).Within(1e-3f), $"i={i}, j={j}");
            }
        }

        [Test]
        public void Angle_OfDoubleCoverPairIsZero()
        {
            foreach (Quat q in SampleRotations())
            {
                Assert.That(QuatMath.Angle(q, -q), Is.EqualTo(0f).Within(1e-3f), $"q={q}");
                Assert.That(QuatMath.Angle(q, q), Is.EqualTo(0f).Within(1e-3f), $"q={q}");
            }
        }

        // ---- D. 回転ベクトル RotationVector --------------------------------

        [Test]
        public void RotationVector_RoundTripPreservesRotation()
        {
            foreach (Quat q in SampleRotations())
            {
                Vector3 r = QuatMath.ToRotationVector(q);
                AssertSameRotation(q, QuatMath.FromRotationVector(r), $"q={q}");
            }
        }

        [Test]
        public void RotationVector_MagnitudeIsAngle()
        {
            foreach (Vector3 axis in Axes)
            foreach (float deg in AnglesDeg)
            {
                Quat q = QuatMath.FromAxisAngle(axis, deg * Mathf.Deg2Rad);
                QuatMath.ToAxisAngle(q, out _, out float theta);
                Assert.That(QuatMath.ToRotationVector(q).magnitude, Is.EqualTo(theta).Within(1e-3f),
                    $"axis={axis}, deg={deg}");
            }
        }

        // ---- E. 鏡映 Reflect / ReflectionPair ------------------------------

        [Test]
        public void Reflect_IsNormPreservingInvolution()
        {
            foreach (Vector3 m in Axes)
            foreach (Vector3 v in Vectors)
            {
                Vector3 once = QuatMath.Reflect(v, m);
                Assert.That(once.magnitude, Is.EqualTo(v.magnitude).Within(Tol), $"m={m}, v={v}: |v|保存");
                AssertVectorEqual(v, QuatMath.Reflect(once, m), $"m={m}, v={v}: 対合");
            }
        }

        [Test]
        public void ReflectionPair_CompositionEqualsRotation()
        {
            // θ/2 で交わる二枚の鏡に映せば像は θ 回る (仕様書 5.1) ―― 半角の根拠。
            Vector3 gauge = new Vector3(0f, 0f, -1f); // 仕様書 4.2 の既定 p0
            foreach (Vector3 axis in Axes)
            foreach (float deg in new[] { 1f, 30f, 90f, 179f, 270f })
            {
                float rad = deg * Mathf.Deg2Rad;
                QuatMath.ReflectionPair(axis, rad, gauge, out Vector3 m1, out Vector3 m2);
                Quat expected = QuatMath.FromAxisAngle(axis, rad);
                foreach (Vector3 v in Vectors)
                {
                    Vector3 mirrored = QuatMath.Reflect(QuatMath.Reflect(v, m1), m2);
                    AssertVectorEqual(expected.Rotate(v), mirrored, 1e-3f,
                        $"axis={axis}, deg={deg}, v={v}");
                }
            }
        }

        [Test]
        public void ReflectionPair_DegenerateGaugeFallsBackToOrthogonalAxis()
        {
            // ゼロ基準が軸に平行なとき、m1 は軸に直交する任意軸へ退避する (仕様書 4.2)
            Vector3 axis = Vector3.forward;
            QuatMath.ReflectionPair(axis, 1f, Vector3.forward * 2f, out Vector3 m1, out Vector3 m2);
            Assert.That(Vector3.Dot(m1, axis), Is.EqualTo(0f).Within(Tol), "m1 ⊥ n");
            Assert.That(m1.magnitude, Is.EqualTo(1f).Within(Tol), "|m1| = 1");
            Assert.That(m2.magnitude, Is.EqualTo(1f).Within(Tol), "|m2| = 1");
        }

        // ---- F. オイラー角速度ヤコビアン EulerRateJacobian ------------------

        [Test]
        public void EulerRateJacobian_DeterminantIsCosPitch()
        {
            foreach (Vector3 deg in EulerDeg)
            {
                Vector3 rad = deg * Mathf.Deg2Rad;
                float det = QuatMath.EulerRateJacobian(rad).Determinant;
                Assert.That(det, Is.EqualTo(Mathf.Cos(rad.x)).Within(1e-3f), $"euler={deg}");
            }
        }

        [Test]
        public void EulerRateJacobian_MatchesNumericalAngularVelocity()
        {
            // ω = E·(ṗ,ẏ,ṙ) を、q(t) = FromEuler(e + rates·t) の数値微分
            // ω_num = (2/h)·log(q(t+h) ⊗ q(t)*) と突き合わせる (仕様書 3.6-F)。
            const float h = 1e-3f;
            Vector3[] ratesSamples =
            {
                new Vector3(1f, 0f, 0f),
                new Vector3(0f, 1f, 0f),
                new Vector3(0f, 0f, 1f),
                new Vector3(0.5f, -1f, 0.7f),
            };

            foreach (Vector3 deg in EulerDeg)
            foreach (Vector3 rates in ratesSamples)
            {
                Vector3 e0 = deg * Mathf.Deg2Rad;
                Quat q0 = QuatMath.FromEuler(e0);
                Quat q1 = QuatMath.FromEuler(e0 + rates * h);
                Vector3 omegaNum = QuatMath.Log(q1 * q0.Conjugate) * (2f / h);
                Vector3 omegaJac = QuatMath.EulerRateJacobian(e0) * rates;
                AssertVectorEqual(omegaNum, omegaJac, 5e-3f, $"euler={deg}, rates={rates}");
            }
        }

        [Test]
        public void EulerRateJacobian_RankDropsAtGimbalLock()
        {
            // pitch = ±90° では pitch 回転が roll 軸 ẑ を ∓ŷ へ倒すため、
            // 第2列 (yaw軸) と第3列 (roll軸) が平行になる ―― 外環と内環の軸が揃う (仕様書 3.4, 3.6-F)
            foreach (float sign in new[] { 1f, -1f })
            {
                Mat3 e = QuatMath.EulerRateJacobian(new Vector3(sign * 90f, 30f, 45f) * Mathf.Deg2Rad);
                Assert.That(Mathf.Abs(e.Determinant), Is.LessThan(1e-3f), $"det E (pitch={sign * 90}°)");
                float parallel = Mathf.Abs(Vector3.Dot(e.Column1.normalized, e.Column2.normalized));
                Assert.That(parallel, Is.EqualTo(1f).Within(1e-3f), $"yaw列とroll列の平行性 (pitch={sign * 90}°)");
            }
        }

        // ---- G. ジンバル段 GimbalStages ------------------------------------

        [Test]
        public void GimbalStages_InnerEqualsFromEuler()
        {
            foreach (Vector3 deg in EulerDeg)
            {
                Vector3 rad = deg * Mathf.Deg2Rad;
                QuatMath.GimbalStages(rad, out Quat outer, out Quat middle, out Quat inner);
                AssertSameRotation(QuatMath.FromEuler(rad), inner, $"euler={deg}: inner");
                AssertSameRotation(QuatMath.FromAxisAngle(Vector3.up, rad.y), outer, $"euler={deg}: outer");
                AssertSameRotation(outer * QuatMath.FromAxisAngle(Vector3.right, rad.x), middle,
                    $"euler={deg}: middle");
            }
        }

        // ---- H. EulerInterp / AngularSpeed ---------------------------------

        [Test]
        public void EulerInterp_EndpointsMatch()
        {
            Vector3 e0 = new Vector3(30f, 45f, 60f) * Mathf.Deg2Rad;
            Vector3 e1 = new Vector3(-20f, 130f, 200f) * Mathf.Deg2Rad;
            AssertSameRotation(QuatMath.FromEuler(e0), QuatMath.FromEuler(QuatMath.EulerInterp(e0, e1, 0f)), "t=0");
            AssertSameRotation(QuatMath.FromEuler(e1), QuatMath.FromEuler(QuatMath.EulerInterp(e0, e1, 1f)), "t=1");
        }

        [Test]
        public void EulerInterp_TakesShortestComponentPath()
        {
            // yaw 350° → 10° は 0° を跨ぐ 20° の道であり、170° 遠回りしない
            Vector3 e0 = new Vector3(0f, 350f, 0f) * Mathf.Deg2Rad;
            Vector3 e1 = new Vector3(0f, 10f, 0f) * Mathf.Deg2Rad;
            Vector3 mid = QuatMath.EulerInterp(e0, e1, 0.5f);
            AssertSameRotation(
                QuatMath.FromAxisAngle(Vector3.up, 0f),
                QuatMath.FromEuler(mid),
                "中点は yaw 0° 相当");
        }

        [Test]
        public void WrapAngle_MapsToHalfOpenInterval()
        {
            Assert.That(QuatMath.WrapAngle(0f), Is.EqualTo(0f).Within(Tol));
            Assert.That(QuatMath.WrapAngle(Mathf.PI), Is.EqualTo(Mathf.PI).Within(Tol), "+π はそのまま");
            Assert.That(QuatMath.WrapAngle(-Mathf.PI), Is.EqualTo(Mathf.PI).Within(Tol), "-π は +π へ");
            Assert.That(QuatMath.WrapAngle(1.5f * Mathf.PI), Is.EqualTo(-0.5f * Mathf.PI).Within(Tol));
            Assert.That(QuatMath.WrapAngle(-1.5f * Mathf.PI), Is.EqualTo(0.5f * Mathf.PI).Within(Tol));
        }

        [Test]
        public void AngularSpeed_IsConstantAlongSlerp()
        {
            Quat q0 = Quat.Identity;
            Quat q1 = QuatMath.FromAxisAngle(new Vector3(1f, 2f, 3f).normalized, 170f * Mathf.Deg2Rad);
            const int n = 20;
            const float dt = 1f / n;

            float first = -1f;
            for (int i = 0; i < n; i++)
            {
                Quat a = QuatMath.Slerp(q0, q1, i * dt);
                Quat b = QuatMath.Slerp(q0, q1, (i + 1) * dt);
                float speed = QuatMath.AngularSpeed(a, b, dt);
                if (first < 0f) first = speed;
                Assert.That(speed, Is.EqualTo(first).Within(1e-2f), $"segment {i}");
            }

            // 速度の絶対値も理論値 (全回転角 / 全時間) に一致する
            Assert.That(first, Is.EqualTo(170f * Mathf.Deg2Rad).Within(1e-2f), "理論値との一致");
        }

        [Test]
        public void AngularSpeed_VariesAlongNlerp()
        {
            // Nlerp は角速度一定にならない (仕様書 5.5) ―― 大角では変動が観測できる
            Quat q0 = Quat.Identity;
            Quat q1 = QuatMath.FromAxisAngle(Vector3.up, 170f * Mathf.Deg2Rad);
            const int n = 20;
            const float dt = 1f / n;

            float min = float.MaxValue, max = float.MinValue;
            for (int i = 0; i < n; i++)
            {
                Quat a = QuatMath.Nlerp(q0, q1, i * dt);
                Quat b = QuatMath.Nlerp(q0, q1, (i + 1) * dt);
                float speed = QuatMath.AngularSpeed(a, b, dt);
                min = Mathf.Min(min, speed);
                max = Mathf.Max(max, speed);
            }

            Assert.That(max - min, Is.GreaterThan(0.1f), "Nlerp の角速度は一定でない");
        }

        // ---- I. 正準形 Canonical -------------------------------------------

        [Test]
        public void Canonical_FoldsDoubleCoverToSingleRepresentative()
        {
            foreach (Quat q in SampleRotations())
            {
                Quat cq = QuatMath.Canonical(q);
                Quat cnq = QuatMath.Canonical(-q);
                Assert.That(cq.w, Is.GreaterThanOrEqualTo(0f), $"q={q}: w ≥ 0");
                AssertSameRotation(q, cq, $"q={q}: 同一回転");
                Assert.That(cq.x, Is.EqualTo(cnq.x).Within(Tol), $"q={q}: 代表元一意 x");
                Assert.That(cq.y, Is.EqualTo(cnq.y).Within(Tol), $"q={q}: 代表元一意 y");
                Assert.That(cq.z, Is.EqualTo(cnq.z).Within(Tol), $"q={q}: 代表元一意 z");
                Assert.That(cq.w, Is.EqualTo(cnq.w).Within(Tol), $"q={q}: 代表元一意 w");
            }
        }

        [Test]
        public void Canonical_TieBreaksAtWZero()
        {
            // w = 0 (180°回転) はベクトル部の先頭非零成分が正になる側を代表元に取る
            var q = new Quat(0f, -1f, 0f, 0f);
            Quat c = QuatMath.Canonical(q);
            Assert.That(c.y, Is.EqualTo(1f).Within(Tol), "y が正の側");

            var qx = new Quat(-0.6f, 0.8f, 0f, 0f);
            Quat cx = QuatMath.Canonical(qx);
            Assert.That(cx.x, Is.GreaterThan(0f), "先頭非零成分 x が正の側");
        }
    }
}
