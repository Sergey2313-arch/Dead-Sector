using UnityEngine;

namespace DeadSector
{
    /// <summary>
    /// Full 8 x 8 km design atlas for the approved world concept.
    /// This is an explicit PLAN layer, not a false satellite view of geometry
    /// that has not been constructed or streamed in yet.
    /// </summary>
    public sealed class SectorWorldAtlas : MonoBehaviour
    {
        public SectorPlayer player;
        public SectorMinimap minimap;

        Texture2D background;
        GUIStyle labelStyle;
        GUIStyle boldStyle;
        GUIStyle headerStyle;
        GUIStyle smallStyle;

        const int MapResolution = 256;

        static readonly Color HighwayColor = new Color(.89f, .77f, .55f, .85f);
        static readonly Color GridColor = new Color(.95f, .95f, .95f, .20f);
        static readonly Color ExistingColor = new Color(.40f, .90f, .82f);
        static readonly Color PlannedColor = new Color(1f, .70f, .32f);

        void Awake()
        {
            BuildBackground();
        }

        void BuildBackground()
        {
            background = new Texture2D(
                MapResolution, MapResolution,
                TextureFormat.RGBA32, false);

            background.name = "DeadSector_DesignAtlas_8km";
            background.wrapMode = TextureWrapMode.Clamp;
            background.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[MapResolution * MapResolution];

            for (int py = 0; py < MapResolution; py++)
            {
                float z = 4000f - (py + .5f) * 8000f / MapResolution;

                for (int px = 0; px < MapResolution; px++)
                {
                    float x = -4000f + (px + .5f) * 8000f / MapResolution;

                    float altitude = SectorLayout.Height(x, z);
                    float detail = Mathf.PerlinNoise(
                        (x + 6400f) / 220f,
                        (z + 4900f) / 220f);

                    Color baseColor;

                    if (IsConceptWater(x, z))
                    {
                        baseColor = Color.Lerp(
                            new Color(.08f, .22f, .29f),
                            new Color(.12f, .35f, .42f),
                            detail);
                    }
                    else if (altitude > 190f)
                    {
                        baseColor = Color.Lerp(
                            new Color(.32f, .35f, .32f),
                            new Color(.47f, .47f, .42f),
                            detail);
                    }
                    else if (altitude > 125f)
                    {
                        baseColor = Color.Lerp(
                            new Color(.18f, .24f, .18f),
                            new Color(.26f, .30f, .23f),
                            detail);
                    }
                    else
                    {
                        baseColor = Color.Lerp(
                            new Color(.105f, .20f, .13f),
                            new Color(.23f, .31f, .18f),
                            detail);
                    }

                    // Texture row zero is bottom; GUI shows row zero at bottom
                    // after DrawTexture, so this produces north at top.
                    int index = (MapResolution - 1 - py) * MapResolution + px;
                    pixels[index] = baseColor;
                }
            }

            background.SetPixels(pixels);
            background.Apply(false, true);
        }

        static bool IsConceptWater(float x, float z)
        {
            // Atlas water comes from exactly the same world-space
            // geography query that builds playable streamed water meshes.
            return SectorGeography.TryGetWaterLevel(x, z, out _);
        }

        void EnsureStyles()
        {
            if (labelStyle != null)
                return;

            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 11;
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.normal.textColor = Color.white;
            labelStyle.wordWrap = false;

            boldStyle = new GUIStyle(labelStyle);
            boldStyle.fontStyle = FontStyle.Bold;
            boldStyle.fontSize = 12;

            headerStyle = new GUIStyle(boldStyle);
            headerStyle.fontSize = 16;
            headerStyle.alignment = TextAnchor.MiddleLeft;

            smallStyle = new GUIStyle(labelStyle);
            smallStyle.fontSize = 10;
            smallStyle.alignment = TextAnchor.MiddleLeft;
        }

