using NUnit.Framework;
using QuaternionViewer.Core;
using UnityEngine;

namespace QuaternionViewer.Tests
{
    /// <summary>
    /// 規約 (仕様書 2章) と数理定義 (同 3章) を Unity の Quaternion と突き合わせて検証する。
    /// </summary>
    /// <remarks>
    /// 仕様書 6.3 の方針「規約は主張ではなくテストで担保する」に対応する。
    /// 自前 Quat が Unity と一致することは、左ねじ (仕様書 2.2) や ZXY (同 2.5) といった
    /// 規約をコメントで宣言するのではなく、ここで実行して示す。
    /// </remarks>
    public class QuatMathTests
    {
        private const float Tol = 1e-4f;

        /// <summary>検証に使う回転軸。基本軸と一般の斜め軸を混ぜる。</summary>
        private static readonly Vector3[] Axes =
        {
            Vector3.right,
            Vector3.up,
            Vector3.forward,
            new Vector3(1f, 1f, 0f).normalized,
            new Vector3(1f, 2f, 3f).normalized,
            new Vector3(-2f, 1f, -0.5f).normalized,
            new Vector3(0.3f, -0.7f, 0.2f).normalized,
        };

        /// <summary>
        /// 検証に使う回転角 (度)。θ = 360° は q = -1 となり log が多価になるため含めない
        /// (<see cref="ExpLog_AtMinusOne_IsGenuineAmbiguity"/> で個別に扱う)。
        /// </summary>
        private static readonly float[] AnglesDeg =
        {
            0f, 0.001f, 1f, 30f, 45f, 90f, 120f, 179f, 180f, 200f, 270f, 359f,
        };

        /// <summary>検証に使うオイラー角 (度)。ジンバルロック近傍と真上を含む。</summary>
        private static readonly Vector3[] EulerDeg =
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(30f, 45f, 60f),
            new Vector3(-20f, 130f, 200f),
            new Vector3(10f, -160f, 75f),
            new Vector3(89.9f, 30f, 45f),
            new Vector3(90f, 30f, 45f),
            new Vector3(-90f, 30f, 45f),
            new Vector3(-89.99f, -110f, 15f),
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

