using UnityEngine;
using UnityEngine.Rendering;

namespace DeadSector
{
    public sealed class SectorBootstrap : MonoBehaviour
    {
        SectorWorld world;
        SectorPlayer player;
        SectorNavigation navigation;
        SectorArt actorArt;
        bool debugOverlay;
        float frameTimeSum;
        int sampleFrames;
        float displayedFps;
        float displayedFrameMs;

        [Header("UI")]
        [Tooltip("Disable to restore the legacy IMGUI during UI testing.")]
        public bool useModernUi = true;

        void Awake()
        {
            actorArt = new SectorArt();

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(38, -35, 0);
            sun.intensity = 1.4f;
            sun.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.48f, .53f, .58f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.48f, .55f, .6f);
            RenderSettings.fogDensity = .0009f;

            var clockObject = new GameObject("World_Clock");
            var clock = clockObject.AddComponent<SectorWorldClock>();
            clock.sun = sun;

            GameObject actor;
            Transform rig;
            Camera camera;

            // Prefer the new Mixamo full-body prefab if the user has placed it
            // in the prototype scene. This prevents the old procedural player
            // from being spawned alongside it.
            GameObject existing = GameObject.Find("Player_Mixamo_FullBody");

            if (existing != null)
            {
                actor = existing;
                actor.SetActive(false);

                actor.tag = "Player";
                actor.transform.position = SectorLayout.Spawn;

                var controller = actor.GetComponent<CharacterController>();
                if (controller == null)
                {
                    controller = actor.AddComponent<CharacterController>();
                    controller.height = SectorPlayer.StandingHeight;
                    controller.radius = SectorPlayer.StandingRadius;
                    controller.center = new Vector3(0, SectorPlayer.StandingHeight * .5f, 0);
                    controller.stepOffset = .3f;
                    controller.slopeLimit = 48;
                }

                // The prefab ships with PlayerMovement. The streamed world uses
                // SectorPlayer, so disable the duplicate movement component.
                foreach (var behaviour in actor.GetComponents<MonoBehaviour>())
                {
                    if (behaviour != null && behaviour.GetType().Name == "PlayerMovement")
                        behaviour.enabled = false;
                }

                rig = actor.transform.Find("FullBody");
                if (rig == null)
                {
                    var animator = actor.GetComponentInChildren<Animator>(true);
                    rig = animator != null ? animator.transform : actor.transform;
                }

                camera = actor.GetComponentInChildren<Camera>(true);
                if (camera == null)
                {
                    var cameraObject = new GameObject("PlayerCamera");
                    cameraObject.transform.SetParent(actor.transform, false);
                    camera = cameraObject.AddComponent<Camera>();
                    cameraObject.tag = "MainCamera";
                    cameraObject.AddComponent<AudioListener>();
                }

                SectorArt.SetActorLayer(actor.transform);
                Debug.Log("[Dead Sector] Using scene full-body player. Old procedural player spawn skipped.");
            }
            else
            {
                actor = new GameObject("Player");
                actor.transform.position = SectorLayout.Spawn;
                actor.tag = "Player";
                actor.layer = 2;

                var controller = actor.AddComponent<CharacterController>();
                controller.height = SectorPlayer.StandingHeight;
                controller.radius = SectorPlayer.StandingRadius;
                controller.center = new Vector3(0, SectorPlayer.StandingHeight * .5f, 0);
                controller.stepOffset = .3f;
                controller.slopeLimit = 48;

                rig = actorArt.Person(actor.transform, new Color(.18f, .28f, .22f)).transform;
                // Adjust only our throwaway fallback mannequin, never an
                // imported FBX or the user's locally edited full-body prefab.
                rig.localScale *= SectorPlayer.StandingHeight / 1.84f;

                var cameraObject = new GameObject("PlayerCamera");
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
                cameraObject.AddComponent<AudioListener>();

                Debug.LogWarning("[Dead Sector] Full-body player was not found in the scene. Using legacy procedural player.");
                actor.SetActive(false);
            }

            camera.fieldOfView = 75;
            camera.nearClipPlane = .05f;
            camera.farClipPlane = 1600;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = RenderSettings.fogColor;
            camera.enabled = true;
            camera.gameObject.tag = "MainCamera";

            // Only the playable player camera can be active on startup.
            // A leftover prototype camera/listener caused duplicate views
            // and "multiple AudioListeners" warnings in the test scene.
            foreach (Camera other in
                FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (other == camera) continue;
                other.enabled = false;

                if (other.CompareTag("MainCamera"))
                    other.gameObject.tag = "Untagged";
            }

            if (camera.GetComponent<AudioListener>() == null)
                camera.gameObject.AddComponent<AudioListener>();

