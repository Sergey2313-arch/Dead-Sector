using UnityEngine;
using UnityEngine.AI;

namespace DeadSector
{
    /// <summary>
    /// Simple hinged door interaction on streamed modular houses.
    /// Position, orientation and collider come from the owning prefab/pivot.
    /// </summary>
    public sealed class SectorDoor : MonoBehaviour
    {
        [Range(-145f, 145f)] public float openAngle = 105f;
        public float turnSpeed = 6f;
        public bool startsOpen;

        Quaternion closedRotation;
        bool isOpen;
        bool initialized;
        NavMeshObstacle navObstacle;

        public bool IsOpen => isOpen;

        void Awake()
        {
            closedRotation = transform.localRotation;
            isOpen = startsOpen;
            initialized = true;
        }

        void Start()
        {
            navObstacle = GetComponent<NavMeshObstacle>();
            SynchronizeObstacle();
        }

        public void Toggle()
        {
            if (!initialized)
                Awake();

            isOpen = !isOpen;
            if (navObstacle == null)
                navObstacle = GetComponent<NavMeshObstacle>();
            SynchronizeObstacle();
        }

        void SynchronizeObstacle()
        {
            if (navObstacle != null)
                navObstacle.enabled = !isOpen;
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
