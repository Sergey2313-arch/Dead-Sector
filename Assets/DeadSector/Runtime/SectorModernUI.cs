using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace DeadSector
{
    /// <summary>
    /// Built at runtime so the HUD does not depend on the user's locally
    /// modified Unity scene, imported FBXs, prefabs or custom font assets.
    /// All player actions call the existing SectorGameplay rules.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SectorModernUI : MonoBehaviour
    {
        static readonly Color Backdrop = new Color(.010f, .014f, .017f, .89f);
        static readonly Color Panel = new Color(.043f, .056f, .057f, .97f);
        static readonly Color Cell = new Color(.085f, .107f, .106f, .94f);
        static readonly Color Edge = new Color(.16f, .215f, .205f, 1f);
        static readonly Color Accent = new Color(.61f, .78f, .52f, 1f);
        static readonly Color Muted = new Color(.56f, .64f, .62f, 1f);
        static readonly Color White = new Color(.92f, .94f, .91f, 1f);

        readonly Color[] vitality =
        {
            new Color(.84f, .33f, .29f),
            new Color(.66f, .83f, .48f),
            new Color(.86f, .67f, .39f),
            new Color(.38f, .69f, .83f)
        };

        SectorGameplay gameplay;
        SectorPlayer player;
        SectorWorldClock clock;
        SectorMinimap minimap;
        SectorCompass compass;
        Canvas mainCanvas;
        Font uiFont;
        GameObject canvasObject;
        GameObject modalRoot;
        GameObject inventoryPage;
        GameObject craftingPage;
        GameObject hintRoot;
        GameObject messageRoot;

        readonly Image[] meterFill = new Image[4];
        readonly Text[] meterValues = new Text[4];
        readonly Text[] hotbarLabels = new Text[3];
        readonly Image[] hotbarAccent = new Image[3];
        readonly Text[] bagTexts = new Text[30];
        readonly Image[] bagBackgrounds = new Image[30];
        readonly Image[] bagCategoryStripes = new Image[30];
        readonly Text[] gearLabels = new Text[5];
        readonly List<RecipeDisplay> recipeDisplays = new List<RecipeDisplay>();

        Text dayLabel;
        Text compassBearing;
        RawImage tacticalMap;
        Text mapScaleLabel;
        Text mapCoordsLabel;
        RectTransform playerArrow;
        RectTransform mapRect;
        RectTransform mapPoiOverlay;
        Texture2D mapTexture;
        Color32[] mapPixels;
        Vector2 lastMapCenter = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        float mapRefreshAt;
        readonly List<CompassTick> compassTicks = new List<CompassTick>();
        readonly Text[] compassPoiTexts = new Text[4];
        readonly RectTransform[] compassPoiRects = new RectTransform[4];
        RectTransform compassPanel;
        readonly List<MapPoiMarker> mapPoiMarkers = new List<MapPoiMarker>();
        Text bagSummary;
        Text selectedTitle;
        Text selectedDetails;
        Text craftingSummary;
        Text hintText;
        Text messageText;
        RectTransform dragGhost;
        Text dragGhostText;
        Button useButton;
        Button equipButton;
        RectTransform bodyPanel;
        string selectedId = "";
        float refreshTime;
        bool lastInventory;
        bool lastCrafting;

        sealed class CompassTick
        {
            public RectTransform rect;
            public Text text;
            public Image line;
        }

        sealed class MapPoiMarker
        {
            public SectorPointOfInterest poi;
            public RectTransform rect;
        }

        sealed class RecipeDisplay
        {
            public SectorRecipe recipe;
            public Text description;
            public Button craftButton;
            public Image background;
        }

        public bool Configure(SectorGameplay source, SectorPlayer target,
            SectorWorldClock worldClock, SectorMinimap worldMinimap,
            SectorCompass worldCompass)
        {
            if (source == null || target == null)
                return false;

            gameplay = source;
            player = target;
            clock = worldClock;
            minimap = worldMinimap;
            compass = worldCompass;

            try
            {
                Build();
                gameplay.ModernUiEnabled = true;
                if (minimap != null)
                    minimap.SetCanvasHudActive(true);
                if (compass != null)
                    compass.enabled = false;
                return true;
            }
            catch (Exception error)
            {
                Debug.LogError("[Dead Sector UI] Canvas setup failed, legacy " +
                    "interface remains available: " + error);
                if (canvasObject != null)
                    Destroy(canvasObject);
                gameplay.ModernUiEnabled = false;
                enabled = false;
                return false;
            }
        }

        void Build()
        {
            uiFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Segoe UI", "Arial", "Liberation Sans" }, 16);

            canvasObject = new GameObject("DeadSector_UI_Canvas",
                typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            mainCanvas = canvas;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;
            EnsureEventSystem();

            RectTransform root = canvasObject.GetComponent<RectTransform>();

            BuildHud(root);
            BuildCompass(root);
            BuildMinimap(root);
            BuildModal(root);
        }

        void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;

            GameObject obj = new GameObject("DeadSector_UI_EventSystem");
            obj.transform.SetParent(canvasObject.transform, false);
            obj.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            InputSystemUIInputModule module =
                obj.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
#else
            obj.AddComponent<StandaloneInputModule>();
#endif
        }

        void BuildHud(RectTransform root)
        {
            // Top-left translucent survival readout. The compass and the
            // existing top-right minimap deliberately remain unobstructed.
            RectTransform vitalPanel = Rect(root, "Survivor_Readout",
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(24f, -24f),
                new Vector2(253f, 202f));
            Paint(vitalPanel, new Color(.018f, .026f, .028f, .82f));
            RectAt(vitalPanel, "Accent_Line", 0f, 0f, 3f, 202f, Accent);
            Label(vitalPanel, "Title", "SURVIVOR   /   01", 16f, 12f,
                210f, 22f, 15, White, FontStyle.Bold);
            Label(vitalPanel, "Subtitle", "STATUS MONITOR",
                16f, 36f, 205f, 14f, 10, Muted);

            string[] names = { "HEALTH", "STAMINA", "HUNGER", "THIRST" };
            for (int i = 0; i < names.Length; i++)
            {
                float y = 56f + 30f * i;
                Label(vitalPanel, names[i] + "_Label", names[i],
                    16f, y, 138f, 15f, 10, Muted);
                meterValues[i] = Label(vitalPanel, "Value_" + i,
                    "100", 197f, y, 40f, 15f, 11, White,
                    FontStyle.Bold, TextAnchor.MiddleRight);
                RectAt(vitalPanel, "MeterTrack_" + i,
                    16f, y + 17f, 221f, 5f, Edge);
                RectTransform fill = RectAt(vitalPanel, "MeterFill_" + i,
                    16f, y + 17f, 221f, 5f, vitality[i]);
                meterFill[i] = fill.GetComponent<Image>();
            }

            Label(vitalPanel, "Shortcuts", "I  INVENTORY     C  CRAFTING",
                16f, 176f, 225f, 16f, 10, Muted);

            RectTransform bar = Rect(root, "Weapon_Hotbar",
                new Vector2(.5f, 0f), new Vector2(.5f, 0f),
                new Vector2(.5f, 0f), new Vector2(0f, 24f),
                new Vector2(446f, 71f));

            for (int i = 0; i < 3; i++)
            {
                RectTransform frame = RectAt(bar, "Slot_" + i,
                    i * 152f, 0f, 142f, 70f, Panel);
                RectAt(frame, "Outline", 0f, 0f, 142f, 1f, Edge);
                hotbarAccent[i] = RectAt(frame, "Active", 0f, 0f,
                    142f, 3f, Accent).GetComponent<Image>();
                Label(frame, "SlotTag", (i + 1) + "  " +
                    (i == 0 ? "PRIMARY" : i == 1 ? "SIDEARM" : "MELEE"),
                    10f, 10f, 120f, 17f, 11, Muted, FontStyle.Bold);
                hotbarLabels[i] = Label(frame, "ItemName", "EMPTY",
                    10f, 34f, 125f, 26f, 13, White, FontStyle.Bold);
            }

            RectTransform dayPanel = Rect(root, "Day_Readout",
                new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(-29f, 32f),
                new Vector2(205f, 54f));
            Paint(dayPanel, new Color(.025f, .038f, .038f, .88f));
            RectAt(dayPanel, "Stripe", 0f, 0f, 3f, 54f, Accent);
            dayLabel = Label(dayPanel, "WorldTime", "DAY 01  /  12:00",
                16f, 10f, 178f, 18f, 13, White, FontStyle.Bold);
            Label(dayPanel, "SaveShortcuts", "F5 SAVE   /   F9 LOAD",
                16f, 32f, 175f, 14f, 10, Muted);

            hintRoot = Rect(root, "Interaction_Tip",
                new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(.5f, .5f), new Vector2(0f, -145f),
                new Vector2(420f, 44f)).gameObject;
            Paint(hintRoot.GetComponent<RectTransform>(),
                new Color(.022f, .030f, .032f, .84f));
            hintText = Label(hintRoot.transform, "Action", "E  INTERACT",
                10f, 6f, 400f, 32f, 14, White,
                FontStyle.Bold, TextAnchor.MiddleCenter);
            hintRoot.SetActive(false);

            messageRoot = Rect(root, "Event_Toast",
                new Vector2(.5f, 0f), new Vector2(.5f, 0f),
                new Vector2(.5f, 0f), new Vector2(0f, 113f),
                new Vector2(360f, 36f)).gameObject;
            Paint(messageRoot.GetComponent<RectTransform>(),
                new Color(.085f, .13f, .092f, .91f));
            messageText = Label(messageRoot.transform, "Toast_Text", "",
                12f, 3f, 336f, 30f, 13, White,
                FontStyle.Bold, TextAnchor.MiddleCenter);
            messageRoot.SetActive(false);
        }


        void BuildCompass(RectTransform root)
        {
            compassPanel = Rect(root, "Tactical_Compass",
                new Vector2(.5f, 1f), new Vector2(.5f, 1f),
                new Vector2(.5f, 1f), new Vector2(0f, -12f),
                new Vector2(632f, 79f));
            Paint(compassPanel, new Color(.02f, .028f, .028f, .80f));

            RectAt(compassPanel, "TopLine", 4f, 0f, 624f, 2f, Accent);
            compassBearing = Label(compassPanel, "Heading", "000°",
                255f, 1f, 122f, 25f, 18, Accent,
                FontStyle.Bold, TextAnchor.MiddleCenter);

            RectAt(compassPanel, "CenterNeedle", 315f, 26f,
                2f, 38f, Accent);
            RectAt(compassPanel, "CenterDiamond", 311f, 24f,
                10f, 3f, Accent);

            for (int i = 0; i < 27; i++)
            {
                RectTransform tick = RectAt(compassPanel,
                    "BearingTick_" + i, 0f, 38f,
                    52f, 37f, Color.clear);
                Image line = RectAt(tick, "Notch", 25f, 0f,
                    1f, 10f, Muted).GetComponent<Image>();
                Text text = Label(tick, "Degrees", "", 0f, 14f,
                    52f, 20f, 10, White, FontStyle.Bold,
                    TextAnchor.MiddleCenter);
                compassTicks.Add(new CompassTick
                {
                    rect = tick,
                    text = text,
                    line = line
                });
            }

            for (int i = 0; i < compassPoiTexts.Length; i++)
            {
                RectTransform marker = RectAt(compassPanel,
                    "PoiBearing_" + i, 0f, 80f, 126f, 21f,
                    new Color(.04f, .063f, .055f, .94f));
                compassPoiRects[i] = marker;
                compassPoiTexts[i] = Label(marker, "POI", "",
                    3f, 0f, 119f, 20f, 10, Accent,
                    FontStyle.Bold, TextAnchor.MiddleCenter);
                marker.gameObject.SetActive(false);
            }
        }

        void BuildMinimap(RectTransform root)
        {
            const int mapSize = 214;
            RectTransform panel = Rect(root, "NorthUp_Local_Map",
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-25f, -23f),
                new Vector2(232f, 283f));
            Paint(panel, new Color(.018f, .030f, .032f, .95f));
            RectAt(panel, "TopAccent", 0f, 0f, 232f, 3f, Accent);
            Label(panel, "MapHeader", "LOCAL SCAN    N ↑",
                10f, 8f, 216f, 21f, 13, White, FontStyle.Bold);

            mapRect = Rect(panel, "SurveyMap",
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(9f, -34f),
                new Vector2(mapSize, mapSize));
            RawImage image = mapRect.gameObject.AddComponent<RawImage>();
            image.color = Color.white;
            image.raycastTarget = false;
            tacticalMap = image;

            mapPixels = new Color32[
                SectorTacticalMapRaster.Resolution *
                SectorTacticalMapRaster.Resolution];
            mapTexture = new Texture2D(
                SectorTacticalMapRaster.Resolution,
                SectorTacticalMapRaster.Resolution,
                TextureFormat.RGBA32, false);
            mapTexture.name = "DeadSector_LocalSurvey_128";
            mapTexture.wrapMode = TextureWrapMode.Clamp;
            mapTexture.filterMode = FilterMode.Bilinear;
            tacticalMap.texture = mapTexture;

            RectTransform overlay = RectAt(mapRect,
                "Map_Annotations", 0f, 0f, mapSize, mapSize,
                Color.clear);
            RectAt(overlay, "CrosshairVertical", mapSize * .5f,
                0f, 1f, mapSize, new Color(.87f, .97f, .80f, .16f));
            RectAt(overlay, "CrosshairHorizontal", 0f, mapSize * .5f,
                mapSize, 1f, new Color(.87f, .97f, .80f, .16f));

            // The marker is always at the local map center; map is
            // north-up while the arrow rotates to the actual player yaw.
            playerArrow = RectAt(overlay, "Player_NorthArrow",
                mapSize * .5f - 17f, mapSize * .5f - 17f,
                34f, 34f, new Color(.02f, .04f, .04f, .82f));
            Label(playerArrow, "ArrowGlyph", "▲",
                0f, 0f, 34f, 34f, 28, Accent,
                FontStyle.Bold, TextAnchor.MiddleCenter);

            Label(panel, "MapFooter", "TERRAIN  /  ROADS  /  WATER",
                10f, 253f, 212f, 13f, 10, Muted);
            mapScaleLabel = Label(panel, "Scale", "340 M",
                166f, 253f, 56f, 13f, 10, Accent,
                FontStyle.Bold, TextAnchor.MiddleRight);
            mapCoordsLabel = Label(panel, "Coordinates", "",
                10f, 268f, 211f, 14f, 10, Muted);
            mapPoiOverlay = overlay;
        }

        void RefreshCompass()
        {
            if (player.view == null)
                return;

            float heading = Mathf.Repeat(
                player.view.transform.eulerAngles.y, 360f);
            compassBearing.text =
                (Mathf.RoundToInt(heading) % 360).ToString("000") + "°";

            int first = Mathf.FloorToInt(
                (heading - 65f) / 5f) * 5;
            for (int i = 0; i < compassTicks.Count; i++)
            {
                int absolute = first + i * 5;
                float delta = Mathf.DeltaAngle(heading, absolute);
                CompassTick tick = compassTicks[i];
                bool visible = Mathf.Abs(delta) <= 64f;
                tick.rect.gameObject.SetActive(visible);
                if (!visible)
                    continue;

                tick.rect.anchoredPosition =
                    new Vector2(310f + delta / 65f * 300f - 26f, -37f);
                int angle = ((absolute % 360) + 360) % 360;
                bool cardinal = angle % 45 == 0;
                bool major = angle % 15 == 0;
                tick.line.rectTransform.sizeDelta =
                    new Vector2(cardinal ? 2f : 1f,
                        cardinal ? 17f : major ? 11f : 6f);
                tick.line.color = cardinal ? Accent : Muted;
                tick.text.text = cardinal
                    ? SectorCompass.CardinalName(angle)
                    : major ? angle.ToString("000") : "";
            }

            int marked = 0;
            if (minimap != null && minimap.world != null &&
                minimap.world.Ready)
            {
                Vector3 center = player.transform.position;
                foreach (SectorPointOfInterest poi in
                    minimap.world.PointsOfInterest)
                {
                    if (marked >= compassPoiTexts.Length)
                        break;

                    Vector3 delta = poi.Position - center;
                    delta.y = 0f;
                    float metres = delta.magnitude;
                    if (metres > 1250f || metres < 16f)
                        continue;

                    float relative = SectorCompass.RelativeAngle(
                        heading, SectorCompass.Bearing(center, poi.Position));
                    if (Mathf.Abs(relative) > 55f)
                        continue;

                    float x = 310f + relative / 65f * 300f;
                    bool overlap = false;
                    for (int j = 0; j < marked; j++)
                    {
                        if (Mathf.Abs(compassPoiRects[j].anchoredPosition.x -
                            (x - 63f)) < 115f)
                        {
                            overlap = true;
                            break;
                        }
                    }

                    if (overlap)
                        continue;

                    compassPoiRects[marked].gameObject.SetActive(true);
                    compassPoiRects[marked].anchoredPosition =
                        new Vector2(x - 63f, -83f);
                    compassPoiTexts[marked].text =
                        poi.Name + "  " + Mathf.RoundToInt(metres) + "m";
                    marked++;
                }
            }

            for (int i = marked; i < compassPoiTexts.Length; i++)
                compassPoiRects[i].gameObject.SetActive(false);
        }

        void RefreshMinimap()
        {
            if (tacticalMap == null || player == null)
                return;

            Vector3 pos = player.transform.position;
            Vector2 center = new Vector2(pos.x, pos.z);
            float diameter = SectorTacticalMapRaster.DefaultDiameter;

            if (Time.unscaledTime >= mapRefreshAt &&
                (lastMapCenter - center).sqrMagnitude > 36f)
            {
                mapRefreshAt = Time.unscaledTime + 1.7f;
                lastMapCenter = center;
                SectorTacticalMapRaster.Write(
                    mapPixels, center, diameter,
                    SectorTacticalMapRaster.Resolution);
                mapTexture.SetPixels32(mapPixels);
                mapTexture.Apply(false, false);
            }

            // Rotate the arrow clockwise when facing East, and keep the
            // top edge of the raster fixed to geographic North.
            if (playerArrow != null)
                playerArrow.localRotation = Quaternion.Euler(
                    0f, 0f, -player.transform.eulerAngles.y);

            mapCoordsLabel.text =
                "X " + pos.x.ToString("0") +
                "      Z " + pos.z.ToString("0") +
                "      [M]  ATLAS";
            mapScaleLabel.text = diameter.ToString("0") + " M";

            BuildPoiMapMarkersIfReady();
            foreach (MapPoiMarker marker in mapPoiMarkers)
            {
                Vector2 position = new Vector2(
                    marker.poi.Position.x, marker.poi.Position.z);
                Vector2 uv = SectorTacticalMapRaster.Project(
                    position, center, diameter);
                bool inside = uv.x >= .03f && uv.x <= .97f &&
                    uv.y >= .03f && uv.y <= .97f;
                marker.rect.gameObject.SetActive(inside);
                if (!inside)
                    continue;

                marker.rect.anchoredPosition = new Vector2(
                    uv.x * 214f - 3f, -(1f - uv.y) * 214f + 3f);
            }
        }

        void BuildPoiMapMarkersIfReady()
        {
            if (mapPoiMarkers.Count > 0 || mapPoiOverlay == null ||
                minimap == null || minimap.world == null ||
                !minimap.world.Ready)
                return;

            foreach (SectorPointOfInterest poi in
                minimap.world.PointsOfInterest)
            {
                if (poi.Name == "Spawn")
                    continue;

                RectTransform marker = RectAt(mapPoiOverlay,
                    "Location_" + poi.Name, 0f, 0f, 7f, 7f,
                    poi.MapColor);
                mapPoiMarkers.Add(new MapPoiMarker
                {
                    poi = poi,
                    rect = marker
                });
                marker.gameObject.SetActive(false);
            }
        }

        void BuildModal(RectTransform root)
        {
            RectTransform backdrop = Rect(root, "Equipment_Modal",
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            Image shade = Paint(backdrop, Backdrop);
            shade.raycastTarget = true;
            modalRoot = backdrop.gameObject;

            RectTransform panel = Rect(backdrop, "Equipment_Root",
                new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(.5f, .5f), Vector2.zero,
                new Vector2(1260f, 738f));
            Paint(panel, Panel);
            RectAt(panel, "Header_Accent", 0f, 0f, 1260f, 3f, Accent);
            Label(panel, "GameTitle", "DEAD SECTOR  /  SURVIVAL SYSTEM",
                30f, 16f, 510f, 26f, 18, White, FontStyle.Bold);
            Label(panel, "Build", "FIELD OPERATIONS   //   PROTOTYPE",
                30f, 44f, 430f, 18f, 11, Muted);

            MakeButton(panel, "TAB_INVENTORY",
                "INVENTORY  [I]", 792f, 22f, 167f, 43f,
                Cell, Accent, () => gameplay.ShowInventory());
            MakeButton(panel, "TAB_CRAFT",
                "CRAFTING  [C]", 965f, 22f, 164f, 43f,
                Cell, Accent, () => gameplay.ShowCrafting());
            MakeButton(panel, "CLOSE", "X", 1169f, 22f, 57f, 43f,
                new Color(.26f, .12f, .12f, 1f), White,
                () => gameplay.CloseInventoryPanels());

            inventoryPage = RectAt(panel, "Inventory_Content",
                0f, 84f, 1260f, 654f, Color.clear).gameObject;
            craftingPage = RectAt(panel, "Crafting_Content",
                0f, 84f, 1260f, 654f, Color.clear).gameObject;

            BuildInventory(inventoryPage.transform);
            BuildCrafting(craftingPage.transform);
            modalRoot.SetActive(false);
        }

        void BuildInventory(Transform root)
        {
            RectTransform left = RectAt(root, "Player_Equipment",
                24f, 13f, 322f, 626f, new Color(.06f, .076f, .074f, 1f));
            RectAt(left, "SectionIndicator", 0f, 0f, 3f, 40f, Accent);
            Label(left, "Title", "OPERATIVE  /  GEAR",
                15f, 9f, 276f, 23f, 15, White, FontStyle.Bold);

            // Minimal vector-like human figure: visual orientation only;
            // actual gear slots are functional buttons underneath.
            bodyPanel = RectAt(left, "Equipment_Outline",
                36f, 48f, 252f, 205f, new Color(.04f, .06f, .059f, 1f));
            RectAt(bodyPanel, "Head", 111f, 16f, 29f, 29f, Edge);
            RectAt(bodyPanel, "Torso", 97f, 52f, 56f, 75f, Edge);
            RectAt(bodyPanel, "LeftArm", 66f, 59f, 24f, 80f, Edge);
            RectAt(bodyPanel, "RightArm", 160f, 59f, 24f, 80f, Edge);
            RectAt(bodyPanel, "LeftLeg", 99f, 135f, 23f, 57f, Edge);
            RectAt(bodyPanel, "RightLeg", 128f, 135f, 23f, 57f, Edge);
            Label(bodyPanel, "SilhouetteLabel", "BODY  /  ARMOR",
                10f, 174f, 110f, 20f, 10, Muted);

            string[] names = { "HEAD", "CHEST", "LEGS", "FEET", "BACKPACK" };
            for (int i = 0; i < 5; i++)
            {
                int gearIndex = i;
                RectTransform tile = RectAt(left, "Gear_" + names[i],
                    13f, 261f + i * 65f, 296f, 58f, Cell);
                RectAt(tile, "Tier_Line", 0f, 0f, 3f, 58f, Muted);
                Label(tile, "GearType", names[i], 13f, 6f,
                    254f, 18f, 10, Muted, FontStyle.Bold);
                gearLabels[i] = Label(tile, "GearItem", "EMPTY",
                    13f, 26f, 264f, 25f, 13, White);
                Button click = tile.gameObject.AddComponent<Button>();
                click.targetGraphic = tile.GetComponent<Image>();
                tile.GetComponent<Image>().raycastTarget = true;
                click.onClick.AddListener(() =>
                    gameplay.RemoveGear((SectorEquipment.GearSlot)gearIndex));
                SectorGearDropTarget drop =
                    tile.gameObject.AddComponent<SectorGearDropTarget>();
                drop.Configure(this, gearIndex);
            }

            RectTransform right = RectAt(root, "Backpack",
                359f, 13f, 875f, 626f,
                new Color(.06f, .076f, .074f, 1f));
            RectAt(right, "SectionIndicator", 0f, 0f, 3f, 40f, Accent);
            Label(right, "Title", "BACKPACK  /  LOADOUT",
                17f, 9f, 490f, 23f, 15, White, FontStyle.Bold);
            bagSummary = Label(right, "Capacity", "",
                548f, 9f, 304f, 23f, 12, Accent,
                FontStyle.Bold, TextAnchor.MiddleRight);

            RectAt(right, "GridBackdrop", 14f, 44f, 577f, 474f,
                new Color(.036f, .047f, .046f, 1f));

            for (int i = 0; i < 30; i++)
            {
                int slot = i;
                float x = 20f + (i % 6) * 94f;
                float y = 51f + (i / 6) * 92f;
                Button button = MakeButton(right, "BagCell_" + i,
                    "", x, y, 86f, 82f, Cell, White,
                    () => SelectBagCell(slot));
                bagBackgrounds[i] = button.GetComponent<Image>();
                bagCategoryStripes[i] = RectAt(button.transform,
                    "CategoryStripe", 0f, 0f, 3f, 82f, Muted)
                    .GetComponent<Image>();
                bagTexts[i] = button.GetComponentInChildren<Text>();
                bagTexts[i].fontSize = 12;
                bagTexts[i].alignment = TextAnchor.MiddleCenter;
                SectorBagDragSource drag =
                    button.gameObject.AddComponent<SectorBagDragSource>();
                drag.Configure(this, slot);
            }

            RectTransform details = RectAt(right, "Selected_Item",
                606f, 44f, 253f, 474f,
                new Color(.036f, .047f, .046f, 1f));
            RectAt(details, "SelectionAccent", 0f, 0f, 253f, 3f, Accent);
            Label(details, "Label", "SELECTED ITEM", 15f, 16f,
                225f, 24f, 12, Muted, FontStyle.Bold);
            selectedTitle = Label(details, "ItemTitle", "NO ITEM",
                15f, 56f, 221f, 68f, 19, White, FontStyle.Bold);
            selectedDetails = Label(details, "ItemMeta", "",
                15f, 139f, 222f, 200f, 13, Muted);
            useButton = MakeButton(details, "UseItem", "USE",
                13f, 365f, 226f, 43f,
                new Color(.18f, .28f, .19f, 1f), White,
                () => gameplay.UseInventoryItem(selectedId));
            equipButton = MakeButton(details, "EquipItem", "EQUIP",
                13f, 416f, 226f, 43f,
                new Color(.18f, .28f, .19f, 1f), White,
                () => gameplay.EquipInventoryItem(selectedId));

            RectTransform footer = RectAt(right, "Inventory_Hint",
                14f, 531f, 844f, 79f,
                new Color(.035f, .047f, .044f, 1f));
            Label(footer, "Help", "CLICK AN ITEM TO INSPECT",
                14f, 11f, 620f, 24f, 12, Accent, FontStyle.Bold);
            Label(footer, "HelpSecondary",
                "DRAG ARMOR TO GEAR SLOT    /    CLICK TO INSPECT    /    USE OR EQUIP ACTIONS",
                14f, 43f, 815f, 22f, 11, Muted);
        }

        void BuildCrafting(Transform root)
        {
            RectTransform left = RectAt(root, "RecipesPanel",
                24f, 13f, 850f, 626f, new Color(.06f, .076f, .074f, 1f));
            RectAt(left, "Indicator", 0f, 0f, 3f, 40f, Accent);
            Label(left, "Header", "FIELD WORKSHOP  /  RECIPES",
                18f, 10f, 660f, 25f, 16, White, FontStyle.Bold);

            RectTransform viewport = RectAt(left, "Scroll_Viewport",
                14f, 46f, 819f, 555f, Color.clear);
            viewport.GetComponent<Image>().raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            ScrollRect scroll = left.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            int count = SectorCrafting.Recipes.Count;
            RectTransform content = Rect(viewport, "Recipe_Scroll_Content",
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), Vector2.zero,
                new Vector2(813f, count * 78f + 12f));
            scroll.content = content;

            for (int i = 0; i < count; i++)
            {
                SectorRecipe recipe = SectorCrafting.Recipes[i];
                RectTransform row = RectAt(content, "Recipe_" + recipe.Id,
                    4f, i * 78f + 5f, 795f, 70f, Cell);
                RectAt(row, "LeftAccent", 0f, 0f, 3f, 70f,
                    recipe.Tier == 0 ? Accent :
                    recipe.Tier == 1 ? new Color(.87f, .68f, .38f) :
                    new Color(.53f, .69f, .87f));
                Label(row, "Tier", "TIER " + recipe.Tier, 14f,
                    4f, 75f, 20f, 11, Muted, FontStyle.Bold);
                Label(row, "RecipeName", recipe.Name,
                    95f, 4f, 500f, 22f, 15, White, FontStyle.Bold);
                Text ingredients = Label(row, "Requirements", "",
                    15f, 34f, 628f, 26f, 12, Muted);
                Button action = MakeButton(row, "MakeRecipe", "CRAFT",
                    670f, 13f, 111f, 42f,
                    new Color(.19f, .31f, .23f, 1f), White,
                    () => gameplay.CraftRecipe(recipe.Id));
                recipeDisplays.Add(new RecipeDisplay
                {
                    recipe = recipe,
                    description = ingredients,
                    craftButton = action,
                    background = row.GetComponent<Image>()
                });
            }

            RectTransform right = RectAt(root, "CraftingInfo",
                891f, 13f, 343f, 626f, new Color(.06f, .076f, .074f, 1f));
            RectAt(right, "Indicator", 0f, 0f, 3f, 40f, Accent);
            Label(right, "Title", "PROGRESSION", 17f, 10f, 295f, 25f,
                16, White, FontStyle.Bold);
            Label(right, "Hint",
                "01  WILDERNESS\n\nGather stone, sticks and plant fiber.\n\n" +
                "02  PRIMITIVE\n\nMake wooden tools and protection.\n\n" +
                "03  TEXTILES\n\nWeave cotton into clothes, a backpack and medical gear.",
                17f, 64f, 301f, 385f, 14, Muted);
            RectTransform summaryPanel = RectAt(right, "Resources",
                17f, 486f, 305f, 111f,
                new Color(.037f, .049f, .047f, 1f));
            craftingSummary = Label(summaryPanel, "BagSummary", "",
                14f, 10f, 282f, 80f, 13, Accent, FontStyle.Bold);
        }

        void Update()
        {
            if (gameplay == null || player == null || !player.Ready)
                return;

            if (Time.unscaledTime < refreshTime)
                return;

            refreshTime = Time.unscaledTime + .12f;
            bool atlasOpen = minimap != null && minimap.TacticalOpen;
            if (mainCanvas != null)
                mainCanvas.enabled = !atlasOpen;
            if (atlasOpen)
                return;

            RefreshHud();
            RefreshCompass();
            RefreshMinimap();
            RefreshModal();
        }

        void RefreshHud()
        {
            float[] values =
            {
                player.Health,
                gameplay.Needs != null ? gameplay.Needs.stamina : 100f,
                gameplay.Needs != null ? gameplay.Needs.hunger : 100f,
                gameplay.Needs != null ? gameplay.Needs.thirst : 100f
            };

            for (int i = 0; i < values.Length; i++)
            {
                meterValues[i].text = Mathf.CeilToInt(values[i]).ToString("000");
                RectTransform fill = meterFill[i].rectTransform;
                Vector2 size = fill.sizeDelta;
                size.x = 221f * Mathf.Clamp01(values[i] / 100f);
                fill.sizeDelta = size;
            }

            for (int i = 0; i < 3; i++)
            {
                string id = gameplay.WeaponInSlot(i);
                string name = SectorItems.TryGet(id, out SectorItemDefinition item)
                    ? item.Label : i == 2 ? "FISTS" : "EMPTY";
                hotbarLabels[i].text = name;
                hotbarAccent[i].color = gameplay.ActiveWeaponSlot == i
                    ? Accent : Edge;
            }

            float hour = clock != null ? clock.hourOfDay : 12f;
            int h = Mathf.FloorToInt(hour);
            int m = Mathf.FloorToInt((hour - h) * 60f);
            int day = clock != null ? clock.DayNumber : 1;
            dayLabel.text = "DAY " + day.ToString("00") + "  /  " +
                h.ToString("00") + ":" + m.ToString("00");

            bool modal = gameplay.InventoryOpen || gameplay.CraftingOpen;
            string hint = modal ? "" : gameplay.InteractionHint();
            hintRoot.SetActive(!string.IsNullOrEmpty(hint));
            if (!string.IsNullOrEmpty(hint))
                hintText.text = hint;

            string toast = gameplay.RecentMessage;
            messageRoot.SetActive(!string.IsNullOrEmpty(toast));
            if (!string.IsNullOrEmpty(toast))
                messageText.text = toast;
        }

        void RefreshModal()
        {
            bool inventory = gameplay.InventoryOpen;
            bool crafting = gameplay.CraftingOpen;
            bool active = inventory || crafting;
            if (modalRoot.activeSelf != active)
                modalRoot.SetActive(active);

            if (!active)
            {
                lastInventory = lastCrafting = false;
                return;
            }

            if (lastInventory != inventory || lastCrafting != crafting)
            {
                inventoryPage.SetActive(inventory);
                craftingPage.SetActive(crafting);
                lastInventory = inventory;
                lastCrafting = crafting;
            }

            if (inventory)
                RefreshInventory();
            else if (crafting)
                RefreshCrafting();
        }

        public string InventoryItemAt(int slot)
        {
            if (gameplay == null || slot < 0 ||
                slot >= gameplay.Inventory.Stacks.Count)
                return "";
            return gameplay.Inventory.Stacks[slot].id;
        }

        public void BeginItemDrag(string id, Vector2 pointerPosition)
        {
            if (canvasObject == null || string.IsNullOrEmpty(id))
                return;

            EndItemDrag();
            RectTransform canvas = canvasObject.GetComponent<RectTransform>();
            dragGhost = Rect(canvas, "Dragging_Item",
                new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(.5f, .5f), Vector2.zero,
                new Vector2(192f, 52f));
            Paint(dragGhost, new Color(.18f, .28f, .21f, .95f));
            dragGhostText = Label(dragGhost, "DraggedLabel",
                SectorItems.Get(id).Label,
                12f, 4f, 168f, 44f, 13, White,
                FontStyle.Bold, TextAnchor.MiddleCenter);
            dragGhost.SetAsLastSibling();
            MoveItemDrag(pointerPosition);
        }

        public void MoveItemDrag(Vector2 pointerPosition)
        {
            if (dragGhost == null || canvasObject == null)
                return;

            RectTransform canvas = canvasObject.GetComponent<RectTransform>();
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas, pointerPosition, null, out Vector2 local))
                dragGhost.anchoredPosition = local +
                    new Vector2(45f, 35f);
        }

        public void EndItemDrag()
        {
            if (dragGhost != null)
                Destroy(dragGhost.gameObject);
            dragGhost = null;
            dragGhostText = null;
        }

        public void EquipDraggedArmor(int targetSlot, string itemId)
        {
            SectorEquipment.GearSlot? matching =
                SectorEquipment.SlotFor(itemId);
            if (matching == null || (int)matching.Value != targetSlot ||
                gameplay == null || gameplay.Inventory.Count(itemId) == 0)
                return;

            gameplay.EquipInventoryItem(itemId);
            RefreshInventory();
        }

        void SelectBagCell(int slot)
        {
            if (gameplay == null || slot < 0 ||
                slot >= gameplay.Inventory.Stacks.Count)
                return;

            selectedId = gameplay.Inventory.Stacks[slot].id;
            RefreshInventory();
        }

        void RefreshInventory()
        {
            SectorInventory inventory = gameplay.Inventory;
            bagSummary.text = inventory.UsedSlots + "/" +
                inventory.SlotLimit + " SLOTS   /   " +
                inventory.Weight.ToString("0.0") + " / " +
                inventory.MaxWeight.ToString("0.0") + " KG";

            if (inventory.Count(selectedId) == 0)
                selectedId = inventory.Stacks.Count > 0
                    ? inventory.Stacks[0].id : "";

            for (int i = 0; i < bagTexts.Length; i++)
            {
                if (i < inventory.Stacks.Count)
                {
                    SectorItemStack stack = inventory.Stacks[i];
                    SectorItemDefinition item = SectorItems.Get(stack.id);
                    bagTexts[i].text = item.Label + "\n×" + stack.count;
                    bagBackgrounds[i].color = selectedId == stack.id
                        ? new Color(.20f, .32f, .24f, 1f) : Cell;
                    bagCategoryStripes[i].color = CategoryColor(item.Kind);
                }
                else
                {
                    bagTexts[i].text = i < inventory.SlotLimit ? "·" : "—";
                    bagBackgrounds[i].color =
                        i < inventory.SlotLimit ? Cell : Panel;
                    bagCategoryStripes[i].color = Edge;
                }
            }

            for (int i = 0; i < gearLabels.Length; i++)
            {
                string id = gameplay.Armor != null
                    ? gameplay.Armor.Equipped((SectorEquipment.GearSlot)i)
                    : "";
                gearLabels[i].text =
                    SectorItems.TryGet(id, out SectorItemDefinition item)
                        ? item.Label + "   [REMOVE]" : "EMPTY";
            }

            if (SectorItems.TryGet(selectedId, out SectorItemDefinition selected))
            {
                selectedTitle.text = selected.Label;
                selectedDetails.text = selected.Kind + "\n\n" +
                    "WEIGHT   " + selected.Weight.ToString("0.00") + " KG\n" +
                    "STACK   " + inventory.Count(selected.Id) + "\n\n" +
                    (selected.Kind == SectorItemKind.Melee ||
                     selected.Kind == SectorItemKind.Firearm
                        ? "DAMAGE   " + selected.Damage.ToString("0") :
                    selected.IsConsumable ? "SURVIVAL CONSUMABLE" :
                    selected.Kind == SectorItemKind.Armor ? "EQUIPMENT" :
                    "CRAFTING MATERIAL");
                useButton.interactable = selected.IsConsumable;
                equipButton.interactable =
                    selected.Kind == SectorItemKind.Armor ||
                    selected.Kind == SectorItemKind.Melee ||
                    selected.Kind == SectorItemKind.Firearm;
            }
            else
            {
                selectedTitle.text = "NO ITEM";
                selectedDetails.text = "Choose an item from your backpack.";
                useButton.interactable = false;
                equipButton.interactable = false;
            }
        }

        static Color CategoryColor(SectorItemKind kind)
        {
            switch (kind)
            {
                case SectorItemKind.Medical: return new Color(.70f, .42f, .37f);
                case SectorItemKind.Food:
                case SectorItemKind.Drink: return new Color(.55f, .72f, .82f);
                case SectorItemKind.Armor: return Accent;
                case SectorItemKind.Firearm:
                case SectorItemKind.Melee: return new Color(.85f, .69f, .39f);
                default: return Muted;
            }
        }

        void RefreshCrafting()
        {
            foreach (RecipeDisplay row in recipeDisplays)
            {
                bool canCraft = SectorCrafting.CanCraft(
                    gameplay.Inventory, row.recipe);
                row.craftButton.interactable = canCraft;
                row.background.color = canCraft
                    ? new Color(.092f, .145f, .112f, 1f) : Cell;

                string requirements = "";
                foreach (SectorIngredient ingredient in row.recipe.Ingredients)
                {
                    if (requirements.Length > 0)
                        requirements += "   +   ";
                    int have = gameplay.Inventory.Count(ingredient.ItemId);
                    requirements += SectorItems.Get(ingredient.ItemId).Label +
                        " " + have + "/" + ingredient.Count;
                }
                row.description.text = requirements;
                row.description.color = canCraft ? Accent : Muted;
            }

            craftingSummary.text =
                "BAG SLOTS   " + gameplay.Inventory.UsedSlots +
                "/" + gameplay.Inventory.SlotLimit +
                "\nWEIGHT   " + gameplay.Inventory.Weight.ToString("0.0") +
                "/" + gameplay.Inventory.MaxWeight.ToString("0.0") +
                " KG";
        }

        static RectTransform Rect(
            Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name,
                typeof(RectTransform));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        static RectTransform RectAt(
            Transform parent, string name, float x, float y,
            float width, float height, Color color)
        {
            RectTransform rect = Rect(parent, name,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(x, -y),
                new Vector2(width, height));
            Paint(rect, color);
            return rect;
        }

        static Image Paint(RectTransform rect, Color color)
        {
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        Text Label(Transform parent, string name, string text,
            float x, float y, float width, float height,
            int fontSize, Color color,
            FontStyle fontStyle = FontStyle.Normal,
            TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            RectTransform rect = Rect(parent, name,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(x, -y),
                new Vector2(width, height));
            Text label = rect.gameObject.AddComponent<Text>();
            label.font = uiFont;
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.color = color;
            label.alignment = alignment;
            label.text = text;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        Button MakeButton(Transform parent, string name,
            string caption, float x, float y,
            float width, float height, Color background, Color foreground,
            Action click)
        {
            RectTransform buttonRect = RectAt(
                parent, name, x, y, width, height, background);
            Button button = buttonRect.gameObject.AddComponent<Button>();
            Image panelImage = buttonRect.GetComponent<Image>();
            panelImage.raycastTarget = true;
            button.targetGraphic = panelImage;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.16f, 1.16f, 1.16f);
            colors.pressedColor = new Color(.7f, .81f, .7f);
            colors.disabledColor = new Color(.42f, .42f, .42f);
            button.colors = colors;

            Label(buttonRect, "Caption", caption, 6f, 3f,
                width - 12f, height - 6f, 13, foreground,
                FontStyle.Bold, TextAnchor.MiddleCenter);
            if (click != null)
                button.onClick.AddListener(() => click());
            return button;
        }

        void OnDestroy()
        {
            if (gameplay != null)
                gameplay.ModernUiEnabled = false;
            if (minimap != null)
                minimap.SetCanvasHudActive(false);
            if (compass != null)
                compass.enabled = true;
            if (mapTexture != null)
                Destroy(mapTexture);
            if (uiFont != null)
                Destroy(uiFont);
        }
    }


}
