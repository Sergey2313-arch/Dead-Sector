using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadSector
{
    [Serializable]
    public sealed class SectorJournalSnapshot
    {
        public int suppliesTaken;
        public int resourceNodesHarvested;
        public int itemsCrafted;
        public int zombiesKilled;
        public List<string> visited = new List<string>();
    }

    /// <summary>
    /// Exploration and survival activity journal.
    /// J toggles a read-only quest panel without interrupting movement.
    /// The entries use actual POI coordinates, not illustrative map pixels.
    /// </summary>
    public sealed class SectorJournal : MonoBehaviour
    {
        public SectorPlayer player;

        SectorJournalSnapshot state = new SectorJournalSnapshot();
        readonly HashSet<string> visited =
            new HashSet<string>(StringComparer.Ordinal);

        bool visible;
        float nextScan;

        public bool ModernUiEnabled { get; set; }
        public bool Visible => visible;

        public void SetVisible(bool value)
        {
            visible = value;
        }

        public int SuppliesTaken => state.suppliesTaken;
        public int ResourcesHarvested => state.resourceNodesHarvested;
        public int ItemsCrafted => state.itemsCrafted;
        public int ZombiesKilled => state.zombiesKilled;

        public void Configure(SectorPlayer target)
        {
            player = target;
        }

        public void RecordPickup()
        {
            state.suppliesTaken++;
        }

        public void RecordHarvest()
        {
            state.resourceNodesHarvested++;
        }

        public void RecordCraft()
        {
            state.itemsCrafted++;
        }

        public void RecordKill()
        {
            state.zombiesKilled++;
        }

        public SectorJournalSnapshot Export()
        {
            var snapshot = new SectorJournalSnapshot
            {
                suppliesTaken = state.suppliesTaken,
                resourceNodesHarvested = state.resourceNodesHarvested,
                itemsCrafted = state.itemsCrafted,
                zombiesKilled = state.zombiesKilled,
                visited = new List<string>(visited)
            };

            return snapshot;
        }

        public void Import(SectorJournalSnapshot saved)
        {
            state = saved ?? new SectorJournalSnapshot();
            visited.Clear();

            if (state.visited != null)
            {
                foreach (string id in state.visited)
                    if (!string.IsNullOrEmpty(id))
                        visited.Add(id);
            }

            nextScan = 0f;
        }

        void Update()
        {
            if (player == null || !player.Ready)
                return;

            if (!ModernUiEnabled &&
                SectorInput.Pressed(KeyCode.J))
                visible = !visible;

            if (Time.time < nextScan)
                return;

            nextScan = Time.time + 1f;

            Vector3 p = player.transform.position;

            foreach (SectorMapPlan.Location poi in SectorMapPlan.Locations)
            {
                if (visited.Contains(poi.Id))
                    continue;

                float dx = p.x - poi.MapPosition.x;
                float dz = p.z - poi.MapPosition.y;

                if (dx * dx + dz * dz <= 110f * 110f)
                    visited.Add(poi.Id);
            }
        }

        public bool HasVisited(string id) => visited.Contains(id);

        public int CompletedCount
        {
            get
            {
                int count = 0;
                if (state.suppliesTaken >= 3) count++;
                if (state.resourceNodesHarvested >= 5) count++;
                if (state.itemsCrafted >= 2) count++;
                if (state.zombiesKilled >= 3) count++;
                if (visited.Contains("village")) count++;
                if (visited.Contains("clinic")) count++;
                if (visited.Contains("factory")) count++;
                if (visited.Contains("radio_station")) count++;
                return count;
            }
        }

        void OnGUI()
        {
            if (ModernUiEnabled || !visible ||
                player == null || !player.Ready)
                return;

            GUI.depth = -160;

            float width = Mathf.Min(375f, Screen.width - 20f);
            float height = 314f;
            float x = Mathf.Min(15f, Screen.width - width - 10f);
            float y = 186f;

            GUI.Box(new Rect(x, y, width, height),
                "DEAD SECTOR  /  FIELD JOURNAL [J]");

            float rowY = y + 32f;

            Entry(x, ref rowY, "Scout the village [B3]",
                HasVisited("village") ? "DONE" : "GO TO B3");
            Entry(x, ref rowY, "Find the medical clinic [E3]",
                HasVisited("clinic") ? "DONE" : "GO TO E3");
            Entry(x, ref rowY, "Search the industrial factory [D4]",
                HasVisited("factory") ? "DONE" : "GO TO D4");
            Entry(x, ref rowY, "Trace the radio signal [E1]",
                HasVisited("radio_station") ? "DONE" : "GO TO E1");

            Entry(x, ref rowY, "Collect supplies",
                Math.Min(3, state.suppliesTaken) + " / 3");
            Entry(x, ref rowY, "Gather field resources",
                Math.Min(5, state.resourceNodesHarvested) + " / 5");
            Entry(x, ref rowY, "Craft equipment or medicine",
                Math.Min(2, state.itemsCrafted) + " / 2");
            Entry(x, ref rowY, "Eliminate infected",
                Math.Min(3, state.zombiesKilled) + " / 3");

            GUI.Label(new Rect(x + 13f, y + height - 25f, width - 22f, 20f),
                "OBJECTIVES  " + CompletedCount + " / 8  |  NO QUEST TELEPORT");
        }

        void Entry(float x, ref float y, string name, string progress)
        {
            GUI.Label(new Rect(x + 12f, y, 263f, 25f), name);
            GUI.Label(new Rect(x + 271f, y, 90f, 25f), progress);
            y += 29f;
        }
    }
}
