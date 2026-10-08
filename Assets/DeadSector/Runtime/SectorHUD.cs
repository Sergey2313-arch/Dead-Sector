using UnityEngine;

namespace DeadSector
{
    /// <summary>
    /// Compact always-visible character panel. The separate full inventory
    /// adds silhouette, head/body/legs/feet/backpack slots and item grid.
    /// </summary>
    public sealed class SectorHUD : MonoBehaviour
    {
        public SectorPlayer player;
        public SectorSurvival needs;
        public SectorWorldClock clock;
        public SectorEquipment equipment;
        public SectorInventory inventory;

        GUIStyle label;
        GUIStyle small;
        GUIStyle value;
        Texture2D fill;

        public void Configure(
            SectorPlayer target,
            SectorSurvival survival,
            SectorWorldClock worldClock,
            SectorEquipment gear,
            SectorInventory bags)
        {
            player = target;
            needs = survival;
            clock = worldClock;
            equipment = gear;
            inventory = bags;
        }

        void EnsureStyles()
        {
            if (label != null) return;

            label = new GUIStyle(GUI.skin.label);
            label.fontStyle = FontStyle.Bold;
            label.fontSize = 12;
            label.normal.textColor = Color.white;

            small = new GUIStyle(label);
            small.fontSize = 10;
            small.fontStyle = FontStyle.Normal;

            value = new GUIStyle(label);
            value.alignment = TextAnchor.MiddleRight;
            value.fontSize = 11;
        }

        void OnGUI()
        {
            if (player == null || !player.Ready)
                return;

            EnsureStyles();
            GUI.depth = -100;

            float width = Mathf.Min(244f, Screen.width - 24f);
            Rect panel = new Rect(12f, 10f, width, 180f);

            DrawRect(panel, new Color(.025f, .037f, .043f, .83f));
            DrawRect(new Rect(panel.x, panel.y, 3f, panel.height),
                new Color(.45f, .73f, .64f, 1f));

            GUI.Label(new Rect(24f, 14f, width - 26f, 20f),
                "SURVIVOR  /  DEAD SECTOR", label);

            DrawBar(24f, 38f, width - 26f, "HP", player.Health,
                new Color(.86f, .29f, .27f));
            DrawBar(24f, 67f, width - 26f, "STAMINA",
                needs != null ? needs.stamina : 100f,
                new Color(.42f, .81f, .49f));
            DrawBar(24f, 96f, width - 26f, "HUNGER",
                needs != null ? needs.hunger : 100f,
                new Color(.85f, .68f, .33f));
            DrawBar(24f, 125f, width - 26f, "THIRST",
                needs != null ? needs.thirst : 100f,
                new Color(.34f, .64f, .96f));

            int day = clock != null ? clock.DayNumber : 1;
            float hr = clock != null ? clock.hourOfDay : 12f;
            int hour = Mathf.FloorToInt(hr);
            int minute = Mathf.Clamp(
                Mathf.FloorToInt((hr - hour) * 60f), 0, 59);
            GUI.Label(new Rect(24f, 153f, width - 26f, 20f),
                "DAY " + day + "   " + hour.ToString("00") +
                ":" + minute.ToString("00") +
                "   ARMOR " + Mathf.RoundToInt(
                    (1f - (equipment != null ? equipment.DamageMultiplier : 1f)) * 100f) + "%",
                small);
        }

        void DrawBar(float x, float y, float availableWidth,
            string title, float amount, Color tint)
        {
            float barW = availableWidth - 7f;

            GUI.Label(new Rect(x, y, barW * .65f, 16f), title, small);
            GUI.Label(new Rect(x, y, barW, 16f),
                Mathf.CeilToInt(amount).ToString("000"), value);

            DrawRect(new Rect(x, y + 17f, barW, 7f),
                new Color(.08f, .11f, .12f, 1f));

            DrawRect(new Rect(x, y + 17f,
                barW * Mathf.Clamp01(amount / 100f), 7f), tint);
        }

        static void DrawRect(Rect rect, Color tint)
        {
            Color before = GUI.color;
            GUI.color = tint;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = before;
        }
    }
}
