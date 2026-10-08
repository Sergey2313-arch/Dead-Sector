using UnityEngine;

namespace DeadSector
{
    /// <summary>
    /// PUBG/STALKER-style azimuth ruler. North is world +Z (000),
    /// east is +X (090), south is -Z (180), west is -X (270).
    /// Uses the actual gameplay camera heading in both FPP and TPP.
    /// </summary>
    public sealed class SectorCompass : MonoBehaviour
    {
        [Header("References")]
        public SectorPlayer player;
        public SectorWorld world;
        public SectorMinimap minimap;

        [Header("Compass")]
        [Range(40f, 110f)] public float halfVisibleDegrees = 85f;
        public float maxWidth = 740f;
        public float top = 9f;

        [Header("Points of interest")]
        public bool showPOI = true;
        public float maxPoiDistance = 1400f;
        public int maxVisiblePOI = 5;

        static readonly string[] Directions =
        {
            "N", "NE", "E", "SE", "S", "SW", "W", "NW"
        };

        static readonly Color BarColor = new Color(.025f, .035f, .04f, .69f);
        static readonly Color LineColor = new Color(.80f, .86f, .87f, .47f);
        static readonly Color BrightColor = new Color(.96f, .97f, .98f, 1f);
        static readonly Color ActiveColor = new Color(1f, .78f, .30f, 1f);

        GUIStyle smallStyle;
        GUIStyle cardinalStyle;
        GUIStyle headingStyle;
        GUIStyle poiStyle;
        readonly float[] poiLabelPositions = new float[32];

        public static float Bearing(Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            return Mathf.Repeat(Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg, 360f);
        }

        public static float RelativeAngle(float viewHeading, float targetBearing)
        {
            return Mathf.DeltaAngle(viewHeading, targetBearing);
        }

        public static string CardinalName(int degrees)
        {
            int wrapped = ((degrees % 360) + 360) % 360;
            if (wrapped % 45 != 0)
                return null;

            return Directions[wrapped / 45];
        }

        void OnGUI()
        {
            if (player == null || player.view == null ||
                (minimap != null && minimap.TacticalOpen))
                return;

            EnsureStyles();

            // Leave the upper-right corner for the minimap at narrow aspect ratios.
            float width = Mathf.Min(maxWidth, Screen.width * .52f - 20f);
            if (width < 180f)
                return;

            float cx = Screen.width * .5f;
            float left = cx - width * .5f;
            float y = top;
            float halfWidth = width * .5f;
            float heading = Mathf.Repeat(player.view.transform.eulerAngles.y, 360f);
            int shownBearing = Mathf.RoundToInt(heading) % 360;

            DrawRect(new Rect(left, y, width, 71f), BarColor);

            // Central, fixed bearing and sight marker. The ticks move underneath it.
            GUI.color = ActiveColor;
            GUI.Label(
                new Rect(cx - 37f, y + 1f, 74f, 23f),
                shownBearing.ToString("000") + "°",
                headingStyle);
            GUI.color = Color.white;

            DrawRect(
                new Rect(left + 5f, y + 47f, width - 10f, 1f),
                LineColor);

            float degreesPerPixel = halfVisibleDegrees / halfWidth;
            int firstTick =
                Mathf.FloorToInt((heading - halfVisibleDegrees) / 5f) * 5;
            int lastTick =
                Mathf.CeilToInt((heading + halfVisibleDegrees) / 5f) * 5;

            for (int degree = firstTick; degree <= lastTick; degree += 5)
            {
                float relative = RelativeAngle(heading, degree);
                if (Mathf.Abs(relative) > halfVisibleDegrees)
                    continue;

                float x = cx + relative / degreesPerPixel;
                int normalized = ((degree % 360) + 360) % 360;

                bool cardinal = normalized % 45 == 0;
                bool major = normalized % 15 == 0;
                float tickHeight = cardinal ? 16f : major ? 12f : 6f;

                DrawRect(
                    new Rect(x, y + 47f - tickHeight, cardinal ? 2f : 1f, tickHeight),
                    cardinal ? BrightColor : LineColor);

                if (cardinal)
                {
                    GUI.color = BrightColor;
                    GUI.Label(
                        new Rect(x - 23f, y + 49f, 46f, 18f),
                        CardinalName(normalized),
                        cardinalStyle);
                }
                else if (major)
                {
                    GUI.color = LineColor;
                    GUI.Label(
                        new Rect(x - 20f, y + 49f, 40f, 17f),
                        normalized.ToString("000"),
                        smallStyle);
                }
            }

            GUI.color = Color.white;
            DrawRect(new Rect(cx - 1f, y + 28f, 2f, 26f), ActiveColor);
            DrawRect(new Rect(cx - 4f, y + 26f, 8f, 2f), ActiveColor);

            if (showPOI && world != null && world.Ready)
                DrawPoiIndicators(heading, cx, halfWidth, y + 73f);
        }

        void DrawPoiIndicators(float heading, float cx, float halfWidth, float y)
        {
            int displayed = 0;
            Vector3 origin = player.transform.position;

            // POIs come from the same registry as the minimap.
            foreach (SectorPointOfInterest poi in world.PointsOfInterest)
            {
                if (poi.Name == "Spawn")
                    continue;

                Vector3 delta = poi.Position - origin;
                delta.y = 0f;
                float distance = delta.magnitude;

                if (distance > maxPoiDistance || distance < 12f)
                    continue;

                float relative = RelativeAngle(heading, Bearing(origin, poi.Position));
                if (Mathf.Abs(relative) > halfVisibleDegrees - 7f)
                    continue;

                float x = cx + relative / halfVisibleDegrees * halfWidth;

                // Compare against every label, not merely the previous POI:
                // registry order does not necessarily match screen order.
                bool overlaps = false;
                for (int i = 0; i < displayed; i++)
                {
                    if (Mathf.Abs(x - poiLabelPositions[i]) < 95f)
                    {
                        overlaps = true;
                        break;
                    }
                }

                if (overlaps)
                    continue;

                poiLabelPositions[displayed] = x;
                displayed++;

                DrawRect(new Rect(x - 3f, y, 6f, 6f), poi.MapColor);

                GUI.color = BrightColor;
                GUI.Label(
                    new Rect(x - 65f, y + 7f, 130f, 17f),
                    poi.Name + "  " + Mathf.RoundToInt(distance) + "m",
                    poiStyle);
                GUI.color = Color.white;

                if (displayed >= maxVisiblePOI)
                    break;
            }
        }

        void EnsureStyles()
        {
            if (smallStyle != null)
                return;

            smallStyle = new GUIStyle(GUI.skin.label);
            smallStyle.alignment = TextAnchor.MiddleCenter;
            smallStyle.fontSize = 10;
            smallStyle.normal.textColor = LineColor;

            cardinalStyle = new GUIStyle(GUI.skin.label);
            cardinalStyle.alignment = TextAnchor.MiddleCenter;
            cardinalStyle.fontSize = 14;
            cardinalStyle.fontStyle = FontStyle.Bold;
            cardinalStyle.normal.textColor = BrightColor;

            headingStyle = new GUIStyle(GUI.skin.label);
            headingStyle.alignment = TextAnchor.MiddleCenter;
            headingStyle.fontSize = 17;
            headingStyle.fontStyle = FontStyle.Bold;
            headingStyle.normal.textColor = ActiveColor;

            poiStyle = new GUIStyle(GUI.skin.label);
            poiStyle.alignment = TextAnchor.MiddleCenter;
            poiStyle.fontSize = 11;
            poiStyle.fontStyle = FontStyle.Bold;
            poiStyle.normal.textColor = BrightColor;
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
