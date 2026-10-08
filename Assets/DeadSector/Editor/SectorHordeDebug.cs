using UnityEditor;
using UnityEngine;

namespace DeadSector.Editor
{
    /// <summary>
    /// Play Mode-only helper for testing day progression and hordes
    /// without waiting a full in-game day.
    /// </summary>
    public static class SectorHordeDebug
    {
        [MenuItem("Dead Sector/Debug/Prepare Night Horde")]
        public static void PrepareNightHorde()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("[Dead Sector] Enter Play Mode first.");
                return;
            }

            SectorWorldClock clock =
                Object.FindFirstObjectByType<SectorWorldClock>();
            SectorHordeDirector horde =
                Object.FindFirstObjectByType<SectorHordeDirector>();

            if (clock == null || horde == null)
            {
                Debug.LogWarning("[Dead Sector] World clock or horde director missing.");
                return;
            }

            int daysSurvived = Mathf.Max(0, clock.DaysSurvived);
            if (horde.LastTriggeredDay == daysSurvived + 1)
                daysSurvived++;

            clock.RestoreTime(20.80f, daysSurvived);
            Debug.Log("[Dead Sector] Horde warning will appear now; " +
                "the attack starts when game time reaches 21:00. " +
                "Day " + clock.DayNumber);
        }

        [MenuItem("Dead Sector/Debug/Add One Survived Day")]
        public static void AdvanceDay()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("[Dead Sector] Enter Play Mode first.");
                return;
            }

            SectorWorldClock clock =
                Object.FindFirstObjectByType<SectorWorldClock>();

            if (clock == null)
                return;

            clock.RestoreTime(
                clock.hourOfDay, clock.DaysSurvived + 1);

            Debug.Log("[Dead Sector] Day " + clock.DayNumber);
        }
    }
}