        private static void AssertComponentsEqual(Quaternion expected, Quat actual, string context)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(Tol), $"{context}: x");
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(Tol), $"{context}: y");
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(Tol), $"{context}: z");
            Assert.That(actual.w, Is.EqualTo(expected.w).Within(Tol), $"{context}: w");
        }

        private static void AssertComponentsEqual(Quat expected, Quat actual, string context)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(Tol), $"{context}: x");
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(Tol), $"{context}: y");
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(Tol), $"{context}: z");
            Assert.That(actual.w, Is.EqualTo(expected.w).Within(Tol), $"{context}: w");
        }

        /// <summary>二重被覆を許容して「同一の回転か」を検証する (|⟨a,b⟩| ≈ 1)。</summary>
        private static void AssertSameRotation(Quat expected, Quat actual, string context)
        {
            float absDot = Mathf.Abs(Quat.Dot(expected.Normalized, actual.Normalized));
            Assert.That(absDot, Is.EqualTo(1f).Within(Tol), $"{context}: |⟨q0,q1⟩| が 1 でない");
        }

        private static void AssertVectorEqual(Vector3 expected, Vector3 actual, string context)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(Tol), $"{context}: x");
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(Tol), $"{context}: y");
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(Tol), $"{context}: z");
        }

        private static void AssertFinite(Quat q, string context)
        {
            Assert.IsFalse(float.IsNaN(q.x) || float.IsNaN(q.y) || float.IsNaN(q.z) || float.IsNaN(q.w),
                $"{context}: NaN が出た ({q})");
            Assert.IsFalse(float.IsInfinity(q.x) || float.IsInfinity(q.y) || float.IsInfinity(q.z) || float.IsInfinity(q.w),
                $"{context}: Inf が出た ({q})");
        }

        private static void AssertFinite(Vector3 v, string context)
        {
            Assert.IsFalse(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z), $"{context}: NaN が出た ({v})");
            Assert.IsFalse(float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z), $"{context}: Inf が出た ({v})");
        }

        // ---- 2.3 ハミルトン積の基底関係 -----------------------------------

        [Test]
        public void HamiltonProduct_SatisfiesBasisRelations()
        {
            // i² = j² = k² = ijk = -1、ij = k、jk = i、ki = j (仕様書 2.3)
            var i = new Quat(1f, 0f, 0f, 0f);
            var j = new Quat(0f, 1f, 0f, 0f);
            var k = new Quat(0f, 0f, 1f, 0f);
            var minusOne = new Quat(0f, 0f, 0f, -1f);

            AssertComponentsEqual(minusOne, i * i, "i²");
            AssertComponentsEqual(minusOne, j * j, "j²");
            AssertComponentsEqual(minusOne, k * k, "k²");
            AssertComponentsEqual(minusOne, i * j * k, "ijk");

            AssertComponentsEqual(k, i * j, "ij");
            AssertComponentsEqual(i, j * k, "jk");
            AssertComponentsEqual(j, k * i, "ki");
        }

        // ---- 6.3 軸角の一致 -----------------------------------------------

        [Test]
        public void FromAxisAngle_MatchesUnityAngleAxis()
        {
            foreach (Vector3 axis in Axes)
            {
                foreach (float deg in AnglesDeg)
                {
                    Quat mine = QuatMath.FromAxisAngle(axis, deg * Mathf.Deg2Rad);
                    Quaternion unity = Quaternion.AngleAxis(deg, axis);
                    AssertComponentsEqual(unity, mine, $"FromAxisAngle(axis={axis}, {deg}°)");
                }
            }
        }

        [Test]
        public void ToAxisAngle_RoundTripsThroughFromAxisAngle()
        {
            foreach (Vector3 axis in Axes)
            {
                foreach (float deg in AnglesDeg)
                {
                    // θ = 0 は軸が定まらないため往復の対象外 (回転としては恒等)。
                    if (deg < 0.01f) continue;

                    Quat q = QuatMath.FromAxisAngle(axis, deg * Mathf.Deg2Rad);
                    QuatMath.ToAxisAngle(q, out Vector3 axis2, out float rad2);
                    Quat back = QuatMath.FromAxisAngle(axis2, rad2);
                    AssertComponentsEqual(q, back, $"ToAxisAngle 往復 (axis={axis}, {deg}°)");
                }
            }
        }

        [Test]
        public void HalfAngle_MatchesCosSinDecomposition()
        {
            // w = cos(θ/2)、|v| = sin(θ/2) (仕様書 5.1)
            foreach (Vector3 axis in Axes)
            {
                foreach (float deg in AnglesDeg)
                {
                    Quat q = QuatMath.FromAxisAngle(axis, deg * Mathf.Deg2Rad);
                    float half = QuatMath.HalfAngle(q);
                    string ctx = $"HalfAngle (axis={axis}, {deg}°)";
                    Assert.That(Mathf.Cos(half), Is.EqualTo(q.w).Within(Tol), $"{ctx}: w = cos(θ/2)");
                    Assert.That(Mathf.Sin(half), Is.EqualTo(q.V.magnitude).Within(Tol), $"{ctx}: |v| = sin(θ/2)");
                }
            }
        }

        // ---- 6.3 合成順の一致 ---------------------------------------------

        [Test]
        public void Product_MatchesUnityProductAndIsAssociativeOnVectors()
        {
            Quat q1 = QuatMath.FromAxisAngle(Vector3.up, 40f * Mathf.Deg2Rad);
            Quat q2 = QuatMath.FromAxisAngle(new Vector3(1f, 2f, 3f).normalized, 110f * Mathf.Deg2Rad);
            Quaternion u1 = q1.ToUnity();
            Quaternion u2 = q2.ToUnity();

            // q2 * q1 は「q1 を先に、q2 を後に」適用する合成であり、Unity と一致する (仕様書 2.4)
            AssertComponentsEqual(u2 * u1, q2 * q1, "q2 * q1");

            foreach (Vector3 v in Vectors)
            {
                AssertVectorEqual((q2 * q1) * v, q2 * (q1 * v), $"(q2*q1)*v == q2*(q1*v), v={v}");
            }
        }

        [Test]
        public void Product_AppliesRightOperandFirst()
        {
            // 「先に q1、後に q2」の意味論そのものを、逐次適用と突き合わせて確かめる。
            Quat q1 = QuatMath.FromAxisAngle(Vector3.right, 90f * Mathf.Deg2Rad);
            Quat q2 = QuatMath.FromAxisAngle(Vector3.up, 90f * Mathf.Deg2Rad);

            foreach (Vector3 v in Vectors)
            {
                AssertVectorEqual(q2.Rotate(q1.Rotate(v)), (q2 * q1).Rotate(v), $"逐次適用との一致, v={v}");
            }
        }

        [Test]
        public void Product_IsNonCommutative()
        {
            // 非可換性は Ch.3 の主題そのもの (仕様書 5.3)。可換になっていないことを明示的に固定する。
            Quat qx = QuatMath.FromAxisAngle(Vector3.right, 90f * Mathf.Deg2Rad);
            Quat qy = QuatMath.FromAxisAngle(Vector3.up, 90f * Mathf.Deg2Rad);

            float absDot = Mathf.Abs(Quat.Dot(qy * qx, qx * qy));
            Assert.That(absDot, Is.LessThan(1f - Tol), "q2*q1 と q1*q2 が同一の回転になってしまっている");
        }

        // ---- 6.3 作用の一致 -----------------------------------------------

        [Test]
        public void Rotate_MatchesUnityQuaternionTimesVector()
        {
            foreach (Vector3 axis in Axes)
            {
                foreach (float deg in AnglesDeg)
                {
                    Quat q = QuatMath.FromAxisAngle(axis, deg * Mathf.Deg2Rad);
                    Quaternion u = q.ToUnity();

                    foreach (Vector3 v in Vectors)
                    {
                        AssertVectorEqual(u * v, q.Rotate(v), $"q ⊗ ṽ ⊗ q* (axis={axis}, {deg}°, v={v})");
                    }
                }
            }
        }

        [Test]
        public void Rotate_PreservesLength()
        {
            foreach (Vector3 axis in Axes)
            {
                Quat q = QuatMath.FromAxisAngle(axis, 137f * Mathf.Deg2Rad);
                foreach (Vector3 v in Vectors)
                {
                    Assert.That(q.Rotate(v).magnitude, Is.EqualTo(v.magnitude).Within(Tol),
                        $"回転が長さを変えている (axis={axis}, v={v})");
                }
            }
        }

        // ---- 6.3 オイラー規約 ---------------------------------------------

        [Test]
        public void FromEuler_MatchesUnityEuler()
        {
            foreach (Vector3 deg in EulerDeg)
            {
                Quat mine = QuatMath.FromEuler(deg * Mathf.Deg2Rad);
                Quaternion unity = Quaternion.Euler(deg);
                AssertSameRotation(Quat.FromUnity(unity), mine, $"FromEuler({deg})");
            }
        }

        [Test]
        public void UnityEuler_IsZxyComposition()
        {
            // Quaternion.Euler(x,y,z) == AngleAxis(y,up) * AngleAxis(x,right) * AngleAxis(z,forward)
            // すなわち Z → X → Y の順に適用される (仕様書 2.5)。規約そのものを固定する。
            foreach (Vector3 deg in EulerDeg)
            {
                Quaternion composed =
                    Quaternion.AngleAxis(deg.y, Vector3.up) *
                    Quaternion.AngleAxis(deg.x, Vector3.right) *
                    Quaternion.AngleAxis(deg.z, Vector3.forward);
                Quaternion euler = Quaternion.Euler(deg);

                AssertSameRotation(Quat.FromUnity(composed), Quat.FromUnity(euler), $"ZXY 合成 ({deg})");
            }
        }

        [Test]
        public void ToEuler_RoundTripsAsRotation()
        {
            foreach (Vector3 deg in EulerDeg)
            {
                Quat q = QuatMath.FromEuler(deg * Mathf.Deg2Rad);
                Vector3 euler = QuatMath.ToEuler(q);
                AssertFinite(euler, $"ToEuler({deg})");

                Quat back = QuatMath.FromEuler(euler);
                AssertSameRotation(q, back, $"ToEuler 往復 ({deg})");
            }
        }

        [Test]
        public void ToEuler_AtGimbalLock_IsFiniteAndRoundTrips()
        {
            // pitch = ±90° は ZXY 写像の座標特異点。yaw と roll が縮退して一意に定まらないが、
            // 回転としての往復は保たれ、q 自体は何ら特異ではない (仕様書 3.4, 5.4)。
            foreach (float pitch in new[] { 90f, -90f })
            {
                foreach (float yaw in new[] { 0f, 37f, -128f })
                {
                    foreach (float roll in new[] { 0f, 55f, -95f })
                    {
                        var deg = new Vector3(pitch, yaw, roll);
                        Quat q = QuatMath.FromEuler(deg * Mathf.Deg2Rad);
                        Vector3 euler = QuatMath.ToEuler(q);

                        AssertFinite(euler, $"ジンバルロック ToEuler({deg})");
                        Assert.That(euler.z, Is.EqualTo(0f).Within(Tol),
                            $"ジンバルロックでは roll を 0 に寄せる規約 ({deg})");
                        AssertSameRotation(q, QuatMath.FromEuler(euler), $"ジンバルロック往復 ({deg})");
                    }
                }
            }
        }

        // ---- 6.3 Slerp の一致 ---------------------------------------------

        [Test]
        public void Slerp_MatchesUnitySlerp()
        {
            Quat q0 = QuatMath.FromAxisAngle(new Vector3(1f, 2f, 3f).normalized, 25f * Mathf.Deg2Rad);
            Quat q1 = QuatMath.FromAxisAngle(new Vector3(-1f, 0.5f, 2f).normalized, 200f * Mathf.Deg2Rad);

            foreach (float t in new[] { 0f, 0.13f, 0.25f, 0.5f, 0.75f, 0.99f, 1f })
            {
                Quat mine = QuatMath.Slerp(q0, q1, t, shortestPath: true);
                Quaternion unity = Quaternion.Slerp(q0.ToUnity(), q1.ToUnity(), t);
                AssertSameRotation(Quat.FromUnity(unity), mine, $"Slerp(t={t})");
            }
        }

        [Test]
        public void Slerp_MatchesExponentialMapForm()
        {
            // slerp(q0,q1;t) == q0 ⊗ exp(t·log(q0* ⊗ q1)) (仕様書 3.2)。
            // Slerp の正体が指数写像であることの表明 (仕様書 5.6)。
            Quat q0 = QuatMath.FromAxisAngle(new Vector3(0.2f, 1f, -0.4f).normalized, 15f * Mathf.Deg2Rad);
            Quat q1 = QuatMath.FromAxisAngle(new Vector3(1f, -1f, 0.3f).normalized, 140f * Mathf.Deg2Rad);

            foreach (float t in new[] { 0f, 0.2f, 0.5f, 0.8f, 1f })
            {
                Quat direct = QuatMath.Slerp(q0, q1, t, shortestPath: true);
                Quat viaExp = QuatMath.SlerpViaExp(q0, q1, t, shortestPath: true);
                AssertSameRotation(direct, viaExp, $"Slerp == exp形 (t={t})");
            }
        }

        [Test]
        public void Slerp_HasConstantAngularSpeed()
        {
            // Slerp は角速度一定の測地線である (仕様書 3.3, 5.5)。
            // 等間隔の t に対し、隣接姿勢間の相対回転角が一定になることで示す。
            Quat q0 = QuatMath.FromAxisAngle(Vector3.up, 10f * Mathf.Deg2Rad);
            Quat q1 = QuatMath.FromAxisAngle(new Vector3(1f, 1f, 1f).normalized, 150f * Mathf.Deg2Rad);

            const int steps = 16;
            float first = 0f;
            for (int i = 0; i < steps; i++)
            {
                Quat a = QuatMath.Slerp(q0, q1, i / (float)steps, shortestPath: true);
                Quat b = QuatMath.Slerp(q0, q1, (i + 1) / (float)steps, shortestPath: true);
                QuatMath.ToAxisAngle((b * a.Conjugate).Normalized, out _, out float step);

                if (i == 0) first = step;
                else Assert.That(step, Is.EqualTo(first).Within(1e-3f), $"Slerp の刻み角が一定でない (i={i})");
            }
        }

        [Test]
        public void Slerp_WithoutShortestPath_TakesLongWay()
        {
            // 最短経路補正を切ると ⟨q0,q1⟩ < 0 のとき遠回りを回り出す。
            // Ch.2 の二重被覆が実害として現れる瞬間そのもの (仕様書 5.5)。
            Quat q0 = Quat.Identity;
            Quat q1 = -QuatMath.FromAxisAngle(Vector3.up, 90f * Mathf.Deg2Rad);
            Assert.That(Quat.Dot(q0, q1), Is.LessThan(0f), "テスト前提: ⟨q0,q1⟩ < 0 であること");

            QuatMath.Slerp(q0, q1, 0.5f, shortestPath: true, out float omegaShort);
            QuatMath.Slerp(q0, q1, 0.5f, shortestPath: false, out float omegaLong);

            Assert.That(omegaLong, Is.GreaterThan(omegaShort), "補正を切っても経路が長くなっていない");
        }

        // ---- 6.3 二重被覆 -------------------------------------------------

        [Test]
        public void DoubleCover_QAndMinusQ_GiveSameVectorImage()
        {
            foreach (Vector3 axis in Axes)
            {
                foreach (float deg in AnglesDeg)
                {
                    Quat q = QuatMath.FromAxisAngle(axis, deg * Mathf.Deg2Rad);
                    Quat neg = -q;

                    foreach (Vector3 v in Vectors)
                    {
                        AssertVectorEqual(q.Rotate(v), neg.Rotate(v), $"二重被覆 (axis={axis}, {deg}°, v={v})");
                    }
                }
            }
        }

        // ---- 6.3 exp / log 往復 -------------------------------------------

        [Test]
        public void ExpLog_RoundTrips()
        {
            foreach (Vector3 axis in Axes)
            {
                foreach (float deg in AnglesDeg)
                {
                    Quat q = QuatMath.FromAxisAngle(axis, deg * Mathf.Deg2Rad);
                    Quat back = QuatMath.Exp(QuatMath.Log(q));
                    AssertComponentsEqual(q, back, $"exp(log(q)) (axis={axis}, {deg}°)");
                }
            }
        }

        [Test]
        public void ExpLog_RoundTripsNearIdentity()
        {
            // |v| → 0 の縮退近傍。テイラー展開による退避が効いていることを確かめる (仕様書 3.1)。
            foreach (Vector3 axis in Axes)
            {
                foreach (float rad in new[] { 0f, 1e-7f, 1e-6f, 1e-5f, 1e-4f, 1e-3f })
                {
                    Quat q = QuatMath.FromAxisAngle(axis, rad);
                    Vector3 log = QuatMath.Log(q);
                    AssertFinite(log, $"log 近傍 (axis={axis}, θ={rad})");

                    Quat back = QuatMath.Exp(log);
                    AssertFinite(back, $"exp(log) 近傍 (axis={axis}, θ={rad})");
                    AssertComponentsEqual(q, back, $"exp(log(q)) 近傍 (axis={axis}, θ={rad})");
                }
            }
        }

        [Test]
        public void ExpLog_AtMinusOne_IsGenuineAmbiguity()
        {
            // q = -1 は「任意の軸まわりの 2π 回転」であり log は多価。除去可能特異点ではない。
            // exp(log(-1)) は +1 を返すため成分としては往復しないが、
            // -1 と +1 は同一の回転であるため、回転としては一致する。
            var minusOne = new Quat(0f, 0f, 0f, -1f);

            Vector3 log = QuatMath.Log(minusOne);
            AssertFinite(log, "log(-1)");

            Quat back = QuatMath.Exp(log);
            AssertFinite(back, "exp(log(-1))");
            AssertSameRotation(minusOne, back, "-1 と exp(log(-1)) は同一の回転");
        }

        // ---- 6.3 除去可能特異点 / NaN 耐性 --------------------------------

        [Test]
        public void Exp_AtZero_IsIdentity()
        {
            Quat e = QuatMath.Exp(Vector3.zero);
            AssertFinite(e, "exp(0)");
            AssertComponentsEqual(Quat.Identity, e, "exp(0) == 恒等回転");
        }

        [Test]
        public void Log_AtIdentity_IsZero()
        {
            Vector3 log = QuatMath.Log(Quat.Identity);
            AssertFinite(log, "log(1)");
            AssertVectorEqual(Vector3.zero, log, "log(1) == 0");
        }

        [Test]
        public void Slerp_WhenOmegaApproachesZero_DoesNotProduceNaN()
        {
            // Ω → 0 は除去可能特異点。線形補間へ連続に落ちる (仕様書 5.5 副題)。
            Quat q0 = QuatMath.FromAxisAngle(Vector3.up, 30f * Mathf.Deg2Rad);

            foreach (float delta in new[] { 0f, 1e-7f, 1e-6f, 1e-5f })
            {
                Quat q1 = QuatMath.FromAxisAngle(Vector3.up, (30f * Mathf.Deg2Rad) + delta);
                foreach (float t in new[] { 0f, 0.5f, 1f })
                {
                    Quat s = QuatMath.Slerp(q0, q1, t, shortestPath: true, out float omega);
                    AssertFinite(s, $"Slerp Ω→0 (delta={delta}, t={t})");
                    Assert.IsFalse(float.IsNaN(omega), $"Ω が NaN (delta={delta}, t={t})");
                    AssertSameRotation(q0, s, $"Ω→0 では q0 に留まる (delta={delta}, t={t})");
                }
            }
        }

        [Test]
        public void Slerp_AtAntipode_DoesNotProduceNaN()
        {
            // Ω → π (q1 = -q0) は経路が定まらない真の特異点。
            // 補正を切った場合でも NaN を出さないことを担保する (仕様書 6.3)。
            Quat q0 = QuatMath.FromAxisAngle(new Vector3(1f, 2f, 3f).normalized, 70f * Mathf.Deg2Rad);
            Quat q1 = -q0;

            foreach (float t in new[] { 0f, 0.25f, 0.5f, 0.75f, 1f })
            {
                Quat off = QuatMath.Slerp(q0, q1, t, shortestPath: false, out float omegaOff);
                AssertFinite(off, $"Slerp 対蹠 補正なし (t={t})");
                Assert.IsFalse(float.IsNaN(omegaOff), $"Ω が NaN (t={t})");

                Quat on = QuatMath.Slerp(q0, q1, t, shortestPath: true, out float omegaOn);
                AssertFinite(on, $"Slerp 対蹠 補正あり (t={t})");
                Assert.IsFalse(float.IsNaN(omegaOn), $"Ω が NaN (t={t})");
            }
        }

        [Test]
        public void Nlerp_AtAntipode_DoesNotProduceNaN()
        {
            // 補正を切ると t = 0.5 で和が零四元数になる。正規化が NaN を出さないこと。
            Quat q0 = QuatMath.FromAxisAngle(Vector3.forward, 45f * Mathf.Deg2Rad);
            Quat q1 = -q0;

            foreach (float t in new[] { 0f, 0.5f, 1f })
            {
                AssertFinite(QuatMath.Nlerp(q0, q1, t, shortestPath: false), $"Nlerp 対蹠 補正なし (t={t})");
                AssertFinite(QuatMath.Nlerp(q0, q1, t, shortestPath: true), $"Nlerp 対蹠 補正あり (t={t})");
            }
        }

        [Test]
        public void FromAxisAngle_WithZeroAxis_ReturnsIdentity()
        {
            Quat q = QuatMath.FromAxisAngle(Vector3.zero, 42f * Mathf.Deg2Rad);
            AssertFinite(q, "FromAxisAngle(0軸)");
            AssertComponentsEqual(Quat.Identity, q, "軸が定まらないため恒等回転");
        }

        [Test]
        public void Normalized_WithZeroQuat_ReturnsIdentity()
        {
            Quat zero = new Quat(0f, 0f, 0f, 0f);
            AssertFinite(zero.Normalized, "零四元数の正規化");
            AssertComponentsEqual(Quat.Identity, zero.Normalized, "零四元数は恒等回転へ退避");
        }

        // ---- 3.3 時間発展 -------------------------------------------------

        [Test]
        public void Integrator_Rk4_ApproachesExactSolution()
        {
            // ω 一定の厳密解は q(t) = exp(½ω̃t) ⊗ q0 (仕様書 3.3)。
            foreach (AngularVelocitySpace space in new[] { AngularVelocitySpace.World, AngularVelocitySpace.Body })
            {
                var omega = new Vector3(0.7f, -1.3f, 0.4f);
                Quat q0 = QuatMath.FromAxisAngle(Vector3.up, 20f * Mathf.Deg2Rad);

                const float dt = 1f / 240f;
                const int steps = 240;

                Quat numeric = q0;
                for (int i = 0; i < steps; i++)
                {
                    numeric = RotationIntegrator.Step(numeric, omega, dt, space, IntegratorMethod.RK4);
                }

                Quat exact = RotationIntegrator.Exact(q0, omega, dt * steps, space);
                AssertSameRotation(exact, numeric, $"RK4 が厳密解に一致しない ({space})");
            }
        }

        [Test]
        public void Integrator_WithoutNormalization_DriftsOffUnitSphere()
        {
            // 正規化を切ると |q| が 1 から漂流する。毎フレーム正規化が要る理由そのもの (仕様書 5.6)。
            var omega = new Vector3(3f, 2f, -1f);
            Quat q = Quat.Identity;

            const float dt = 1f / 60f;
            for (int i = 0; i < 600; i++)
            {
                q = RotationIntegrator.Step(q, omega, dt, AngularVelocitySpace.World, IntegratorMethod.Euler, normalize: false);
            }

            AssertFinite(q, "正規化なし Euler法");
            Assert.That(Mathf.Abs(q.Norm - 1f), Is.GreaterThan(1e-3f),
                "Euler法・正規化なしで |q| が漂流していない (Ch.6 の演示が成立しない)");
        }

        [Test]
        public void Integrator_WithNormalization_StaysOnUnitSphere()
        {
            var omega = new Vector3(3f, 2f, -1f);
            Quat q = Quat.Identity;

            const float dt = 1f / 60f;
            for (int i = 0; i < 600; i++)
            {
                q = RotationIntegrator.Step(q, omega, dt, AngularVelocitySpace.World, IntegratorMethod.Euler, normalize: true);
            }

            AssertFinite(q, "正規化あり Euler法");
            Assert.That(q.Norm, Is.EqualTo(1f).Within(Tol), "正規化しても |q| が 1 に保たれていない");
        }

        [Test]
        public void Integrator_WorldAndBodySpace_DifferForOffAxisOmega()
        {
            // ワールド系とボディ系の違いが一目で分かることが Ch.6 の演示の要 (仕様書 5.6)。
            var omega = new Vector3(0f, 1.5f, 0f);
            Quat q0 = QuatMath.FromAxisAngle(Vector3.right, 80f * Mathf.Deg2Rad);

            Quat world = RotationIntegrator.Exact(q0, omega, 0.6f, AngularVelocitySpace.World);
            Quat body = RotationIntegrator.Exact(q0, omega, 0.6f, AngularVelocitySpace.Body);

            float absDot = Mathf.Abs(Quat.Dot(world, body));
            Assert.That(absDot, Is.LessThan(1f - Tol), "ワールド系とボディ系が同一の姿勢になってしまっている");
        }
    }
}
