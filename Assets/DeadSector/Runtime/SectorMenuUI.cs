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
        Text profileHelp;
        readonly Button[] profileButtons = new Button[4];
        readonly Button[] deleteButtons = new Button[4];
        readonly Text[] profileLabels = new Text[4];
        readonly Text[] profileStatuses = new Text[4];
        GameObject backButton;
        GameObject deleteConfirmation;
        Text deleteConfirmationText;
        int pendingDeleteSlot = -1;
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
            Background(left, "LeftBar",
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), Vector2.zero,
                new Vector2(5f, 680f), Accent);
            Label(left, "SmallBrand", "DEAD SECTOR    /    ПРОТОКОЛ ВЫЖИВАНИЯ",
                34f, 24f, 550f, 22f, 15, Accent, FontStyle.Bold);
            Label(left, "Title1", "DEAD", 25f, 72f,
                780f, 122f, 88, White, FontStyle.Bold);
            Label(left, "Title2", "SECTOR", 25f, 171f,
                840f, 125f, 86, White, FontStyle.Bold);
            Label(left, "Tagline", "НЕТ СИГНАЛА. НЕТ ПОМОЩИ. ВЫЖИВАЙ.",
                34f, 303f, 630f, 24f, 16, Muted);
            Background(left, "Divider", new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(34f, -348f), new Vector2(610f, 2f), Accent);

            // Decorative operational status — keeps title screen from
            // feeling like a default stock Unity dialog.
            RectTransform status = At(left, "ProfileSelection",
                693f, 70f, 369f, 462f, Panel);
            At(status, "SignalLine", 0f, 0f, 369f, 3f, Accent);
            Label(status, "StatusHeader", "СЛОТЫ СОХРАНЕНИЙ",
                19f, 13f, 335f, 31f, 19, White, FontStyle.Bold);
            Label(status, "ProfileHint",
                "ВЫБЕРИ СЛОТ  /  ПУСТОЙ — НОВАЯ ИГРА",
                19f, 44f, 333f, 19f, 11, Muted);

            for (int i = 0; i < profileButtons.Length; i++)
            {
                int slot = i;
                Button choice = MakeButton(status,
                    "Profile_" + slot, "",
                    14f, 73f + i * 77f, 271f, 68f,
                    PanelLight, White, () => SelectProfile(slot));
                profileButtons[i] = choice;
                Text label = choice.GetComponentInChildren<Text>();
                label.rectTransform.anchoredPosition =
                    new Vector2(12f, -7f);
                label.rectTransform.sizeDelta = new Vector2(244f, 25f);
                label.fontSize = 14;
                profileLabels[i] = label;
                profileStatuses[i] = Label(choice.transform,
                    "ProfileStatus", "", 15f, 32f, 244f, 26f,
                    11, Muted);

                if (slot > SectorSaveProfiles.LegacySlot)
                {
                    Button erase = MakeButton(status,
                        "DeleteProfile_" + slot, "УДАЛ.",
                        293f, 91f + i * 77f, 62f, 33f,
                        new Color(.24f, .086f, .077f), White,
                        () => RequestDeleteProfile(slot));
                    erase.GetComponentInChildren<Text>().fontSize = 12;
                    deleteButtons[slot] = erase;
                }
            }

            profileHelp = Label(status, "ProfileMessage",
                "УДАЛЕНИЕ ТОЛЬКО НЕАКТИВНЫХ ПРОФИЛЕЙ 01–03.",
                18f, 395f, 332f, 61f, 11, Accent);

            homePage = At(left, "HomeActions", 22f, 363f,
                640f, 294f, Color.clear).gameObject;
            MakeButton(homePage.transform, "ResumeButton",
                "НАЧАТЬ ИГРУ   /   ПРОДОЛЖИТЬ",
                14f, 0f, 590f, 62f, PanelLight, Accent,
                () => Resume());
            MakeButton(homePage.transform, "SettingsButton",
                "НАСТРОЙКИ",
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
                "НАСТРОЙКИ ИГРЫ",
                14f, 1f, 590f, 31f, 20, White, FontStyle.Bold);
            volumeSlider = MakeSlider(settingsPage.transform,
                "ОБЩАЯ ГРОМКОСТЬ", 44f, 0f, 1f, out volumeValue,
                v => ChangeVolume(v));
            mouseSlider = MakeSlider(settingsPage.transform,
                "ЧУВСТВИТЕЛЬНОСТЬ МЫШИ", 109f, .35f, 2.5f,
                out mouseValue, v => ChangeMouse(v));
            fovSlider = MakeSlider(settingsPage.transform,
                "ПОЛЕ ЗРЕНИЯ", 174f, 60f, 100f,
                out fovValue, v => ChangeFov(v));
            MakeButton(settingsPage.transform, "QualityButton",
                "КАЧЕСТВО ГРАФИКИ  < СМЕНИТЬ >", 14f, 239f,
                405f, 40f, PanelLight, White,
                () => CycleQuality());
            qualityValue = Label(settingsPage.transform, "QualityName",
                "", 432f, 242f, 161f, 35f, 12, Accent,
                FontStyle.Bold, TextAnchor.MiddleRight);
            backButton = MakeButton(left, "BackButton", "НАЗАД",
                39f, 632f, 136f, 32f, PanelLight, White,
                () => ShowHome()).gameObject;
            backButton.SetActive(false);
            settingsPage.SetActive(false);

            // Modal confirmation covers the entire menu. An opaque input
            // blocker prevents accidental profile switching or resuming
            // while the destructive action awaits explicit confirmation.
            Image modalBackdrop = Background(root, "DeleteConfirmationOverlay",
                Vector2.zero, Vector2.one,
                new Vector2(.5f, .5f), Vector2.zero, Vector2.zero,
                new Color(0f, 0f, 0f, .87f));
            modalBackdrop.raycastTarget = true;
            deleteConfirmation = modalBackdrop.gameObject;

            RectTransform confirm = Rect(
                deleteConfirmation.transform, "DeleteConfirmPanel",
                new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(.5f, .5f), Vector2.zero,
                new Vector2(570f, 270f));
            Image confirmImage = confirm.gameObject.AddComponent<Image>();
            confirmImage.color = Panel;
            confirmImage.raycastTarget = true;

            Label(confirm, "Heading", "УДАЛЕНИЕ СОХРАНЕНИЯ",
                30f, 22f, 512f, 35f, 25, White, FontStyle.Bold);
            deleteConfirmationText = Label(confirm, "Warning",
                "", 30f, 77f, 510f, 92f, 17, Accent);
            MakeButton(confirm, "ConfirmErase",
                "УДАЛИТЬ НАВСЕГДА",
                30f, 195f, 235f, 53f,
                new Color(.30f, .085f, .07f), White,
                () => ConfirmDeleteProfile());
            MakeButton(confirm, "CancelErase",
                "ОТМЕНА",
                290f, 195f, 250f, 53f,
                PanelLight, White, () => CancelDeleteProfile());
            deleteConfirmation.SetActive(false);
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

            // Death overlay owns the input once the player has died.
            // Keep the initial profile selector functional until Continue.
            if (pendingTitle || player.Health <= 0f ||
                !SectorInput.Pressed(KeyCode.Escape))
                return;

            if (open && pendingDeleteSlot >= 0)
            {
                CancelDeleteProfile();
                return;
            }

            SectorEscapeAction action = SectorMenuRules.OnEscape(
                open, settings,
                gameplay.InventoryOpen, gameplay.CraftingOpen,
                minimap != null && minimap.TacticalOpen,
                gameplay.Journal != null && gameplay.Journal.Visible);

            switch (action)
            {
                case SectorEscapeAction.CloseSettings:
                    ShowHome();
                    break;
                case SectorEscapeAction.Resume:
                    Resume();
                    break;
                case SectorEscapeAction.CloseInventory:
                    gameplay.CloseInventoryPanels();
                    break;
                case SectorEscapeAction.CloseJournal:
                    gameplay.SetJournalOpen(false);
                    break;
                case SectorEscapeAction.CloseAtlas:
                    if (minimap != null)
                        minimap.CloseTacticalMap();
                    break;
                case SectorEscapeAction.OpenPause:
                    OpenMenu();
                    break;
            }
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
                ? "ВЫБЕРИ СОХРАНЕНИЕ  /  ПРОДОЛЖИТЬ"
                : "ESC  —  ПРОДОЛЖИТЬ";
            ShowHome();
        }

        public void Resume()
        {
            if (!open)
                return;

            CancelDeleteProfile();
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
            CancelDeleteProfile();
            settings = false;
            homePage.SetActive(true);
            settingsPage.SetActive(false);
            backButton.SetActive(false);
            RefreshProfiles();
            UpdateSettingsLabels();
        }

        void ShowSettings()
        {
            CancelDeleteProfile();
            settings = true;
            homePage.SetActive(false);
            settingsPage.SetActive(true);
            backButton.SetActive(true);
            UpdateSettingsLabels();
        }

        void SaveFromMenu()
        {
            if (gameplay == null || !gameplay.HasLoadedInitialSave)
                return;
            if (gameplay.TrySaveGame())
                profileHelp.text = "СОХРАНЕНО: " + gameplay.ActiveProfileName;
            else
                profileHelp.text = "ОШИБКА СОХРАНЕНИЯ — СМОТРИ КОНСОЛЬ";
            RefreshProfiles();
        }

        void RefreshProfiles()
        {
            if (gameplay == null || profileButtons[0] == null)
                return;

            for (int slot = 0; slot < profileButtons.Length; slot++)
            {
                bool occupied = SectorSaveProfiles.Exists(
                    Application.persistentDataPath, slot);
                bool valid = occupied && SectorSaveProfiles.TryRead(
                    Application.persistentDataPath, slot,
                    out SectorGameSave _);
                bool active = slot == gameplay.ActiveSaveSlot;

                profileButtons[slot].interactable =
                    (valid || (!occupied && slot > 0) ||
                     (slot == 0 && active));
                profileLabels[slot].text =
                    SectorSaveProfiles.Name(slot) +
                    (active ? "    ● АКТИВНЫЙ" : "");
                profileStatuses[slot].text =
                    gameplay.ProfileStatus(slot);
                if (slot > SectorSaveProfiles.LegacySlot &&
                    deleteButtons[slot] != null)
                    deleteButtons[slot].interactable = occupied && !active;
            }
        }

        void RequestDeleteProfile(int slot)
        {
            if (!open || gameplay == null ||
                !gameplay.HasLoadedInitialSave ||
                slot <= SectorSaveProfiles.LegacySlot ||
                slot > SectorSaveProfiles.MaxProfileSlot)
                return;

            if (slot == gameplay.ActiveSaveSlot)
            {
                profileHelp.text =
                    "АКТИВНЫЙ СЛОТ: СНАЧАЛА ВЫБЕРИ ДРУГОЙ ПРОФИЛЬ.";
                return;
            }

            if (!SectorSaveProfiles.Exists(
                Application.persistentDataPath, slot))
            {
                profileHelp.text = "СЛОТ УЖЕ ПУСТ.";
                RefreshProfiles();
                return;
            }

            pendingDeleteSlot = slot;
            deleteConfirmationText.text =
                SectorSaveProfiles.Name(slot) +
                "\nФайл сохранения и его резервная копия будут удалены." +
                "\nОтменить удаление после подтверждения нельзя.";
            deleteConfirmation.SetActive(true);
        }

        void CancelDeleteProfile()
        {
            pendingDeleteSlot = -1;
            if (deleteConfirmation != null)
                deleteConfirmation.SetActive(false);
        }

        void ConfirmDeleteProfile()
        {
            int slot = pendingDeleteSlot;
            if (slot < 1 || slot > SectorSaveProfiles.MaxProfileSlot)
                return;

            // Never delete the original legacy file or the active timeline.
            bool success = gameplay != null &&
                gameplay.DeleteProfile(slot);
            CancelDeleteProfile();
            RefreshProfiles();

            profileHelp.text = success
                ? "УДАЛЕНО: " + SectorSaveProfiles.Name(slot) +
                  " — МОЖНО СОЗДАТЬ НОВУЮ ИГРУ."
                : "НЕ УДАЛОСЬ УДАЛИТЬ — СМОТРИ КОНСОЛЬ.";
        }

        void SelectProfile(int slot)
        {
            if (gameplay == null || !gameplay.HasLoadedInitialSave ||
                pendingDeleteSlot >= 0 ||
                !SectorSaveProfiles.IsValidSlot(slot))
                return;

            if (slot == gameplay.ActiveSaveSlot)
            {
                profileHelp.text = "УЖЕ ВЫБРАН: " +
                    gameplay.ActiveProfileName;
                return;
            }

            // Once gameplay has begun, persist current progress BEFORE
            // switching. Never overwrite an unreadable profile.
            if (!initialTitle)
            {
                bool currentExists = SectorSaveProfiles.Exists(
                    Application.persistentDataPath,
                    gameplay.ActiveSaveSlot);
                if (currentExists && !SectorSaveProfiles.TryRead(
                    Application.persistentDataPath,
                    gameplay.ActiveSaveSlot, out SectorGameSave _))
                {
                    profileHelp.text =
                        "ТЕКУЩЕЕ СОХРАНЕНИЕ ПОВРЕЖДЕНО — СМЕНА ОТМЕНЕНА";
                    return;
                }

                if (!gameplay.TrySaveGame(true))
                {
                    profileHelp.text =
                        "НЕ УДАЛОСЬ СОХРАНИТЬ — СМЕНА ОТМЕНЕНА";
                    return;
                }
            }

            bool exists = SectorSaveProfiles.Exists(
                Application.persistentDataPath, slot);

            bool success = exists
                ? gameplay.LoadProfile(slot)
                : gameplay.CreateNewProfile(slot);

            if (success)
                profileHelp.text = "ГОТОВО: " + gameplay.ActiveProfileName;
            else
                profileHelp.text = exists
                    ? "ОШИБКА ЗАГРУЗКИ — СОХРАНЕНИЕ НЕ ИЗМЕНЕНО"
                    : "НЕ УДАЛОСЬ СОЗДАТЬ СЛОТ";

            RefreshProfiles();
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
                    ? SectorRussian.Quality(names[index]) : "ПО УМОЛЧАНИЮ";
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
