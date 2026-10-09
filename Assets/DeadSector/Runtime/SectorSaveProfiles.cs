using System;
using System.IO;
using UnityEngine;

namespace DeadSector
{
    /// <summary>
    /// Four deliberately isolated save locations:
    /// 0 is the untouched original V1 file; 1..3 are new playthroughs.
    /// These helpers never delete, rename or migrate the legacy file.
    /// </summary>
    public static class SectorSaveProfiles
    {
        public const int LegacySlot = 0;
        public const int MaxProfileSlot = 3;
        public const string LegacyFileName = "DeadSector_Save_v1.json";

        public static bool IsValidSlot(int slot) =>
            slot >= LegacySlot && slot <= MaxProfileSlot;

        public static string Name(int slot) =>
            slot == LegacySlot ? "LEGACY / OLD SAVE" :
            IsValidSlot(slot) ? "PROFILE " + slot.ToString("00") :
            "INVALID";

        public static string FilePath(string directory, int slot)
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentException(
                    "Save directory must be specified.", nameof(directory));
            if (!IsValidSlot(slot))
                throw new ArgumentOutOfRangeException(nameof(slot));

            return Path.Combine(directory, slot == LegacySlot
                ? LegacyFileName
                : "DeadSector_Profile_" + slot.ToString("00") + "_v1.json");
        }

        public static bool Exists(string directory, int slot) =>
            IsValidSlot(slot) && File.Exists(FilePath(directory, slot));

        public static bool TryRead(
            string directory, int slot, out SectorGameSave saved)
        {
            saved = null;
            if (!Exists(directory, slot))
                return false;

            try
            {
                string json = File.ReadAllText(FilePath(directory, slot));
                if (string.IsNullOrWhiteSpace(json))
                    return false;
                SectorGameSave candidate =
                    JsonUtility.FromJson<SectorGameSave>(json);

                if (candidate == null || candidate.version != 1 ||
                    candidate.inventory == null ||
                    candidate.containers == null ||
                    float.IsNaN(candidate.health) ||
                    float.IsInfinity(candidate.health) ||
                    float.IsNaN(candidate.hourOfDay) ||
                    float.IsInfinity(candidate.hourOfDay))
                    return false;

                saved = candidate;
                return true;
            }
            catch (Exception error)
            {
                Debug.LogWarning("[Dead Sector Save] Cannot read " +
                    Name(slot) + ": " + error.Message);
                return false;
            }
        }

        /// <summary>
        /// Create a new file only if the slot is EMPTY. Move never uses
        /// overwrite semantics. If the process fails, legacy and occupied
        /// profiles are not touched.
        /// </summary>
        public static bool TryCreateNew(
            string directory, int slot, SectorGameSave starter)
        {
            if (slot <= LegacySlot || slot > MaxProfileSlot ||
                starter == null || starter.version != 1)
                return false;

            string destination = FilePath(directory, slot);
            if (File.Exists(destination))
                return false;

            string temporary = destination + "." +
                Guid.NewGuid().ToString("N") + ".tmp";

            try
            {
                Directory.CreateDirectory(directory);
                // FileMode.CreateNew avoids replacing any existing temp.
                using (var stream = new FileStream(temporary,
                    FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream))
                    writer.Write(JsonUtility.ToJson(starter, true));

                // File.Move throws when the target already exists.
                File.Move(temporary, destination);
                return true;
            }
            catch (Exception error)
            {
                Debug.LogError("[Dead Sector Save] Profile creation failed: " +
                    error.Message);
                return false;
            }
            finally
            {
                // Only a uniquely generated temporary file is cleaned up.
                if (File.Exists(temporary))
                {
                    try { File.Delete(temporary); }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
            }
        }

        public static SectorGameSave Starter()
        {
            return new SectorGameSave
            {
                version = 1,
                hourOfDay = 12f,
                daysSurvived = 0,
                position = SectorLayout.Spawn,
                health = 100f,
                hunger = 100f,
                thirst = 100f,
                stamina = 100f,
                primary = "",
                secondary = "",
                melee = "",
                selectedSlot = 2,
                armor = new string[5],
                inventory = new System.Collections.Generic.List<SectorItemStack>
                {
                    new SectorItemStack("water", 1),
                    new SectorItemStack("bandage", 2)
                },
                containers =
                    new System.Collections.Generic.List<SectorContainerSnapshot>(),
                harvestedResourceIds =
                    new System.Collections.Generic.List<string>(),
                journal = new SectorJournalSnapshot(),
                horde = new SectorHordeSnapshot()
            };
        }

        public static string Description(string directory, int slot)
        {
            if (!IsValidSlot(slot))
                return "INVALID SLOT";
            if (!Exists(directory, slot))
                return slot == LegacySlot
                    ? "NO LEGACY SAVE" : "EMPTY / NEW GAME";

            if (!TryRead(directory, slot, out SectorGameSave save))
                return "UNREADABLE / PRESERVED";

            return "DAY " + (Math.Max(0, save.daysSurvived) + 1).ToString("00") +
                "   " + Mathf.Repeat(save.hourOfDay, 24f).ToString("00.0") + " H" +
                "   HP " + Mathf.Clamp(save.health, 0f, 100f).ToString("0");
        }
    }
}
