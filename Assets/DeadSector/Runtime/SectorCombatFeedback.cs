using UnityEngine;

namespace DeadSector
{
    /// <summary>
    /// Small, cheap crosshair and immediate strike feedback. Does not impose
    /// wind-up on the hit: impact/shot state is reported in the same frame as
    /// SectorGameplay.Attack. Replace IMGUI with UI Toolkit later.
    /// </summary>
    public sealed class SectorCombatFeedback : MonoBehaviour
    {
        public SectorPlayer player;

        float lastAttack = -100f;
        float lastImpact = -100f;
        float damageDone;
        bool powerful;
        bool lastHit;
        bool firearm;

        public void Configure(SectorPlayer target) => player = target;

        public void ShowAttack(bool hit, bool heavy, bool gun, float damage)
        {
            lastAttack = Time.time;
            powerful = heavy;
            firearm = gun;
            lastHit = hit;

            if (hit)
            {
                lastImpact = Time.time;
                damageDone = damage;
            }
        }

        void OnGUI()
        {
            if (player == null || !player.Ready ||
                player.Health <= 0f ||
                player.InputBlockedByUI ||
                Cursor.lockState != CursorLockMode.Locked)
                return;

            GUI.depth = -95;

            float centerX = Screen.width * .5f;
            float centerY = Screen.height * .5f;
            float elapsed = Time.time - lastAttack;
            float kick = elapsed < .14f ?
                (1f - Mathf.Clamp01(elapsed / .14f)) *
                (powerful ? 9f : firearm ? 5f : 3f) : 0f;
            float gap = 6f + kick;

            Color neutral = new Color(.87f, .92f, .86f, .85f);
            Color tint = lastHit && elapsed < .17f
                ? new Color(.97f, .41f, .33f, 1f) : neutral;

            DrawRect(new Rect(centerX - 1f,
                centerY - gap - 10f, 2f, 10f), tint);
            DrawRect(new Rect(centerX - 1f,
                centerY + gap, 2f, 10f), tint);
            DrawRect(new Rect(centerX - gap - 10f,
                centerY - 1f, 10f, 2f), tint);
            DrawRect(new Rect(centerX + gap,
                centerY - 1f, 10f, 2f), tint);

            float age = Time.time - lastImpact;
            if (age < .40f && lastHit)
            {
                float alpha = 1f - Mathf.Clamp01(age / .40f);
                Color hit = new Color(1f, .75f, .60f, alpha);
                DrawRect(new Rect(centerX - 16f, centerY - 16f,
                    5f, 2f), hit);
                DrawRect(new Rect(centerX + 11f, centerY + 14f,
                    5f, 2f), hit);

                Color previous = GUI.color;
                GUI.color = hit;
                GUI.Label(new Rect(centerX + 19f,
                    centerY - 28f - age * 24f, 100f, 25f),
                    "-" + Mathf.RoundToInt(damageDone));
                GUI.color = previous;
            }
        }

        static void DrawRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
