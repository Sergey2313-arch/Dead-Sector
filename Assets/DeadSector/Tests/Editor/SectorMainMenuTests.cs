using NUnit.Framework;

namespace DeadSector.Tests
{
    public sealed class SectorMainMenuTests
    {
        [Test]
        public void EscapeClosesSettingsBeforeResumingGame()
        {
            Assert.AreEqual(
                SectorEscapeAction.CloseSettings,
                SectorMenuRules.OnEscape(true, true, false, false, false));
            Assert.AreEqual(
                SectorEscapeAction.Resume,
                SectorMenuRules.OnEscape(true, false, false, false, false));
        }

        [Test]
        public void EscapeClosesInventoryOrCraftingBeforeOpeningPause()
        {
            Assert.AreEqual(
                SectorEscapeAction.CloseInventory,
                SectorMenuRules.OnEscape(false, false, true, false, false));
            Assert.AreEqual(
                SectorEscapeAction.CloseInventory,
                SectorMenuRules.OnEscape(false, false, false, true, false));
            Assert.AreEqual(
                SectorEscapeAction.CloseInventory,
                SectorMenuRules.OnEscape(false, false, true, false, true));
        }

        [Test]
        public void EscapeClosesAtlasBeforeOpeningPause()
        {
            Assert.AreEqual(
                SectorEscapeAction.CloseAtlas,
                SectorMenuRules.OnEscape(false, false, false, false, true));
            Assert.AreEqual(
                SectorEscapeAction.OpenPause,
                SectorMenuRules.OnEscape(false, false, false, false, false));
        }

        [Test]
        public void MouseSensitivityClampsAndRestoresWithoutLosingPrevious()
        {
            float original = SectorInput.LookSensitivity;
            try
            {
                SectorInput.LookSensitivity = -50f;
                Assert.AreEqual(.35f, SectorInput.LookSensitivity, .001f);

                SectorInput.LookSensitivity = 15f;
                Assert.AreEqual(2.5f, SectorInput.LookSensitivity, .001f);

                SectorInput.LookSensitivity = 1.25f;
                Assert.AreEqual(1.25f, SectorInput.LookSensitivity, .001f);
            }
            finally
            {
                SectorInput.LookSensitivity = original;
            }
        }

        [Test]
        public void MainMenuIsASeparateRuntimeCanvasComponent()
        {
            Assert.IsTrue(typeof(UnityEngine.MonoBehaviour)
                .IsAssignableFrom(typeof(SectorMenuUI)));
        }
    }
}
