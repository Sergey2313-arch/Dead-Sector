using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeadSector
{
    /// <summary>
    /// Original tactical main/pause/settings overlay. Runtime Canvas avoids
    /// overwriting the user's scene, menus own Escape without touching
    /// previous save files, and PlayerPrefs stores only display/input values.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SectorMenuUI : MonoBehaviour
    {
        const string PrefVolume = "DeadSector.Audio.MasterV1";
        const string PrefMouse = "DeadSector.Input.LookV1";
        const string PrefFov = "DeadSector.Camera.FovV1";
        const string PrefQuality = "DeadSector.Video.QualityV1";

        static readonly Color Black = new Color(.012f, .018f, .018f, .97f);
        static readonly Color Panel = new Color(.045f, .059f, .056f, 1f);
        static readonly Color PanelLight = new Color(.083f, .12f, .103f, 1f);
        static readonly Color Accent = new Color(.67f, .82f, .56f, 1f);
        static readonly Color White = new Color(.94f, .95f, .92f, 1f);
        static readonly Color Muted = new Color(.54f, .64f, .59f, 1f);

        SectorGameplay gameplay;
        SectorPlayer player;
        SectorMinimap minimap;
        Font font;
        GameObject canvasObject;
        GameObject homePage;
        GameObject settingsPage;
        CanvasScaler scaler;
        Slider volumeSlider;
        Slider mouseSlider;
        Slider fovSlider;
        Text volumeValue;
        Text mouseValue;
        Text fovValue;
        Text qualityValue;
        Text menuSubtitle;
        bool pendingTitle = true;
        bool open;
        bool settings;
        bool initialTitle;
        float priorTimeScale = 1f;

        public bool IsOpen => open;
        public bool IsSettingsOpen => settings;

        public bool Configure(SectorGameplay game,
            SectorPlayer avatar, SectorMinimap map)
        {
            if (game == null || avatar == null)
                return false;

            gameplay = game;
            player = avatar;
            minimap = map;

            try
            {
                Build();
                RestoreSettings();
                player.EscapeHandledByUi = true;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("[Dead Sector Menu] Setup failed: " + e);
                if (canvasObject != null)
                    Destroy(canvasObject);
                enabled = false;
                return false;
            }
        }

        void Build()
        {
            font = Font.CreateDynamicFontFromOSFont(
                new[] { "Segoe UI", "Arial", "Liberation Sans" }, 16);

            canvasObject = new GameObject("DeadSector_MainPauseCanvas",
                typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 420;

            scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;

            RectTransform root = canvasObject.GetComponent<RectTransform>();
            Image veil = Background(root, "Backdrop",
                Vector2.zero, Vector2.one,
                new Vector2(.5f, .5f), Vector2.zero, Vector2.zero, Black);
            veil.raycastTarget = true;

            // Left region: large typography, compact buttons and
            // restrained green geometry — no third-party background art.
            RectTransform left = Rect(root, "MenuFrame",
                new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(.5f, .5f), Vector2.zero,
                new Vector2(1090f, 680f));
            Background(left, "LeftBar", Vector2.zero, Vector2.zero,
                new Vector2(0f, 1f), new Vector2(0f, 0f),
                new Vector2(5f, 680f), Accent);
            Label(left, "SmallBrand", "DEAD SECTOR    /    SURVIVAL PROTOCOL",
                34f, 24f, 550f, 22f, 15, Accent, FontStyle.Bold);
            Label(left, "Title1", "DEAD", 25f, 72f,
                780f, 122f, 88, White, FontStyle.Bold);
            Label(left, "Title2", "SECTOR", 25f, 171f,
                840f, 125f, 86, White, FontStyle.Bold);
            Label(left, "Tagline", "NO SIGNAL. NO RESCUE. SURVIVE.",
                34f, 303f, 630f, 24f, 16, Muted);
            Background(left, "Divider", new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(34f, -348f), new Vector2(610f, 2f), Accent);

            // Decorative operational status — keeps title screen from
            // feeling like a default stock Unity dialog.
            RectTransform status = At(left, "DeploymentInfo",
                693f, 70f, 369f, 462f, Panel);
            At(status, "SignalLine", 0f, 0f, 369f, 3f, Accent);
            Label(status, "StatusHeader", "FIELD OPERATIONS",
                22f, 20f, 315f, 30f, 19, White, FontStyle.Bold);
            Label(status, "Signal",
                "SECTOR     08 x 08 KM\n\n" +
                "LOCATION   UNKNOWN\n\n" +
                "THREAT      INFECTED\n\n" +
                "ACCESS      RESTRICTED\n\n" +
                "MODE        SINGLE PLAYER",
                22f, 75f, 320f, 290f, 17, Muted);
            Label(status, "Footnote",
                "PROTOTYPE  /  BUILD IN PROGRESS",
                22f, 408f, 327f, 29f, 12, Accent);

            homePage = At(left, "HomeActions", 22f, 363f,
                640f, 294f, Color.clear).gameObject;
            MakeButton(homePage.transform, "ResumeButton",
                "ВОЙТИ В СЕКТОР   /   CONTINUE",
                14f, 0f, 590f, 62f, PanelLight, Accent,
                () => Resume());
            MakeButton(homePage.transform, "SettingsButton",
                "НАСТРОЙКИ   /   SETTINGS",
                14f, 73f, 590f, 52f, Panel, White,
                () => ShowSettings());
            MakeButton(homePage.transform, "SaveButton",
                "СОХРАНИТЬ ПРОГРЕСС   /   F5",
                14f, 137f, 590f, 50f, Panel, White,
                () => SaveFromMenu());
            MakeButton(homePage.transform, "ExitButton",
                "ВЫЙТИ ИЗ ИГРЫ",
                14f, 202f, 590f, 50f,
                new Color(.19f, .084f, .073f), White,
                () => ExitGame());
            menuSubtitle = Label(homePage.transform, "Instructions",
                "ESC  —  ПАУЗА  /  ПРОДОЛЖИТЬ",
                20f, 259f, 574f, 24f, 12, Muted);

            settingsPage = At(left, "SettingsPage", 22f, 363f,
                640f, 299f, Color.clear).gameObject;
            Label(settingsPage.transform, "SettingsTitle",
                "SYSTEM CONFIGURATION",
                14f, 1f, 590f, 31f, 20, White, FontStyle.Bold);
            volumeSlider = MakeSlider(settingsPage.transform,
                "MASTER VOLUME", 44f, 0f, 1f, out volumeValue,
                v => ChangeVolume(v));
            mouseSlider = MakeSlider(settingsPage.transform,
                "MOUSE SENSITIVITY", 109f, .35f, 2.5f,
                out mouseValue, v => ChangeMouse(v));
            fovSlider = MakeSlider(settingsPage.transform,
                "FIELD OF VIEW", 174f, 60f, 100f,
                out fovValue, v => ChangeFov(v));
            MakeButton(settingsPage.transform, "QualityButton",
                "GRAPHICS QUALITY  < CHANGE >", 14f, 239f,
                405f, 40f, PanelLight, White,
                () => CycleQuality());
            qualityValue = Label(settingsPage.transform, "QualityName",
                "", 432f, 242f, 161f, 35f, 12, Accent,
                FontStyle.Bold, TextAnchor.MiddleRight);
            MakeButton(left, "BackButton", "НАЗАД",
                39f, 632f, 136f, 32f, PanelLight, White,
                () => ShowHome());
            settingsPage.SetActive(false);
            canvasObject.SetActive(false);
        }

        void Update()
        {
            if (gameplay == null || player == null)
                return;

            // The streamed world must finish loading first. Pausing before
            // this point can stall terrain initialization coroutines.
            if (pendingTitle && player.Ready &&
                gameplay.HasLoadedInitialSave)
            {
                pendingTitle = false;
                initialTitle = true;
                OpenMenu();
                return;
            }

            if (pendingTitle || !SectorInput.Pressed(KeyCode.Escape))
                return;

            if (open)
            {
                if (settings)
                    ShowHome();
                else
                    Resume();
                return;
            }

            if (gameplay.InventoryOpen || gameplay.CraftingOpen)
            {
                gameplay.CloseInventoryPanels();
                return;
            }

            if (minimap != null && minimap.TacticalOpen)
            {
                minimap.CloseTacticalMap();
                return;
            }

            OpenMenu();
        }

        void OpenMenu()
        {
            if (open || gameplay == null || player == null)
                return;

            open = true;
            priorTimeScale = Mathf.Max(.001f, Time.timeScale);
            gameplay.SetExternalUiBlocking(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0f;

            canvasObject.SetActive(true);
            menuSubtitle.text = initialTitle
                ? "ИГРА ЗАГРУЖЕНА  /  НАЖМИ CONTINUE"
                : "ESC  —  ПРОДОЛЖИТЬ";
            ShowHome();
        }

        public void Resume()
        {
            if (!open)
                return;

            open = false;
            initialTitle = false;
            settings = false;
            canvasObject.SetActive(false);
            Time.timeScale = priorTimeScale;
            gameplay.SetExternalUiBlocking(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void ShowHome()
        {
            settings = false;
            homePage.SetActive(true);
            settingsPage.SetActive(false);
            UpdateSettingsLabels();
        }

        void ShowSettings()
        {
            settings = true;
            homePage.SetActive(false);
            settingsPage.SetActive(true);
            UpdateSettingsLabels();
        }

        void SaveFromMenu()
        {
            if (gameplay == null || !gameplay.HasLoadedInitialSave)
                return;
            gameplay.SaveGame();
        }

        static void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void RestoreSettings()
        {
            volumeSlider.SetValueWithoutNotify(
                Mathf.Clamp01(PlayerPrefs.GetFloat(PrefVolume, 1f)));
            mouseSlider.SetValueWithoutNotify(
                Mathf.Clamp(PlayerPrefs.GetFloat(PrefMouse, 1f),
                    .35f, 2.5f));
            fovSlider.SetValueWithoutNotify(
                Mathf.Clamp(PlayerPrefs.GetFloat(PrefFov, 75f),
                    60f, 100f));
            int quality = Mathf.Clamp(
                PlayerPrefs.GetInt(PrefQuality,
                    QualitySettings.GetQualityLevel()),
                0, Mathf.Max(0, QualitySettings.names.Length - 1));

            QualitySettings.SetQualityLevel(quality, true);
            ChangeVolume(volumeSlider.value);
            ChangeMouse(mouseSlider.value);
            ChangeFov(fovSlider.value);
            UpdateSettingsLabels();
        }

        void ChangeVolume(float value)
        {
            value = Mathf.Clamp01(value);
            AudioListener.volume = value;
            PlayerPrefs.SetFloat(PrefVolume, value);
            UpdateSettingsLabels();
        }

        void ChangeMouse(float value)
        {
            SectorInput.LookSensitivity = value;
            PlayerPrefs.SetFloat(PrefMouse, SectorInput.LookSensitivity);
            UpdateSettingsLabels();
        }

        void ChangeFov(float value)
        {
            float fov = Mathf.Clamp(value, 60f, 100f);
            if (player != null)
            {
                player.firstPersonFov = fov;
                player.thirdPersonFov = Mathf.Clamp(fov - 7f, 55f, 95f);
            }
            PlayerPrefs.SetFloat(PrefFov, fov);
            UpdateSettingsLabels();
        }

        void CycleQuality()
        {
            int count = QualitySettings.names.Length;
            if (count <= 0)
                return;

            int selected = (QualitySettings.GetQualityLevel() + 1) % count;
            QualitySettings.SetQualityLevel(selected, true);
            PlayerPrefs.SetInt(PrefQuality, selected);
            UpdateSettingsLabels();
        }

        void UpdateSettingsLabels()
        {
            if (volumeValue != null && volumeSlider != null)
                volumeValue.text =
                    Mathf.RoundToInt(volumeSlider.value * 100f) + "%";

            if (mouseValue != null && mouseSlider != null)
                mouseValue.text = mouseSlider.value.ToString("0.00") + " X";

            if (fovValue != null && fovSlider != null)
                fovValue.text = Mathf.RoundToInt(fovSlider.value) + "°";

            if (qualityValue != null)
            {
                int index = QualitySettings.GetQualityLevel();
                string[] names = QualitySettings.names;
                qualityValue.text = index >= 0 && index < names.Length
                    ? names[index] : "DEFAULT";
            }
        }

        Slider MakeSlider(Transform parent, string label,
            float top, float low, float high, out Text value,
            Action<float> onChange)
        {
            Label(parent, label + "Title", label,
                14f, top, 315f, 22f, 14, White, FontStyle.Bold);
            value = Label(parent, label + "Value",
                "", 489f, top, 116f, 22f, 14, Accent,
                FontStyle.Bold, TextAnchor.MiddleRight);

            RectTransform track = At(parent, label + "Track",
                15f, top + 28f, 588f, 13f, PanelLight);
            RectTransform fill = At(track, "Fill",
                0f, 0f, 588f, 13f, Accent);
            RectTransform handle = At(track, "Knob",
                0f, -5f, 19f, 23f, White);

            Slider slider = track.gameObject.AddComponent<Slider>();
            slider.minValue = low;
            slider.maxValue = high;
            slider.direction = Slider.Direction.LeftToRight;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            track.GetComponent<Image>().raycastTarget = true;
            slider.onValueChanged.AddListener(v => onChange(v));
            return slider;
        }

        static Image Background(Transform parent, string name,
            Vector2 min, Vector2 max, Vector2 pivot,
            Vector2 position, Vector2 size, Color tint)
        {
            RectTransform rect = Rect(parent, name,
                min, max, pivot, position, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = tint;
            image.raycastTarget = false;
            return image;
        }

        static RectTransform Rect(Transform parent, string name,
            Vector2 min, Vector2 max, Vector2 pivot,
            Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        static RectTransform At(Transform parent, string name,
            float x, float y, float width, float height, Color tint)
        {
            RectTransform rect = Rect(parent, name,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(x, -y),
                new Vector2(width, height));
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = tint;
            image.raycastTarget = false;
            return rect;
        }

        Text Label(Transform parent, string name, string text,
            float x, float y, float width, float height,
            int size, Color color, FontStyle style = FontStyle.Normal,
            TextAnchor align = TextAnchor.MiddleLeft)
        {
            RectTransform rect = Rect(parent, name,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(x, -y),
                new Vector2(width, height));
            Text label = rect.gameObject.AddComponent<Text>();
            label.font = font;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.text = text;
            label.alignment = align;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        Button MakeButton(Transform parent, string name, string label,
            float x, float y, float width, float height,
            Color background, Color foreground, Action onClick)
        {
            RectTransform rect = At(
                parent, name, x, y, width, height, background);
            Image image = rect.GetComponent<Image>();
            image.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.17f, 1.13f);
            colors.pressedColor = new Color(.7f, .8f, .7f);
            button.colors = colors;
            Label(rect, "Caption", label,
                15f, 5f, width - 30f, height - 10f,
                16, foreground, FontStyle.Bold);
            button.onClick.AddListener(() => onClick());
            return button;
        }

        void OnDisable()
        {
            Cleanup();
        }

        void OnDestroy()
        {
            Cleanup();
            if (font != null)
                Destroy(font);
        }

        void Cleanup()
        {
            if (!Application.isPlaying)
                return;

            if (open)
            {
                Time.timeScale = priorTimeScale;
                open = false;
            }

            if (gameplay != null)
                gameplay.SetExternalUiBlocking(false);

            if (player != null)
                player.EscapeHandledByUi = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
