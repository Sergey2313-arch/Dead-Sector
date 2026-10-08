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

        readonly Color dayFog = new Color(.48f, .55f, .60f);
        readonly Color nightFog = new Color(.045f, .075f, .105f);
        readonly Color dayAmbient = new Color(.48f, .53f, .58f);
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
            RenderSettings.fogDensity = Mathf.Lerp(.0015f, .0009f, daylight);
        }
    }
}
