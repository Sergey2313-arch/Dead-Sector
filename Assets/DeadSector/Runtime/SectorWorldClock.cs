using UnityEngine;
using UnityEngine.Rendering;

namespace DeadSector
{
    /// <summary>
    /// Shared world clock for prototype day/night lighting. Gameplay time
    /// is deterministic per saved hour; network-host authority comes later.
    /// </summary>
    public sealed class SectorWorldClock : MonoBehaviour
    {
        public Light sun;

        [Range(0f, 24f)]
        public float hourOfDay = 12f;

        [Min(10f)]
        public float dayLengthMinutes = 60f;

        public bool progressTime = true;
        public int DaysSurvived { get; private set; }
        public int DayNumber => DaysSurvived + 1;

        // Distant ground used to fade into bright blue-grey, looking like
        // a giant lake even in dry inland regions.
        readonly Color dayFog = new Color(.43f, .47f, .43f);
        readonly Color nightFog = new Color(.052f, .064f, .073f);
        readonly Color dayAmbient = new Color(.49f, .50f, .44f);
        readonly Color nightAmbient = new Color(.075f, .09f, .125f);

        void Update()
        {
            if (progressTime)
            {
                float hourAdvance = Time.deltaTime * 24f /
                    (Mathf.Max(10f, dayLengthMinutes) * 60f);
                float progressed = hourOfDay + hourAdvance;
                if (progressed >= 24f)
                    DaysSurvived += Mathf.FloorToInt(progressed / 24f);

                hourOfDay = Mathf.Repeat(progressed, 24f);
            }

            ApplyLighting();
        }

        public void RestoreTime(float savedHour, int daysSurvived = 0)
        {
            hourOfDay = Mathf.Repeat(savedHour, 24f);
            DaysSurvived = Mathf.Max(0, daysSurvived);
            ApplyLighting();
        }

        // Kept independent of current weather for predictable time-of-day
        // fog and an inexpensive EditMode regression check.
        public static float BaseFogDensity(float daylight)
        {
            return Mathf.Lerp(.0011f, .00038f, Mathf.Clamp01(daylight));
        }

        void ApplyLighting()
        {
            float sunHeight = Mathf.Sin(
                (hourOfDay - 6f) * Mathf.PI / 12f);
            float daylight = Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(-.12f, .40f, sunHeight));

            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(
                    hourOfDay * 15f - 90f, -35f, 0f);

                sun.intensity = Mathf.Lerp(.025f, 1.40f, daylight);
                sun.color = Color.Lerp(
                    new Color(.59f, .73f, 1f),
                    new Color(1f, .95f, .86f), daylight);
            }

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight =
                Color.Lerp(nightAmbient, dayAmbient, daylight);
            RenderSettings.fogColor =
                Color.Lerp(nightFog, dayFog, daylight);
            RenderSettings.fogDensity = BaseFogDensity(daylight);
        }
    }
}
