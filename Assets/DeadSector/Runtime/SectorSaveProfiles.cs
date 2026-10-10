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
            slot == LegacySlot ? "СТАРОЕ СОХРАНЕНИЕ" :
            IsValidSlot(slot) ? "ПРОФИЛЬ " + slot.ToString("00") :
            "НЕВЕРНЫЙ";

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
                if (string.IsNullOrWhiteSpace(json) ||
                    !json.Contains("\"version\"") ||
                    !json.Contains("\"position\"") ||
                    !json.Contains("\"inventory\"") ||
                    !json.Contains("\"containers\""))
                    return false;

                SectorGameSave candidate =
                    JsonUtility.FromJson<SectorGameSave>(json);

                if (candidate == null || candidate.version != 1 ||
                    candidate.inventory == null ||
                    candidate.containers == null ||
                    float.IsNaN(candidate.health) ||
                    float.IsInfinity(candidate.health) ||
                    float.IsNaN(candidate.hourOfDay) ||
                    float.IsInfinity(candidate.hourOfDay) ||
                    float.IsNaN(candidate.position.x) ||
                    float.IsInfinity(candidate.position.x) ||
                    float.IsNaN(candidate.position.y) ||
                    float.IsInfinity(candidate.position.y) ||
                    float.IsNaN(candidate.position.z) ||
                    float.IsInfinity(candidate.position.z))
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

        /// <summary>
        /// Permanently remove a numbered, occupied save slot. Slot zero is
        /// the legacy V1 file and can never be deleted through this API.
        /// Callers must reject deleting the active profile before invoking it.
        /// The only associated files removed are the known backup and
        /// temporary file for the same fixed, validated slot path.
        /// A corrupt profile is intentionally deletable.
        /// </summary>
        public static bool TryDelete(string directory, int slot)
        {
            if (slot <= LegacySlot || slot > MaxProfileSlot ||
                string.IsNullOrEmpty(directory))
                return false;

            string path = FilePath(directory, slot);
            if (!File.Exists(path))
                return false;

            try
            {
                File.Delete(path);
                // File.Replace creates a backup on normal saves.
                // Do not leave a previous playthrough behind as .bak.
                string backup = path + ".bak";
                if (File.Exists(backup))
                    File.Delete(backup);

                string temporary = path + ".tmp";
                if (File.Exists(temporary))
                    File.Delete(temporary);

                return true;
            }
            catch (Exception error)
            {
                Debug.LogError("[Dead Sector Save] Cannot delete " +
                    Name(slot) + ": " + error.Message);
                return false;
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
                return "НЕВЕРНЫЙ СЛОТ";
            if (!Exists(directory, slot))
                return slot == LegacySlot
                    ? "НЕТ СТАРОГО СОХРАНЕНИЯ" : "ПУСТО / НОВАЯ ИГРА";

            if (!TryRead(directory, slot, out SectorGameSave save))
                return "ПОВРЕЖДЕНО / СОХРАНЕНО";

            return "ДЕНЬ " + (Math.Max(0, save.daysSurvived) + 1).ToString("00") +
                "   " + Mathf.Repeat(save.hourOfDay, 24f).ToString("00.0") + " Ч" +
                "   ЗДОРОВЬЕ " + Mathf.Clamp(save.health, 0f, 100f).ToString("0");
        }
    }
}
