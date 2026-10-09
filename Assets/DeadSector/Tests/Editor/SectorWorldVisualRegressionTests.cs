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
        public void FirstPersonHidesOnlyPlayerButKeepsZombiesVisible()
        {
            Assert.AreNotEqual(
                SectorArt.PlayerVisualLayer, SectorArt.EnemyVisualLayer);
            const int originalMask = ~0;
            int fpp = SectorPlayer.CullingMaskForView(originalMask, false);
            int tpp = SectorPlayer.CullingMaskForView(fpp, true);

            Assert.AreEqual(0,
                fpp & (1 << SectorArt.PlayerVisualLayer));
            Assert.AreNotEqual(0,
                fpp & (1 << SectorArt.EnemyVisualLayer));
            Assert.AreNotEqual(0,
                tpp & (1 << SectorArt.EnemyVisualLayer));
            Assert.AreNotEqual(0,
                tpp & (1 << SectorArt.PlayerVisualLayer));

            var root = new GameObject("ZombieVisual");
            var child = new GameObject("ZombieBody");
            try
            {
                child.transform.SetParent(root.transform, false);
                SectorArt.SetActorLayer(root.transform, SectorArt.EnemyVisualLayer);
                Assert.AreEqual(SectorArt.EnemyVisualLayer, root.layer);
                Assert.AreEqual(SectorArt.EnemyVisualLayer, child.layer);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void TerrainGrassAvoidsBuildingCenters()
        {
            Assert.IsFalse(SectorWorld.CanGrowGrass(0f, 0f),
                "Never place billboard grass inside the starting settlement.");
        }

        [Test]
        public void ResidentialDoorAndPorchAreAccessibleToHumanPlayer()
        {
            Assert.That(SectorPlayer.StandingHeight, Is.InRange(1.65f, 1.90f));
            Assert.GreaterOrEqual(
                SectorWorld.ResidentialDoorWidth,
                SectorPlayer.StandingRadius * 2f + .7f,
                "The doorway must have room for a capsule and shoulder clearance.");
            // Door height is measured from the FINISHED FLOOR, not terrain.
            // Previously the roof lintel was placed only 2.22m above terrain,
            // resulting in 1.80m headroom above the raised 0.42m floor.
            Assert.That(SectorWorld.ResidentialDoorHeight,
                Is.InRange(2.05f, 2.25f));
            Assert.Greater(
                SectorWorld.ResidentialDoorHeight,
                SectorPlayer.StandingHeight + .25f);
            Assert.That(
                SectorWorld.ResidentialFloorTop + SectorWorld.ResidentialDoorHeight,
                Is.InRange(2.50f, 2.75f),
                "Lintel must include raised finished-floor elevation.");
            Assert.Less(
                SectorWorld.ResidentialFloorTop,
                SectorWorld.ResidentialPorchTop + .05f);
            Assert.LessOrEqual(SectorWorld.ResidentialStepTop, .30f);
            Assert.LessOrEqual(
                SectorWorld.ResidentialPorchTop - SectorWorld.ResidentialStepTop,
                .30f, "Both porch rises must be below CharacterController stepOffset.");
        }

        [Test]
        public void HingedDoorCanBeFoundThroughPhysicalDoorLeaf()
        {
            var root = new GameObject("DoorRegressionRoot");
            try
            {
                SectorDoor door = root.AddComponent<SectorDoor>();
                var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slab.transform.SetParent(root.transform, false);
                Assert.AreSame(door,
                    slab.GetComponent<BoxCollider>().GetComponentInParent<SectorDoor>());

                Assert.IsFalse(door.IsOpen);
                door.Toggle();
                Assert.IsTrue(door.IsOpen);
                door.Toggle();
                Assert.IsFalse(door.IsOpen);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RussianItemNamesKeepOriginalStableSaveIds()
        {
            SectorItemDefinition water = SectorItems.Get("water");
            Assert.AreEqual("water", water.Id);
            StringAssert.Contains("Вода", water.Label);
            Assert.AreEqual("Бинт", SectorItems.Get("bandage").Label);
            StringAssert.Contains("нож", SectorRussian.ItemName("knife"));
            Assert.AreEqual("Деревянная стена",
                SectorBuildCatalog.At((int)SectorBuildKind.Wall).Label);
        }

        [Test]
        public void CraftingOutputsHaveRenderableItemIcons()
        {
            try
            {
                foreach (SectorRecipe recipe in SectorCrafting.Recipes)
                {
                    Sprite icon = SectorItemIcons.Get(recipe.OutputId);
                    Assert.IsNotNull(icon, "Missing icon for " + recipe.OutputId);
                    var pixels = icon.texture.GetPixels32();
                    bool visible = false;
                    foreach (Color32 pixel in pixels)
                    {
                        if (pixel.a == 0) continue;
                        visible = true;
                        break;
                    }
                    Assert.IsTrue(visible, "Transparent icon for " + recipe.OutputId);
                }
            }
            finally
            {
                SectorItemIcons.Release();
            }
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
