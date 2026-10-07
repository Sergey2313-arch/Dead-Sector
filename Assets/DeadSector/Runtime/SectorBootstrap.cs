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
            var sun = new GameObject("Sun").AddComponent<Light>(); sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(38, -35, 0); sun.intensity = 1.4f; sun.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.48f, .53f, .58f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.48f, .55f, .6f); RenderSettings.fogDensity = .0009f;
            var actor = new GameObject("Player"); actor.transform.position = SectorLayout.Spawn;
            actor.tag = "Player"; actor.layer = 2;
            var controller = actor.AddComponent<CharacterController>(); controller.height = 1.8f; controller.radius = .3f;
            controller.center = new Vector3(0, .9f, 0); controller.stepOffset = .3f; controller.slopeLimit = 48;
            var rig = actorArt.Person(actor.transform, new Color(.18f, .28f, .22f));
            var cameraObject = new GameObject("PlayerCamera"); var camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera"; cameraObject.AddComponent<AudioListener>();
            camera.fieldOfView = 75; camera.nearClipPlane = .05f; camera.farClipPlane = 1600;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = RenderSettings.fogColor;
            // Assign fields before Awake, so the controller sees the finished visual rig.
            actor.SetActive(false);
            player = actor.AddComponent<SectorPlayer>(); player.view = camera; player.visual = rig.transform;
            actor.SetActive(true);
            world = new GameObject("World_8x8km").AddComponent<SectorWorld>(); world.player = player;
            navigation = new GameObject("StartZone_AI").AddComponent<SectorNavigation>(); navigation.world = world; navigation.player = player;
        }
        void OnGUI()
        {
            GUI.Box(new Rect(12, 12, 350, 152), "DEAD SECTOR / 8 x 8 km prototype");
            GUI.Label(new Rect(24, 42, 325, 24), "WASD move | Shift run | Space jump | V camera");
            GUI.Label(new Rect(24, 65, 325, 24), "Esc cursor | Click resume | R respawn");
            GUI.Label(new Rect(24, 88, 325, 24), "HP " + player.Health.ToString("0") + " | Terrain: " + world.LoadedTiles + "/64 | Zombies: " + navigation.ZombieCount);
            GUI.Label(new Rect(24, 111, 325, 24), "World: " + world.Status + " | AI: " + navigation.Status);
            GUI.Label(new Rect(24, 134, 325, 24), "Position: " + player.transform.position.ToString("F0"));
            if (!world.Ready) GUI.Box(new Rect(Screen.width / 2 - 140, Screen.height / 2 - 25, 280, 50), "Preparing map, please wait...");
            if (player.Health <= 0) GUI.Box(new Rect(Screen.width / 2 - 140, Screen.height / 2 - 25, 280, 50), "You died. Press R to respawn.");
        }
        void OnDestroy() => actorArt?.Dispose();
    }
}
