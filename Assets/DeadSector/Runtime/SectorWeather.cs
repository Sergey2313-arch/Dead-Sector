using UnityEngine;

namespace DeadSector
{
    public enum SectorWeatherKind
    {
        Clear,
        Overcast,
        Rain,
        Mist
    }

    /// <summary>
    /// First weather pass: fog, atmosphere and local rain particles.
    /// Uses the already-saved game-world clock so load restores the same
    /// weather interval. Audio and wet surface shaders come in art polish.
    /// </summary>
    public sealed class SectorWeather : MonoBehaviour
    {
        public SectorPlayer player;
        public SectorWorldClock clock;

        public SectorWeatherKind Weather { get; private set; }
        public string WeatherLabel => Weather.ToString();

        ParticleSystem rain;
        Material rainMaterial;
        public bool ModernUiEnabled { get; set; }
        SectorWeatherKind lastWeather = (SectorWeatherKind)(-1);
        readonly Color weatherFog = new Color(.29f, .34f, .37f);

        public void Configure(SectorPlayer target, SectorWorldClock time)
        {
            player = target;
            clock = time;
            BuildRain();
        }

        void BuildRain()
        {
            if (rain != null)
                return;

            GameObject obj = new GameObject("Local_Weather_Rain");
            obj.transform.SetParent(transform, false);
            obj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            rain = obj.AddComponent<ParticleSystem>();
            rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = rain.main;
            main.loop = true;
            main.duration = 2f;
            main.startLifetime = 1.15f;
            main.startSpeed = 21f;
            main.startSize = .018f;
            main.maxParticles = 650;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new Color(.7f, .8f, .92f, .26f);

            ParticleSystem.EmissionModule emission = rain.emission;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = rain.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(34f, 1f, 34f);

            ParticleSystemRenderer renderer =
                obj.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            // Original stretched streaks appeared as metre-long white poles.
            renderer.lengthScale = .42f;
            renderer.velocityScale = .01f;
            renderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
                shader = Shader.Find("Particles/Standard Unlit");

            if (shader != null)
            {
                rainMaterial = new Material(shader);
                renderer.sharedMaterial = rainMaterial;
            }

            rain.Play();
        }

        void LateUpdate()
        {
            if (clock == null || player == null || !player.Ready)
                return;

            int interval = Mathf.FloorToInt(
                Mathf.Repeat(clock.hourOfDay, 24f) / 6f);

            // A predictable four-part 24-hour weather loop for the prototype.
            // This is not yet a procedural forecast or a meteorological sim.
            SectorWeatherKind next = interval == 0
                ? SectorWeatherKind.Mist
                : interval == 1
                    ? SectorWeatherKind.Clear
                    : interval == 2
                        ? SectorWeatherKind.Overcast
                        : SectorWeatherKind.Rain;

            Weather = next;

            if (Weather != lastWeather)
            {
                lastWeather = Weather;

                ParticleSystem.EmissionModule emission = rain.emission;
                emission.rateOverTime =
                    Weather == SectorWeatherKind.Rain ? 210f : 0f;

                if (Weather != SectorWeatherKind.Rain)
                    rain.Clear();
            }

            Transform rainTransform = rain.transform;
            rainTransform.position =
                player.transform.position + Vector3.up * 19f;

            // The world clock owns time-of-day fog. Apply atmospheric
            // weather modifiers after the clock changes its values.
            float fogModifier = Weather == SectorWeatherKind.Mist
                ? 2.8f : Weather == SectorWeatherKind.Rain
                    ? 1.65f : Weather == SectorWeatherKind.Overcast
                        ? 1.25f : 1f;

            RenderSettings.fogDensity *= fogModifier;

            if (Weather != SectorWeatherKind.Clear)
                RenderSettings.fogColor = Color.Lerp(
                    RenderSettings.fogColor,
                    weatherFog,
                    Weather == SectorWeatherKind.Mist ? .7f : .35f);
        }

        void OnGUI()
        {
            if (ModernUiEnabled || clock == null ||
                player == null || !player.Ready)
                return;

            GUI.Label(new Rect(14f, 202f, 250f, 24f),
                "Weather: " + WeatherLabel +
                "  |  Time: " + clock.hourOfDay.ToString("00.0") + "h");
        }

        void OnDestroy()
        {
            if (rainMaterial != null)
                Destroy(rainMaterial);
        }
    }
}
