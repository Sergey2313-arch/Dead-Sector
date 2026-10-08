using NUnit.Framework;
using UnityEngine;

namespace DeadSector.Tests
{
    public sealed class SectorJournalTests
    {
        [Test]
        public void JournalProgressRoundTripsThroughSnapshot()
        {
            GameObject first = new GameObject("JournalTest_Source");
            GameObject restored = new GameObject("JournalTest_Restored");
            try
            {
                SectorJournal a = first.AddComponent<SectorJournal>();
                a.RecordPickup();
                a.RecordPickup();
                a.RecordPickup();
                a.RecordCraft();
                a.RecordHarvest();
                a.RecordKill();

                SectorJournalSnapshot data = a.Export();
                Assert.AreEqual(3, data.suppliesTaken);
                Assert.AreEqual(1, data.itemsCrafted);

                SectorJournal b = restored.AddComponent<SectorJournal>();
                b.Import(data);

                SectorJournalSnapshot copy = b.Export();
                Assert.AreEqual(3, copy.suppliesTaken);
                Assert.AreEqual(1, copy.itemsCrafted);
                Assert.AreEqual(1, copy.resourceNodesHarvested);
                Assert.AreEqual(1, copy.zombiesKilled);
                Assert.AreEqual(1, b.CompletedCount);
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(restored);
            }
        }

        [Test]
        public void MissingJournalOnOldSaveStartsEmpty()
        {
            GameObject obj = new GameObject("JournalTest_Compat");
            try
            {
                SectorJournal journal = obj.AddComponent<SectorJournal>();
                journal.RecordKill();
                journal.Import(null);
                Assert.AreEqual(0, journal.Export().zombiesKilled);
                Assert.AreEqual(0, journal.CompletedCount);
            }
            finally
            {
                Object.DestroyImmediate(obj);
            }
        }
    }
}
