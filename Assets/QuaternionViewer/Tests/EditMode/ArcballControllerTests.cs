using NUnit.Framework;
using QuaternionViewer.Core;
using QuaternionViewer.Input;
using UnityEngine;

namespace QuaternionViewer.Tests
{
    /// <summary>
    /// アークボールの球面写像 (仕様書 3.5) の検証。
    /// カーソルのレイ → 球面上の単位ベクトル p の写像と、
    /// 「掴んだ点がカーソルへ追従する」θ 版の合成を確かめる。
    /// </summary>
    public class ArcballControllerTests
    {
        private const float Eps = 1e-5f;

        [Test]
        public void MapToSphere_RayThroughCenter_HitsFrontOfSphere()
        {
            // 原点の球 (半径1.5) をカメラ位置 (0,0,-5) から +Z へ覗く → 手前の交点 (0,0,-1.5) が単位化されて返る
            Vector3 p = ArcballController.MapToSphere(
                new Vector3(0f, 0f, -5f), Vector3.forward, Vector3.zero, 1.5f);
            Assert.That(Vector3.Distance(p, Vector3.back), Is.LessThan(Eps));
        }

        [Test]
        public void MapToSphere_AlwaysReturnsUnitVector()
        {
            var origins = new[] { new Vector3(0f, 0f, -5f), new Vector3(2f, 1f, -4f), new Vector3(-3f, 2f, -6f) };
            var targets = new[] { Vector3.zero, new Vector3(0.4f, -0.2f, 0f), new Vector3(5f, 5f, 0f) };
            foreach (Vector3 o in origins)
            {
                foreach (Vector3 tgt in targets)
                {
                    Vector3 dir = (tgt - o).normalized;
                    Vector3 p = ArcballController.MapToSphere(o, dir, Vector3.zero, 1.5f);
                    Assert.That(p.magnitude, Is.EqualTo(1f).Within(Eps), $"o={o} tgt={tgt}");
                }
            }
        }

        [Test]
        public void MapToSphere_MissingRay_ProjectsToSilhouette()
        {
            // 球を大きく外すレイ → レイの最近接点方向のシルエット (中心直交面) へ射影される
            Vector3 p = ArcballController.MapToSphere(
                new Vector3(5f, 0f, -5f), Vector3.forward, Vector3.zero, 1.5f);
            Assert.That(Vector3.Distance(p, Vector3.right), Is.LessThan(Eps));
            // シルエット点はレイ方向と直交する
            Assert.That(Vector3.Dot(p, Vector3.forward), Is.EqualTo(0f).Within(Eps));
        }

        [Test]
        public void ArcballDrag_GrabbedPointFollowsCursor()
        {
            // θ 版アークボール: q = FromToRotation(p0, p1) は p0 を正確に p1 へ運ぶ (仕様書 3.5 の採用理由)
            Vector3 p0 = new Vector3(0.2f, -0.3f, -0.9f).normalized;
            Vector3 p1 = new Vector3(-0.5f, 0.4f, -0.7f).normalized;
            Quat q = QuatMath.FromToRotation(p0, p1);
            Vector3 moved = q * p0;
            Assert.That(Vector3.Distance(moved, p1), Is.LessThan(Eps));
        }

        [Test]
        public void ArcballDrag_ComposesOnTopOfStartPose()
        {
            // ドラッグ適用は世界系の左乗 q_drag ⊗ q_start ―― 掴み点の追従が姿勢の初期値に依存しない
            Quat start = QuatMath.FromAxisAngle(new Vector3(1f, 2f, 0.5f), 2.0944f); // 120°
            Vector3 p0 = new Vector3(0f, 0.6f, -0.8f).normalized;
            Vector3 p1 = new Vector3(0.3f, 0.1f, -0.948f).normalized;
            Quat composed = QuatMath.FromToRotation(p0, p1) * start;
            // 合成後も単位四元数のまま
            Assert.That(Mathf.Abs(composed.Norm - 1f), Is.LessThan(1e-4f));
        }
    }
}
