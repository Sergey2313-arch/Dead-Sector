using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace DeadSector.Tests
{
    public sealed class SectorSaveProfilesTests
    {
        string sandbox;

        [SetUp]
        public void SetUp()
        {
            sandbox = Path.Combine(Path.GetTempPath(),
                "DeadSectorSaveTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(sandbox);
        }

        [TearDown]
        public void TearDown()
        {
            // This directory is uniquely created by the current test.
            // Never delete any user's actual persistentDataPath.
            if (!string.IsNullOrEmpty(sandbox) &&
                Directory.Exists(sandbox))
                Directory.Delete(sandbox, true);
        }

        [Test]
        public void OriginalLegacySaveIsNotMigratedOverwrittenOrDeleted()
        {
            string legacy = SectorSaveProfiles.FilePath(sandbox, 0);
            string original = "{\"version\":1,\"inventory\":[],\"containers\":[],\"health\":54,\"hourOfDay\":15}";
            File.WriteAllText(legacy, original);

            Assert.IsTrue(SectorSaveProfiles.TryCreateNew(
                sandbox, 1, SectorSaveProfiles.Starter()));
            Assert.IsTrue(SectorSaveProfiles.TryCreateNew(
                sandbox, 2, SectorSaveProfiles.Starter()));

            Assert.AreEqual(original, File.ReadAllText(legacy));
            Assert.AreNotEqual(legacy, SectorSaveProfiles.FilePath(sandbox, 1));
            Assert.AreNotEqual(
                SectorSaveProfiles.FilePath(sandbox, 1),
                SectorSaveProfiles.FilePath(sandbox, 2));
            Assert.IsFalse(SectorSaveProfiles.TryCreateNew(
                sandbox, 0, SectorSaveProfiles.Starter()));
            Assert.AreEqual(original, File.ReadAllText(legacy));
        }

        [Test]
        public void OccupiedSaveSlotCanNeverBeResetByNewGame()
        {
            SectorGameSave starter = SectorSaveProfiles.Starter();

            Assert.IsTrue(SectorSaveProfiles.TryCreateNew(
                sandbox, 1, starter));
            string file = SectorSaveProfiles.FilePath(sandbox, 1);
            string firstData = File.ReadAllText(file);

            Assert.IsFalse(SectorSaveProfiles.TryCreateNew(
                sandbox, 1, starter));
            Assert.AreEqual(firstData, File.ReadAllText(file));
        }

        [Test]
        public void AllSlotsHaveFixedPathsWithoutUserProvidedNames()
        {
            Assert.AreEqual("DeadSector_Save_v1.json",
                Path.GetFileName(SectorSaveProfiles.FilePath(sandbox, 0)));
            Assert.AreEqual("DeadSector_Profile_01_v1.json",
                Path.GetFileName(SectorSaveProfiles.FilePath(sandbox, 1)));
            Assert.AreEqual("DeadSector_Profile_02_v1.json",
                Path.GetFileName(SectorSaveProfiles.FilePath(sandbox, 2)));
            Assert.AreEqual("DeadSector_Profile_03_v1.json",
                Path.GetFileName(SectorSaveProfiles.FilePath(sandbox, 3)));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SectorSaveProfiles.FilePath(sandbox, 4));
            Assert.IsFalse(SectorSaveProfiles.TryCreateNew(
                sandbox, 4, SectorSaveProfiles.Starter()));
        }

        [Test]
        public void DeletingOneProfileLeavesLegacyAndOtherSlotsUntouched()
        {
            string legacy = SectorSaveProfiles.FilePath(sandbox, 0);
            File.WriteAllText(legacy, "ORIGINAL V1");
            Assert.IsTrue(SectorSaveProfiles.TryCreateNew(
                sandbox, 1, SectorSaveProfiles.Starter()));
            Assert.IsTrue(SectorSaveProfiles.TryCreateNew(
                sandbox, 2, SectorSaveProfiles.Starter()));
            Assert.IsTrue(SectorSaveProfiles.TryCreateNew(
                sandbox, 3, SectorSaveProfiles.Starter()));

            string first = SectorSaveProfiles.FilePath(sandbox, 1);
            string second = SectorSaveProfiles.FilePath(sandbox, 2);
            string third = SectorSaveProfiles.FilePath(sandbox, 3);
            string firstContent = File.ReadAllText(first);
            string thirdContent = File.ReadAllText(third);
            File.WriteAllText(second + ".bak", "OLDER CHECKPOINT");
            File.WriteAllText(second + ".tmp", "INTERRUPTED CHECKPOINT");

            Assert.IsTrue(SectorSaveProfiles.TryDelete(sandbox, 2));
            Assert.IsFalse(File.Exists(second));
            Assert.IsFalse(File.Exists(second + ".bak"));
            Assert.IsFalse(File.Exists(second + ".tmp"));
            Assert.IsTrue(File.Exists(first));
            Assert.IsTrue(File.Exists(third));
            Assert.AreEqual(firstContent, File.ReadAllText(first));
            Assert.AreEqual(thirdContent, File.ReadAllText(third));
            Assert.AreEqual("ORIGINAL V1", File.ReadAllText(legacy));
            Assert.IsTrue(SectorSaveProfiles.TryCreateNew(
                sandbox, 2, SectorSaveProfiles.Starter()),
                "A deleted profile must be available as a new game slot.");
        }

        [Test]
        public void ProfileDeletionRefusesLegacyInvalidAndEmptySlots()
        {
            string legacy = SectorSaveProfiles.FilePath(sandbox, 0);
            File.WriteAllText(legacy, "DO NOT DELETE LEGACY");

            Assert.IsFalse(SectorSaveProfiles.TryDelete(sandbox, 0));
            Assert.IsFalse(SectorSaveProfiles.TryDelete(sandbox, -1));
            Assert.IsFalse(SectorSaveProfiles.TryDelete(sandbox, 4));
            Assert.IsFalse(SectorSaveProfiles.TryDelete(sandbox, 1));
            Assert.IsFalse(SectorSaveProfiles.TryDelete("", 1));
            Assert.AreEqual("DO NOT DELETE LEGACY",
                File.ReadAllText(legacy));
        }

        [Test]
        public void CorruptNonActiveProfileCanBeRemovedAfterConfirmation()
        {
            string corruptPath = SectorSaveProfiles.FilePath(sandbox, 3);
            File.WriteAllText(corruptPath, "{broken profile");
            Assert.IsTrue(SectorSaveProfiles.Exists(sandbox, 3));
            Assert.IsFalse(SectorSaveProfiles.TryRead(sandbox, 3, out _));
            Assert.IsTrue(SectorSaveProfiles.TryDelete(sandbox, 3));
            Assert.IsFalse(SectorSaveProfiles.Exists(sandbox, 3));
        }

        [Test]
        public void FreshProfileHasProperUnarmedStarterInventory()
        {
            SectorGameSave starter = SectorSaveProfiles.Starter();
            Assert.AreEqual(1, starter.version);
            Assert.AreEqual(100f, starter.health, .001f);
            Assert.AreEqual(100f, starter.stamina, .001f);
            Assert.AreEqual(0, starter.daysSurvived);
            Assert.AreEqual(12f, starter.hourOfDay, .001f);
            Assert.AreEqual("", starter.primary);
            Assert.AreEqual("", starter.melee);

            Assert.IsTrue(SectorSaveProfiles.TryCreateNew(
                sandbox, 3, starter));
            Assert.IsTrue(SectorSaveProfiles.TryRead(
                sandbox, 3, out SectorGameSave restored));
            var inventory = new SectorInventory();
            inventory.Import(restored.inventory);
            Assert.AreEqual(1, inventory.Count("water"));
            Assert.AreEqual(2, inventory.Count("bandage"));
        }

        [Test]
        public void CorruptOrUnsupportedSlotIsNeverTreatedAsEmpty()
        {
            string slot = SectorSaveProfiles.FilePath(sandbox, 2);
            File.WriteAllText(slot, "{corrupted json");

            Assert.IsTrue(SectorSaveProfiles.Exists(sandbox, 2));
            Assert.IsFalse(SectorSaveProfiles.TryRead(
                sandbox, 2, out _));
            Assert.IsFalse(SectorSaveProfiles.TryCreateNew(
                sandbox, 2, SectorSaveProfiles.Starter()));
            Assert.AreEqual("{corrupted json", File.ReadAllText(slot));
            StringAssert.Contains("ПОВРЕЖДЕНО",
                SectorSaveProfiles.Description(sandbox, 2));

            string other = SectorSaveProfiles.FilePath(sandbox, 1);
            File.WriteAllText(other,
                "{\"version\":999,\"inventory\":[],\"containers\":[]}");
            Assert.IsFalse(SectorSaveProfiles.TryRead(sandbox, 1,
                out _));
        }
    }
}
