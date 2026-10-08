using NUnit.Framework;
using UnityEngine;

namespace DeadSector.Tests
{
    /// <summary>
    /// Regression checks for the live Unity screenshot issues:
    /// inverted gabled roofs and Mixamo body scaling.
    /// </summary>
    public sealed class SectorWorldVisualRegressionTests
    {
        [Test]
        public void LeftAndRightRoofsRiseTowardTheRidge()
        {
            const float pitch = 28f;
            float leftRotation = SectorRoofGeometry.PanelRotationZ(-1, pitch);
            float rightRotation = SectorRoofGeometry.PanelRotationZ(1, pitch);

            Assert.Greater(leftRotation, 0f);
            Assert.Less(rightRotation, 0f);

            Vector3 halfPanel = Vector3.right * 2f;
            Vector3 leftInward = Quaternion.Euler(0f, 0f, leftRotation) *
                halfPanel;
            Vector3 rightInward = Quaternion.Euler(0f, 0f, rightRotation) *
                -halfPanel;

            Assert.Greater(leftInward.y, 0f,
                "Left roof must rise toward center.");
            Assert.Greater(rightInward.y, 0f,
                "Right roof must rise toward center.");
            Assert.Greater(SectorRoofGeometry.Rise(10f, pitch), 0f);
        }

        [Test]
        public void StandardSizedCharacterNeedsAlmostNoScaling()
        {
            Assert.AreEqual(1f,
                SectorPlayer.BodyScaleForHeight(1.76f), .001f);
        }

        [Test]
        public void UndersizedMixamoBodyIsScaledUpAndMalformedBoundsIgnored()
        {
            Assert.Greater(SectorPlayer.BodyScaleForHeight(.55f), 2f);
            Assert.AreEqual(1f, SectorPlayer.BodyScaleForHeight(0f));
            Assert.AreEqual(1f, SectorPlayer.BodyScaleForHeight(101f));
        }
    }
}
