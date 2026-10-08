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
                    controller.height = 1.8f;
                    controller.radius = .3f;
                    controller.center = new Vector3(0, .9f, 0);
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
                controller.height = 1.8f;
                controller.radius = .3f;
                controller.center = new Vector3(0, .9f, 0);
                controller.stepOffset = .3f;
                controller.slopeLimit = 48;

                rig = actorArt.Person(actor.transform, new Color(.18f, .28f, .22f)).transform;

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
        }

        void OnGUI()
        {
            // Keep the top of the screen free for the azimuth compass.
            float hudY = Mathf.Max(12f, Screen.height - 188f);
            GUI.Box(new Rect(12, hudY, 350, 176), "DEAD SECTOR / 8 x 8 km prototype");
            GUI.Label(new Rect(24, hudY + 30f, 325, 24), "WASD | Shift | Space | V view | M map | I inventory");
            GUI.Label(new Rect(24, hudY + 53f, 325, 24), "E gather | LMB weak/RMB strong | C craft | J journal");
            GUI.Label(new Rect(24, hudY + 76f, 325, 24), "HP " + player.Health.ToString("0") + " | Terrain: " + world.LoadedTiles + "/64 | Zombies: " + navigation.ZombieCount);
            GUI.Label(new Rect(24, hudY + 99f, 325, 24), "World: " + world.Status + " | AI: " + navigation.Status);
            GUI.Label(new Rect(24, hudY + 122f, 325, 24), "Position: " + player.transform.position.ToString("F0"));
            GUI.Label(new Rect(24, hudY + 145f, 325, 24), "Anim: " + player.AnimationState + " | Grounded: " + player.IsGrounded);
            if (!world.Ready) GUI.Box(new Rect(Screen.width / 2 - 140, Screen.height / 2 - 25, 280, 50), "Preparing map, please wait...");
            if (player.Health <= 0) GUI.Box(new Rect(Screen.width / 2 - 140, Screen.height / 2 - 25, 280, 50), "You died. Press R to respawn.");
        }

        void OnDestroy() => actorArt?.Dispose();
    }
}
