using UnityEngine;
using UnityEngine.UI;

namespace DeadSector
{
    /// <summary>
    /// Non-destructive death state. The current live save remains intact
    /// until explicitly reloaded; a player can instead respawn at camp.
    /// </summary>
    public sealed class SectorDeathUI : MonoBehaviour
    {
        SectorGameplay gameplay;
        SectorPlayer player;
        SectorMenuUI frontEnd;
        GameObject root;
        Button restoreButton;
        Text status;
        Font font;
        bool visible;
        float restoreTimeScale = 1f;

        public bool IsVisible => visible;

        public bool Configure(SectorGameplay source, SectorPlayer avatar)
        {
            if (source == null || avatar == null)
                return false;

            gameplay = source;
            player = avatar;
            frontEnd = FindFirstObjectByType<SectorMenuUI>();
            try
            {
                Build();
                return true;
            }
            catch (System.Exception error)
            {
                Debug.LogError("[Dead Sector] Death UI failed: " + error);
                enabled = false;
                return false;
            }
        }

        void Build()
        {
            font = Font.CreateDynamicFontFromOSFont(
                new[] { "Segoe UI", "Arial" }, 16);

            root = new GameObject("DeadSector_DeathCanvas",
                typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 510;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;

            RectTransform baseRect = root.GetComponent<RectTransform>();
            Image backdrop = Rect(baseRect, "Death_Backdrop",
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(.5f, .5f), Vector2.zero, Vector2.zero)
                .gameObject.AddComponent<Image>();
            backdrop.color = new Color(.015f, .012f, .012f, .95f);
            backdrop.raycastTarget = true;

            RectTransform frame = Rect(baseRect, "DeathFrame",
                new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(.5f, .5f), Vector2.zero,
                new Vector2(770f, 560f));

            AddImage(frame, "Warning", 0f, 0f, 6f, 560f,
                new Color(.73f, .22f, .19f));
            Label(frame, "Chapter", "DEAD SECTOR  /  СМЕРТЬ",
                30f, 20f, 700f, 30f, 16,
                new Color(.77f, .45f, .42f));
            Label(frame, "Title", "ВЫ ПОГИБЛИ", 18f, 89f,
                750f, 125f, 87, Color.white);
            Label(frame, "Description",
                "ВЫЖИВШИЙ ПОГИБ. ПОСЛЕДНЕЕ СОХРАНЕНИЕ ОСТАЛОСЬ.",
                30f, 220f, 720f, 52f, 16,
                new Color(.69f, .72f, .67f));

            restoreButton = MakeButton(frame, "Checkpoint",
                "ЗАГРУЗИТЬ ПОСЛЕДНЕЕ СОХРАНЕНИЕ",
                30f, 300f, 707f, 65f,
                new Color(.17f, .27f, .19f),
                () =>
                {
                    if (gameplay.RestoreCheckpointAfterDeath())
                        HideDeath();
                    else
                        status.text =
                            "СОХРАНЕНИЕ НЕ НАЙДЕНО ИЛИ ПОВРЕЖДЕНО";
                });

            MakeButton(frame, "Respawn", "ВОЗРОДИТЬСЯ В ЛАГЕРЕ",
                30f, 375f, 707f, 65f,
                new Color(.13f, .17f, .17f),
                () =>
                {
                    gameplay.RespawnAtCamp();
                    HideDeath();
                });

            status = Label(frame, "WarningText",
                "СОХРАНЕНИЯ НЕ УДАЛЯЮТСЯ ПРИ СМЕРТИ.",
                30f, 455f, 707f, 62f, 13,
                new Color(.79f, .71f, .58f));
            root.SetActive(false);
        }

        void Update()
        {
            if (player == null || gameplay == null ||
                !player.Ready || !gameplay.HasLoadedInitialSave)
                return;

            // On startup, let the player choose another save profile
            // before showing the death report for a dead legacy checkpoint.
            if (!visible && player.Health <= 0f &&
                (frontEnd == null || !frontEnd.IsOpen))
                ShowDeath();
        }

        void ShowDeath()
        {
            if (visible)
                return;

            visible = true;
            restoreTimeScale = Time.timeScale > .001f
                ? Time.timeScale : 1f;

            // Close the journal before transferring input ownership.
            // Otherwise it would remain visible behind the death screen
            // and reclaim pointer focus after the player respawns.
            if (gameplay.Journal != null && gameplay.Journal.Visible)
                gameplay.SetJournalOpen(false);
            gameplay.CloseInventoryPanels();
            if (gameplay.Building != null)
                gameplay.Building.ExitBuildMode();
            gameplay.SetExternalUiBlocking(true);
            Time.timeScale = 0f;
            root.SetActive(true);
            restoreButton.interactable = gameplay.HasLiveCheckpoint;
            status.text = gameplay.HasLiveCheckpoint
                ? "ДОСТУПНО ПОСЛЕДНЕЕ СОХРАНЕНИЕ"
                : "НЕТ СОХРАНЕНИЯ — МОЖНО ВОЗРОДИТЬСЯ";
        }

        void HideDeath()
        {
            if (!visible)
                return;

            visible = false;
            root.SetActive(false);
            Time.timeScale = restoreTimeScale;
            gameplay.SetExternalUiBlocking(false);
        }

        static RectTransform Rect(Transform parent, string label,
            Vector2 min, Vector2 max, Vector2 pivot,
            Vector2 point, Vector2 size)
        {
            GameObject o = new GameObject(label, typeof(RectTransform));
            RectTransform rt = o.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = pivot;
            rt.anchoredPosition = point;
            rt.sizeDelta = size;
            return rt;
        }

        static Image AddImage(Transform parent, string label,
            float x, float y, float width, float height, Color tint)
        {
            RectTransform rect = Rect(parent, label,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(x, -y),
                new Vector2(width, height));
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = tint;
            image.raycastTarget = false;
            return image;
        }

        Text Label(Transform parent, string label, string content,
            float x, float y, float width, float height,
            int fontSize, Color color)
        {
            RectTransform rect = Rect(parent, label,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(x, -y),
                new Vector2(width, height));
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.text = content;
            text.color = color;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleLeft;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            return text;
        }

        Button MakeButton(Transform parent, string name, string caption,
            float x, float y, float width, float height, Color tint,
            UnityEngine.Events.UnityAction callback)
        {
            RectTransform rect = Rect(parent, name,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(x, -y),
                new Vector2(width, height));
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = tint;
            image.raycastTarget = true;

            Button b = rect.gameObject.AddComponent<Button>();
            b.targetGraphic = image;
            Label(rect, "ButtonLabel", caption,
                18f, 4f, width - 36f, height - 8f,
                16, Color.white);
            b.onClick.AddListener(callback);
            return b;
        }

        void OnDestroy()
        {
            if (visible)
            {
                Time.timeScale = restoreTimeScale;
                if (gameplay != null)
                    gameplay.SetExternalUiBlocking(false);
            }

            if (font != null)
                Destroy(font);
        }
    }
}
