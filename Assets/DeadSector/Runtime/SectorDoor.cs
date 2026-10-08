using UnityEngine;

namespace DeadSector
{
    /// <summary>
    /// Simple hinged door interaction on streamed modular houses.
    /// Position, orientation and collider come from the owning prefab/pivot.
    /// </summary>
    public sealed class SectorDoor : MonoBehaviour
    {
        [Range(45f, 145f)] public float openAngle = 105f;
        public float turnSpeed = 6f;
        public bool startsOpen;

        Quaternion closedRotation;
        bool isOpen;
        bool initialized;

        public bool IsOpen => isOpen;

        void Awake()
        {
            closedRotation = transform.localRotation;
            isOpen = startsOpen;
            initialized = true;
        }

        public void Toggle()
        {
            if (!initialized)
                Awake();

            isOpen = !isOpen;
        }

        void Update()
        {
            Quaternion destination = closedRotation *
                Quaternion.Euler(0f, isOpen ? openAngle : 0f, 0f);

            transform.localRotation = Quaternion.Slerp(
                transform.localRotation,
                destination,
                1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
        }
    }
}