            foreach (AudioListener listener in
                FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            {
                if (listener.gameObject != camera.gameObject)
                    listener.enabled = false;
            }

            player = actor.GetComponent<SectorPlayer>();
            if (player == null)
                player = actor.AddComponent<SectorPlayer>();

            player.view = camera;
            player.BindVisual(rig);

            actor.SetActive(true);

            world = new GameObject("World_8x8km").AddComponent<SectorWorld>();
            world.player = player;

            navigation = new GameObject("StartZone_AI").AddComponent<SectorNavigation>();
            navigation.world = world;
            navigation.player = player;

            var minimapObject = new GameObject("Minimap_System");
            var minimap = minimapObject.AddComponent<SectorMinimap>();
            minimap.Configure(player, world);

            var atlasObject = new GameObject("WorldAtlas_System");
            var atlas = atlasObject.AddComponent<SectorWorldAtlas>();
            atlas.player = player;
            atlas.minimap = minimap;

            var compassObject = new GameObject("Compass_System");
            var compass = compassObject.AddComponent<SectorCompass>();
            compass.player = player;
            compass.world = world;
            compass.minimap = minimap;

            var weatherObject = new GameObject("Weather_System");
            var weather = weatherObject.AddComponent<SectorWeather>();
            weather.Configure(player, clock);

            var hordeObject = new GameObject("Night_Horde_Director");
            var horde = hordeObject.AddComponent<SectorHordeDirector>();
            horde.Configure(player, clock, navigation);

            var gameplayObject = new GameObject("Survival_Gameplay");
            var gameplay = gameplayObject.AddComponent<SectorGameplay>();
            gameplay.Configure(player, world);
            gameplay.AttachHorde(horde);

            var hudObject = new GameObject("Character_HUD");
            var hud = hudObject.AddComponent<SectorHUD>();
            hud.Configure(player, gameplay.Needs, clock,
                gameplay.Armor, gameplay.Inventory);

            // The runtime-built Canvas replaces the IMGUI HUD and modal
            // windows, without editing locally modified Unity scenes.
            // If creation fails, legacy IMGUI remains the fallback.
            if (useModernUi)
            {
                var modern = new GameObject("Tactical_UI")
                    .AddComponent<SectorModernUI>();
                if (modern.Configure(gameplay, player, clock,
                    minimap, compass))
                {
                    hud.enabled = false;
                    weather.ModernUiEnabled = true;
                    var menu = new GameObject("DeadSector_FrontEnd")
                        .AddComponent<SectorMenuUI>();
                    if (!menu.Configure(gameplay, player, minimap))
                        Debug.LogWarning("[Dead Sector] Modern front-end " +
                            "was unavailable; gameplay HUD remains active.");
                    var death = new GameObject("DeadSector_DeathState")
                        .AddComponent<SectorDeathUI>();
                    if (!death.Configure(gameplay, player))
                        Debug.LogWarning("[Dead Sector] Death overlay unavailable; " +
                            "legacy R respawn still works.");
                }
            }
        }

        void Update()
        {
            // Collect a small rolling sample without allocations. F3 is a
            // lightweight performance check, not a replacement for Profiler.
            float dt = Time.unscaledDeltaTime;
            if (dt > 0f && dt < .5f)
            {
                frameTimeSum += dt;
                sampleFrames++;
            }

            if (frameTimeSum >= .5f && sampleFrames > 0)
            {
                displayedFps = sampleFrames / frameTimeSum;
                displayedFrameMs = frameTimeSum * 1000f / sampleFrames;
                frameTimeSum = 0f;
                sampleFrames = 0;
            }

            if (SectorInput.Pressed(KeyCode.F3))
                debugOverlay = !debugOverlay;
        }

        void OnGUI()
        {
            if (player == null || world == null || navigation == null)
                return;

            if (!world.Ready)
                GUI.Box(new Rect(Screen.width / 2 - 140,
                    Screen.height / 2 - 25, 280, 50),
                    "Создаём карту, подождите...");

            if (player.Health <= 0 &&
                (!useModernUi ||
                 FindFirstObjectByType<SectorDeathUI>() == null))
                GUI.Box(new Rect(Screen.width / 2 - 140,
                    Screen.height / 2 - 25, 280, 50),
                    "Вы погибли. Нажмите R для возрождения.");

            if (!debugOverlay)
                return;

            // Keep the top of the screen free for the azimuth compass.
            float hudY = Mathf.Max(12f, Screen.height - 235f);
            GUI.Box(new Rect(12, hudY, 370, 222), "DEAD SECTOR / прототип 8 × 8 км");
            GUI.Label(new Rect(24, hudY + 30f, 325, 24), "WASD ходьба | Shift бег | Space прыжок | V вид");
            GUI.Label(new Rect(24, hudY + 53f, 325, 24), "E собрать | ЛКМ/ПКМ удар | C крафт | J журнал");
            GUI.Label(new Rect(24, hudY + 76f, 325, 24), "Здоровье " + player.Health.ToString("0") + " | Карта: " + world.LoadedTiles + "/64 | Zombies: " + navigation.ZombieCount);
            GUI.Label(new Rect(24, hudY + 99f, 325, 24), "Мир: " + SectorRussian.DebugStatus(world.Status) +
                " | ИИ: " + SectorRussian.DebugStatus(navigation.Status));
            GUI.Label(new Rect(24, hudY + 122f, 325, 24), "Позиция: " + player.transform.position.ToString("F0"));
            GUI.Label(new Rect(24, hudY + 145f, 325, 24),
                "Анимация: " + (player.AnimationState == "Procedural"
                    ? "Манекен" : player.AnimationState) +
                " | На земле: " + (player.IsGrounded ? "ДА" : "НЕТ"));
            GUI.Label(new Rect(24, hudY + 167f, 345, 24),
                "Ноги Y: " + player.transform.position.y.ToString("F2") +
                " | Земля Y: " + player.TerrainUnderPlayer.ToString("F2") +
                " | Бег: " + (player.IsSprinting ? "ДА" : "НЕТ"));
            GUI.Label(new Rect(24, hudY + 190f, 335, 24),
                "FPS: " + displayedFps.ToString("F0") +
                " | Кадр: " + displayedFrameMs.ToString("F1") + " мс");

        }

        void OnDestroy() => actorArt?.Dispose();
    }
}
