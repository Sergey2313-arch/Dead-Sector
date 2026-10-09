using UnityEngine;

namespace DeadSector
{
    // Temporary survival flashlight for inspecting night-time structures
    // and furnished rooms; replace with an actual inventory-held lamp later.
    public sealed class SectorFlashlight : MonoBehaviour
    {
        public Camera view;
        public SectorPlayer player;
        public bool Enabled { get; private set; }

        Light beam;

        public void Configure(SectorPlayer target)
        {
            player = target;
            view = target != null ? target.view : null;
            if (beam == null)
            {
                GameObject root = new GameObject("Prototype_Flashlight_Beam");
                root.transform.SetParent(transform, false);
                beam = root.AddComponent<Light>();
                beam.type = LightType.Spot;
                beam.color = new Color(1f, .93f, .77f);
                beam.range = 21f;
                beam.spotAngle = 63f;
                beam.intensity = 2.1f;
                beam.shadows = LightShadows.None;
                beam.enabled = false;
            }
        }

        public void SetEnabled(bool value)
        {
            Enabled = value;
            if (beam != null) beam.enabled = value;
        }

        void Update()
        {
            if (player == null || !player.Ready ||
                player.Health <= 0f || player.InputBlockedByUI)
                return;

            if (SectorInput.Pressed(KeyCode.F))
                SetEnabled(!Enabled);
        }

        void LateUpdate()
        {
            if (beam == null) return;
            if (view == null && player != null)
                view = player.view;
            if (view == null) return;

            Transform cam = view.transform;
            beam.transform.position = cam.position +
                cam.right * .14f - cam.up * .10f + cam.forward * .2f;
            beam.transform.rotation = cam.rotation;
        }
    }
}
