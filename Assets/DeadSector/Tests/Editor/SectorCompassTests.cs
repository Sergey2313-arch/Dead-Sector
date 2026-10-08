using NUnit.Framework;
using UnityEngine;

namespace DeadSector.Tests
{
    public sealed class SectorCompassTests
    {
        [Test]
        public void CardinalBearingsFollowUnityWorldAxes()
        {
            Assert.AreEqual(0f, SectorCompass.Bearing(Vector3.zero, Vector3.forward), .001f);
            Assert.AreEqual(90f, SectorCompass.Bearing(Vector3.zero, Vector3.right), .001f);
            Assert.AreEqual(180f, SectorCompass.Bearing(Vector3.zero, Vector3.back), .001f);
            Assert.AreEqual(270f, SectorCompass.Bearing(Vector3.zero, Vector3.left), .001f);
        }

        [Test]
        public void RelativeBearingsWrapAcrossNorthWithoutJumps()
        {
            Assert.AreEqual(20f, SectorCompass.RelativeAngle(350f, 10f), .001f);
            Assert.AreEqual(-20f, SectorCompass.RelativeAngle(10f, 350f), .001f);
            Assert.AreEqual(0f, SectorCompass.RelativeAngle(90f, 450f), .001f);
        }

        [Test]
        public void CardinalLabelsWrapAndRejectIntermediateTicks()
        {
            Assert.AreEqual("N", SectorCompass.CardinalName(0));
            Assert.AreEqual("NE", SectorCompass.CardinalName(45));
            Assert.AreEqual("S", SectorCompass.CardinalName(180));
            Assert.AreEqual("NW", SectorCompass.CardinalName(-45));
            Assert.AreEqual("N", SectorCompass.CardinalName(360));
            Assert.IsNull(SectorCompass.CardinalName(15));
        }
    }
}
