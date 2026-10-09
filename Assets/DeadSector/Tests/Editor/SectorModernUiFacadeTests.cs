using NUnit.Framework;
using UnityEngine;

namespace DeadSector.Tests
{
    public sealed class SectorModernUiFacadeTests
    {
        [Test]
        public void CanvasInterfaceAndDragHandlersArePresent()
        {
            Assert.IsTrue(typeof(MonoBehaviour).IsAssignableFrom(
                typeof(SectorModernUI)));
            Assert.IsNotNull(typeof(SectorBagDragSource)
                .GetInterface("IBeginDragHandler"));
            Assert.IsNotNull(typeof(SectorBagDragSource)
                .GetInterface("IEndDragHandler"));
            Assert.IsNotNull(typeof(SectorGearDropTarget)
                .GetInterface("IDropHandler"));
        }

        [Test]
        public void CraftButtonUsesSameTransactionalInventoryAsLegacyGameplay()
        {
            GameObject host = new GameObject("UiFacadeTests");
            try
            {
                SectorGameplay gameplay =
                    host.AddComponent<SectorGameplay>();
                Assert.IsTrue(gameplay.Inventory.Add("plant_fiber", 3));
                Assert.AreEqual(3, gameplay.Inventory.Count("plant_fiber"));

                Assert.IsTrue(gameplay.CraftRecipe("cord"));
                Assert.AreEqual(0, gameplay.Inventory.Count("plant_fiber"));
                Assert.AreEqual(1, gameplay.Inventory.Count("cord"));

                Assert.IsFalse(gameplay.CraftRecipe("cord"));
                Assert.AreEqual(1, gameplay.Inventory.Count("cord"));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void InvalidHotbarSelectionKeepsPreviousValidIndex()
        {
            GameObject host = new GameObject("UiWeaponTests");
            try
            {
                SectorGameplay gameplay =
                    host.AddComponent<SectorGameplay>();
                gameplay.SelectWeaponSlot(1);
                Assert.AreEqual(1, gameplay.ActiveWeaponSlot);
                gameplay.SelectWeaponSlot(999);
                Assert.AreEqual(1, gameplay.ActiveWeaponSlot);
                Assert.AreEqual("", gameplay.WeaponInSlot(999));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }
    }
}
