using UnityEngine;

namespace DeadSector
{
    /// <summary>
    /// Clip-independent hit recoil and short death fall, applied only to
    /// the zombie model (never to its NavMeshAgent). Animator is disabled
    /// on death by SectorZombie and the posed mesh remains temporarily.
    /// </summary>
    public sealed class SectorZombieReaction : MonoBehaviour
    {
        Transform model;
        Vector3 restPosition;
        Quaternion restRotation;
        float hitAt = -100f;
        float deadAt = -100f;
        bool dead;

        public bool HasDied => dead;

        public void Configure(Transform visual)
        {
            model = visual;
            if (model == null)
                return;

            restPosition = model.localPosition;
            restRotation = model.localRotation;
        }

        public void Hit()
        {
            if (model == null || dead)
                return;

            hitAt = Time.time;
        }

        public void Die()
        {
            if (model == null || dead)
                return;

            dead = true;
            deadAt = Time.time;
            hitAt = -100f;
        }

        void LateUpdate()
        {
            if (model == null)
                return;

            if (dead)
            {
                float t = Mathf.Clamp01((Time.time - deadAt) / .72f);
                t = t * t * (3f - 2f * t);

                model.localRotation = restRotation *
                    Quaternion.Euler(75f * t, 0f, 16f * t);

                model.localPosition = restPosition +
                    new Vector3(0f, -.42f * t, .10f * t);
                return;
            }

            float age = Time.time - hitAt;
            if (age < 0f || age >= .23f)
            {
                model.localPosition = restPosition;
                return;
            }

            float recoil = Mathf.Sin(
                Mathf.Clamp01(age / .23f) * Mathf.PI);

            model.localPosition = restPosition -
                new Vector3(0f, 0f, .11f * recoil);
        }
    }
}
