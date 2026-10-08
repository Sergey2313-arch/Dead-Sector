using UnityEngine;

namespace DeadSector
{
    /// <summary>
    /// Player needs and stamina. Values are physiological meters, not levels,
    /// rarity tiers or RPG progression.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SectorSurvival : MonoBehaviour
    {
        [Header("Needs")]
        [Range(0f, 100f)] public float hunger = 100f;
        [Range(0f, 100f)] public float thirst = 100f;
        [Range(0f, 100f)] public float stamina = 100f;

        [Header("Rates per real-time second (prototype tuning)")]
        public float hungerDrain = .012f;
        public float thirstDrain = .018f;
        public float sprintStaminaDrain = 13f;
        public float staminaRecovery = 10f;
        public float exhaustionDamageInterval = 12f;

        SectorPlayer player;
        float exhaustionTimer;

        public bool CanSprint => stamina > 2f && hunger > 0f && thirst > 0f;
        public bool CanJump => stamina >= 8f;

        void Awake()
        {
            player = GetComponent<SectorPlayer>();
        }

        void Update()
        {
            if (player == null || !player.Ready || player.Health <= 0f)
                return;

            float seconds = Time.deltaTime;
            hunger = Mathf.Max(0f, hunger - hungerDrain * seconds);
            thirst = Mathf.Max(0f, thirst - thirstDrain * seconds);

            bool sprinting = player.IsSprinting && player.Speed > .25f;

            stamina = Mathf.Clamp(
                stamina + (sprinting ? -sprintStaminaDrain : staminaRecovery) * seconds,
                0f, 100f);

            if (hunger <= 0f || thirst <= 0f)
            {
                exhaustionTimer += seconds;

                if (exhaustionTimer >= exhaustionDamageInterval)
                {
                    exhaustionTimer = 0f;
                    player.Damage(2f);
                }
            }
            else
            {
                exhaustionTimer = 0f;
            }
        }

        public void ConsumeJumpStamina()
        {
            stamina = Mathf.Max(0f, stamina - 8f);
        }

        public void Restore(float food, float water)
        {
            hunger = Mathf.Clamp(hunger + food, 0f, 100f);
            thirst = Mathf.Clamp(thirst + water, 0f, 100f);
        }

        public void ApplySaved(float savedFood, float savedWater, float savedStamina)
        {
            hunger = Mathf.Clamp(savedFood, 0f, 100f);
            thirst = Mathf.Clamp(savedWater, 0f, 100f);
            stamina = Mathf.Clamp(savedStamina, 0f, 100f);
            exhaustionTimer = 0f;
        }
    }
}
