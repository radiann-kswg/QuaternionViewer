using NUnit.Framework;
using QuaternionViewer.Core;
using QuaternionViewer.Visualization;
using UnityEngine;

namespace QuaternionViewer.Tests
{
    /// <summary>二体サイコロの逐次適用 (仕様書 5.3 ―― 非可換性の演示) の検証。</summary>
    public class TwinDiceRigTests
    {
        private const float AngleRad = 90f * Mathf.Deg2Rad;

        [Test]
        public void PoseFor_CompletedRun_MatchesCompositionOrder()
        {
            // 完了時 (t=2) は q_second ⊗ q_first (仕様書 2.4)
            Quat pose = TwinDiceRig.PoseFor(Vector3.right, Vector3.up, AngleRad, 2f);
            Quat expected = QuatMath.FromAxisAngle(Vector3.up, AngleRad)
                            * QuatMath.FromAxisAngle(Vector3.right, AngleRad);
            Assert.That(QuatMath.Angle(pose, expected) * Mathf.Rad2Deg, Is.LessThan(1e-3f));
        }

        [Test]
        public void PoseFor_OrderSwap_YieldsDifferentAttitude()
        {
            // X→Y と Y→X で出目 (姿勢) が食い違う ―― 非可換性そのもの
            Quat xy = TwinDiceRig.PoseFor(Vector3.right, Vector3.up, AngleRad, 2f);
            Quat yx = TwinDiceRig.PoseFor(Vector3.up, Vector3.right, AngleRad, 2f);
            Assert.That(QuatMath.Angle(xy, yx) * Mathf.Rad2Deg, Is.GreaterThan(10f));
        }

        [Test]
        public void PoseFor_Midway_AppliesOnlyFirstRotation()
        {
            Quat half = TwinDiceRig.PoseFor(Vector3.right, Vector3.up, AngleRad, 1f);
            Quat firstOnly = QuatMath.FromAxisAngle(Vector3.right, AngleRad);
            Assert.That(QuatMath.Angle(half, firstOnly) * Mathf.Rad2Deg, Is.LessThan(1e-3f));
        }
    }
}
