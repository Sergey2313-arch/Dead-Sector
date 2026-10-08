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

        [Header("Sprint exhaustion hysteresis")]
        [Range(0f, 100f)] public float sprintStopStamina = 5f;
        [Range(0f, 100f)] public float sprintResumeStamina = 30f;

        SectorPlayer player;
        float exhaustionTimer;
        bool sprintExhausted;

        // Don't toggle sprint every frame when stamina hovers near zero.
        // After exhaustion the player must recover substantially before
        // sprint can resume, even if Shift is held continuously.
        public bool CanSprint
        {
            get
            {
                UpdateSprintLockout();
                return !sprintExhausted && hunger > 0f && thirst > 0f;
            }
        }

        void UpdateSprintLockout()
        {
            float resume = Mathf.Max(
                sprintStopStamina + 1f, sprintResumeStamina);

            if (stamina <= sprintStopStamina)
                sprintExhausted = true;
            else if (stamina >= resume)
                sprintExhausted = false;
        }
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
            UpdateSprintLockout();

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

        // Punches always react on the mouse press, including when exhausted.
        // Attack power is reduced rather than delayed or blocked.
        public float SpendAttackStamina(float cost)
        {
            if (cost <= 0f) return 1f;
            float available = stamina;
            stamina = Mathf.Max(0f, stamina - cost);
            return Mathf.Lerp(.45f, 1f, Mathf.Clamp01(available / cost));
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
            sprintExhausted = stamina <= sprintStopStamina;
            exhaustionTimer = 0f;
        }
    }
}
