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
        public string WeatherLabel
        {
            get
            {
                switch (Weather)
                {
                    case SectorWeatherKind.Clear: return "Ясно";
                    case SectorWeatherKind.Overcast: return "Облачно";
                    case SectorWeatherKind.Rain: return "Дождь";
                    case SectorWeatherKind.Mist: return "Туман";
                    default: return "Неизвестно";
                }
            }
        }

        ParticleSystem rain;
        Material rainMaterial;
        public bool ModernUiEnabled { get; set; }
        SectorWeatherKind lastWeather = (SectorWeatherKind)(-1);
        readonly Color weatherFog = new Color(.34f, .36f, .34f);

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
            // Keep the horizontal X/Z emission area aligned with the ground.
            obj.transform.rotation = Quaternion.identity;

            rain = obj.AddComponent<ParticleSystem>();
            rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = rain.main;
            main.loop = true;
            main.duration = 2f;
            main.startLifetime = .85f;
            main.startSpeed = 0f;
            main.startSize = .025f;
            main.maxParticles = 680;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new Color(.7f, .8f, .92f, .26f);

            ParticleSystem.EmissionModule emission = rain.emission;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = rain.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(30f, 1f, 30f);

            // Rain falls straight down in world space, independently of
            // emitter orientation and player camera movement.
            ParticleSystem.VelocityOverLifetimeModule velocity =
                rain.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.y = new ParticleSystem.MinMaxCurve(-17f);

            // Buildings and player-built roof panels have real colliders.
            // World collision kills rain when it reaches roofs or the ground,
            // while rain outside windows and doorways stays visible.
            ConfigureRainWorldCollision(rain);

            ParticleSystemRenderer renderer =
                obj.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            // Original stretched streaks appeared as metre-long white poles.
            renderer.lengthScale = .14f;
            renderer.velocityScale = .015f;
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

        public static void ConfigureRainWorldCollision(ParticleSystem particles)
        {
            if (particles == null)
                return;

            ParticleSystem.CollisionModule collision = particles.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D;
            collision.quality = ParticleSystemCollisionQuality.High;

            // Layer 2 is used by the player rig (Ignore Raycast).
            // Collide with the world geometry, including TerrainColliders,
            // houses, industrial roofs and player-built roof panels.
            collision.collidesWith = ~(1 << 2);
            collision.bounce = 0f;
            collision.dampen = 0f;
            collision.lifetimeLoss = 1f;
            collision.radiusScale = .25f;
            collision.sendCollisionMessages = false;
        }

        public static float FogMultiplier(SectorWeatherKind state)
        {
            // Keep mist visible while preventing normal overcast afternoons
            // from turning the entire inland terrain into a blue-grey sheet.
            switch (state)
            {
                case SectorWeatherKind.Mist: return 2.0f;
                case SectorWeatherKind.Rain: return 1.4f;
                case SectorWeatherKind.Overcast: return 1.12f;
                default: return 1f;
            }
        }

        // A SolidColor player camera is our temporary horizon until a
        // full skybox is built. Unlike fog, Camera.backgroundColor is not
        // updated by RenderSettings; keep both colors in sync every frame.
        public static void SyncCameraBackdrop(Camera camera, Color fogColor)
        {
            if (camera == null ||
                camera.clearFlags != CameraClearFlags.SolidColor)
                return;

            camera.backgroundColor = new Color(
                fogColor.r, fogColor.g, fogColor.b, 1f);
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
                    Weather == SectorWeatherKind.Rain ? 500f : 0f;

                if (Weather != SectorWeatherKind.Rain)
                    rain.Clear();
            }

            Transform rainTransform = rain.transform;
            rainTransform.position =
                player.transform.position + Vector3.up * 12f;

            // The world clock owns time-of-day fog. Apply atmospheric
            // weather modifiers after the clock changes its values.
            float fogModifier = FogMultiplier(Weather);

            RenderSettings.fogDensity *= fogModifier;

            if (Weather != SectorWeatherKind.Clear)
                RenderSettings.fogColor = Color.Lerp(
                    RenderSettings.fogColor,
                    weatherFog,
                    Weather == SectorWeatherKind.Mist ? .7f : .35f);

            // Do this after clock and weather have both computed the current
            // atmospheric color, so the 1600m camera clip plane never reveals
            // the stale blue-grey startup clear color.
            SyncCameraBackdrop(player.view, RenderSettings.fogColor);
        }

        void OnGUI()
        {
            if (ModernUiEnabled || clock == null ||
                player == null || !player.Ready)
                return;

            GUI.Label(new Rect(14f, 202f, 250f, 24f),
                "Погода: " + WeatherLabel +
                "  |  Время: " + clock.hourOfDay.ToString("00.0") + "h");
        }

        void OnDestroy()
        {
            if (rainMaterial != null)
                Destroy(rainMaterial);
        }
    }
}
