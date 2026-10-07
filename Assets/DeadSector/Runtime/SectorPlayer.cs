using UnityEngine;

namespace DeadSector
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class SectorPlayer : MonoBehaviour
    {
        public float walkSpeed = 4f, runSpeed = 7f, jumpHeight = 1.3f;
        public Camera view;
        public Transform visual;
        public bool thirdPerson = true;
        public bool Ready { get; set; }
        public float Health { get; private set; } = 100f;
        public float Speed { get; private set; }
        CharacterController body;
        SectorMannequin rig;
        float pitch = 12f, vertical, groundedAt = -10f, jumpAt = -10f;
        Renderer[] renderers;

        void Awake()
        {
            body = GetComponent<CharacterController>();
            rig = visual.GetComponent<SectorMannequin>();
            renderers = visual.GetComponentsInChildren<Renderer>();
            SetCursor(true);
        }
        void Update()
        {
            if (SectorInput.Pressed(KeyCode.Escape)) SetCursor(false);
            if (SectorInput.Click && Health > 0) SetCursor(true);
            if (SectorInput.Pressed(KeyCode.R)) Respawn();
            if (!Ready || Health <= 0 || Cursor.lockState != CursorLockMode.Locked)
            {
                Speed = 0; rig.SetMotion(0, body.isGrounded, vertical); return;
            }
            if (SectorInput.Pressed(KeyCode.V)) thirdPerson = !thirdPerson;
            Vector2 look = SectorInput.Look;
            transform.Rotate(0, look.x, 0);
            pitch = Mathf.Clamp(pitch - look.y, -75f, 75f);
            if (body.isGrounded)
            {
                groundedAt = Time.time;
                if (vertical < 0) vertical = -2f;
            }
            if (SectorInput.Pressed(KeyCode.Space)) jumpAt = Time.time;
            if (Time.time - groundedAt < .12f && Time.time - jumpAt < .12f)
            {
                vertical = Mathf.Sqrt(jumpHeight * 40f);
                groundedAt = jumpAt = -10f;
            }
            Vector2 input = SectorInput.Move;
            Vector3 move = transform.right * input.x + transform.forward * input.y;
            vertical = Mathf.Max(vertical - 20f * Time.deltaTime, -45f);
            var flags = body.Move((move * (SectorInput.Sprint ? runSpeed : walkSpeed) + Vector3.up * vertical) * Time.deltaTime);
            if ((flags & CollisionFlags.Above) != 0 && vertical > 0) vertical = 0;
            var p = transform.position;
            p.x = Mathf.Clamp(p.x, -3990, 3990); p.z = Mathf.Clamp(p.z, -3990, 3990);
            if (p != transform.position) { body.enabled = false; transform.position = p; body.enabled = true; }
            if (p.y < -30f) Respawn();
            var v = body.velocity; v.y = 0; Speed = v.magnitude;
            rig.SetMotion(Speed, body.isGrounded, vertical);
        }
        void LateUpdate()
        {
            if (view == null) return;
            foreach (var r in renderers) r.enabled = thirdPerson;
            Quaternion rotation = Quaternion.Euler(pitch, transform.eulerAngles.y, 0);
            Vector3 pivot = transform.position + Vector3.up * 1.65f;
            Vector3 offset = thirdPerson ? rotation * new Vector3(.55f, .1f, -3.5f) : Vector3.zero;
            float length = offset.magnitude;
            // Ignore actor layer: the camera must never collide with its own body.
            if (length > 0 && Physics.SphereCast(pivot, .18f, offset / length, out RaycastHit hit, length, ~(1 << 2), QueryTriggerInteraction.Ignore))
                offset = offset.normalized * Mathf.Max(.1f, hit.distance - .1f);
            view.transform.SetPositionAndRotation(pivot + offset, rotation);
        }
        public void Damage(float amount)
        {
            Health = Mathf.Max(0, Health - amount);
            if (Health <= 0) SetCursor(false);
        }
        public void Respawn()
        {
            if (!Ready) return;
            body.enabled = false; transform.position = SectorLayout.Spawn; body.enabled = true;
            Health = 100f; vertical = 0; groundedAt = jumpAt = -10f; SetCursor(true);
        }
        void OnDisable() => SetCursor(false);
        static void SetCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