        void OnGUI()
        {
            if (minimap == null || !minimap.TacticalOpen || background == null)
                return;

            // Overlay the existing gameplay view; do not pretend all streamed
            // terrain is loaded. Planned landmarks are explicitly distinguished.
            GUI.depth = -150;
            EnsureStyles();

            float available = Mathf.Min(Screen.height - 108f, Screen.width - 84f);
            float side = Mathf.Max(170f, available);
            Rect outer = new Rect(
                (Screen.width - side) * .5f,
                (Screen.height - side) * .5f + 13f,
                side, side);

            Rect map = new Rect(
                outer.x + 17f,
                outer.y + 19f,
                outer.width - 34f,
                outer.height - 34f);

            DrawRect(new Rect(0f, 0f, Screen.width, Screen.height),
                new Color(.012f, .017f, .02f, .97f));

            GUI.color = Color.white;
            GUI.Label(
                new Rect(outer.x, 7f, side, 25f),
                "DEAD SECTOR   /   WORLD PLAN 8×8 KM  [M — CLOSE]",
                headerStyle);

            GUI.DrawTexture(map, background, ScaleMode.StretchToFill);

            for (int i = 0; i <= 8; i++)
            {
                float p = i / 8f;
                float gx = map.x + p * map.width;
                float gy = map.y + p * map.height;

                DrawRect(new Rect(gx, map.y, 1f, map.height), GridColor);
                DrawRect(new Rect(map.x, gy, map.width, 1f), GridColor);

                if (i < 8)
                {
                    GUI.Label(
                        new Rect(map.x + (i + .5f) * map.width / 8f - 15f,
                            map.y - 17f, 30f, 17f),
                        ((char)('A' + i)).ToString(), smallStyle);

                    GUI.Label(
                        new Rect(map.x - 18f,
                            map.y + (i + .5f) * map.height / 8f - 9f,
                            18f, 17f),
                        (i + 1).ToString(), smallStyle);
                }
            }

            foreach (Vector2[] road in SectorMapPlan.RoadRoutes)
            {
                for (int i = 1; i < road.Length; i++)
                    DrawLine(ToMap(map, road[i - 1]), ToMap(map, road[i]),
                        HighwayColor, 2f);
            }

            foreach (SectorMapPlan.Location location in SectorMapPlan.Locations)
            {
                Vector2 pos = ToMap(map, location.MapPosition);
                bool categoryPrototyped =
                    location.Status != SectorMapPlan.BuildStatus.Planned;

                DrawRect(new Rect(pos.x - 3f, pos.y - 3f, 7f, 7f),
                    categoryPrototyped ? ExistingColor : PlannedColor);

                if (side >= 360f)
                {
                    GUI.Label(
                        new Rect(pos.x - 55f, pos.y + 5f, 110f, 16f),
                        location.Name, labelStyle);
                }
            }

            if (player != null)
            {
                Vector2 current = new Vector2(
                    player.transform.position.x,
                    player.transform.position.z);
                Vector2 pos = ToMap(map, current);

                DrawRect(new Rect(pos.x - 4f, pos.y - 4f, 9f, 9f),
                    new Color(.18f, .85f, 1f));

                GUI.Label(
                    new Rect(pos.x - 45f, pos.y - 24f, 90f, 18f),
                    "YOU", boldStyle);
            }

            Rect footer = new Rect(
                outer.x, outer.yMax + 3f,
                Mathf.Min(Screen.width - outer.x, side), 24f);

            GUI.Label(footer,
                "ORANGE: PLANNED   |   TURQUOISE: WORLD BLOCKOUT (NOT FINAL ART)   |   BLUE: PLAYER",
                smallStyle);
        }

        static Vector2 ToMap(Rect rect, Vector2 position)
        {
            return new Vector2(
                rect.x + Mathf.Clamp01((position.x + 4000f) / 8000f) * rect.width,
                rect.y + Mathf.Clamp01((4000f - position.y) / 8000f) * rect.height);
        }

        static void DrawRect(Rect rect, Color tint)
        {
            Color old = GUI.color;
            GUI.color = tint;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
        }

        static void DrawLine(Vector2 a, Vector2 b, Color color, float width)
        {
            Vector2 delta = b - a;
            float length = delta.magnitude;

            if (length < .01f)
                return;

            Matrix4x4 old = GUI.matrix;

            GUIUtility.RotateAroundPivot(
                Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, a);

            DrawRect(new Rect(a.x, a.y - width * .5f, length, width), color);
            GUI.matrix = old;
        }

        void OnDestroy()
        {
            if (background != null)
                Destroy(background);
        }
    }
}
