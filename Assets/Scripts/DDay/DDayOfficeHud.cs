using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Linq;

namespace DDay
{
    // Screen-space controls and world-following nameplates share one scaled canvas.
    [RequireComponent(typeof(DDayGame))]
    public sealed class DDayOfficeHud : MonoBehaviour
    {
        DDayGame game;
        DDayUI ui;
        Canvas canvas;
        RectTransform canvasRect;
        GameObject toolbar;
        readonly RectTransform[] nameplates = new RectTransform[7];
        readonly Image[] nameBacks = new Image[7];
        readonly Button[] explorationBubbles = new Button[7];
        Button startButton;
        GameObject unreadBadge;
        Text startText;
        Font font;
        static readonly Color Dark = new Color(.18f, .15f, .13f, .96f);
        static readonly Color Accent = new Color(.66f, .51f, .31f, 1f);
        static readonly Color TextColor = new Color(.99f, .94f, .82f, 1f);

        void Awake()
        {
            game = GetComponent<DDayGame>();
            ui = GetComponent<DDayUI>();
            font = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 24);
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject root = new GameObject("D-DAY Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvasRect = root.GetComponent<RectTransform>();
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.matchWidthOrHeight = .5f;
            if (FindFirstObjectByType<EventSystem>() == null)
                new GameObject("D-DAY EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            toolbar = Panel("Bottom controls", new Vector2(960, 54), new Vector2(920, 78), Dark, true);
            HorizontalLayoutGroup layout = toolbar.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 9, 9);
            layout.spacing = 24;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            Button recordButton = Button("记录册", () => game.ToggleBoard(), 180);
            Text badge = Label(recordButton.transform, "!", new Vector2(159, 42), new Vector2(24, 26), 23);
            badge.gameObject.name = "Unread badge";
            badge.color = new Color(1f, .81f, .37f);
            badge.fontStyle = FontStyle.Bold;
            unreadBadge = badge.gameObject;
            unreadBadge.SetActive(false);
            Button("存档", () => game.ToggleSaveSlots(), 180);
            Button("设置", () => ui.OpenOfficeSettings(), 180);
            startButton = Button("开始工作", () => game.StartWork(), 240);
            startText = startButton.GetComponentInChildren<Text>();

            for (int i = 0; i < 7; i++)
            {
                GameObject tag = Panel("Name " + SeatRules.Ids[i], Vector2.zero,
                    new Vector2(160, 34), Dark, false);
                nameplates[i] = tag.GetComponent<RectTransform>();
                nameBacks[i] = tag.GetComponent<Image>();
                Label(tag.transform, SeatRules.DisplayName(SeatRules.Ids[i]),
                    new Vector2(80, 17), new Vector2(154, 31), 18);
                GameObject bubble = Panel("Explore " + SeatRules.Ids[i], Vector2.zero,
                    new Vector2(52, 38), Dark, false);
                bubble.GetComponent<Image>().raycastTarget = true;
                explorationBubbles[i] = bubble.AddComponent<Button>();
                int animal = i;
                explorationBubbles[i].onClick.AddListener(() => {
                    var tutorial = game.GetComponent<DDayTutorialController>();
                    if (tutorial != null && !tutorial.AllowsExploration) return;
                    DDayAudioManager.Play(SfxCue.Click);
                    game.OpenSeatExploration(animal);
                });
                var colors = explorationBubbles[i].colors;
                colors.normalColor = new Color(1f, .91f, .68f);
                colors.highlightedColor = Color.white;
                colors.pressedColor = new Color(.65f, .83f, .67f);
                explorationBubbles[i].colors = colors;
                Label(bubble.transform, "···", new Vector2(26, 19), new Vector2(48, 34), 25);
                Outline outline = bubble.AddComponent<Outline>();
                outline.effectColor = Accent;
                outline.effectDistance = new Vector2(2, -2);
                bubble.SetActive(false);
            }
        }

        void OnEnable()
        {
            // Unity can reload scripts while Play Mode keeps the runtime Canvas alive.
            // Restore the non-serialized lookup arrays instead of duplicating the HUD.
            if (Application.isPlaying) RebindAfterReload();
        }

        void RebindAfterReload()
        {
            Transform root = transform.Find("D-DAY Canvas");
            if (root == null) return;
            game = GetComponent<DDayGame>();
            ui = GetComponent<DDayUI>();
            canvas = root.GetComponent<Canvas>();
            canvasRect = root.GetComponent<RectTransform>();
            toolbar = root.Find("Bottom controls")?.gameObject;
            if (toolbar != null)
            {
                startButton = toolbar.transform.Find("开始工作")?.GetComponent<Button>();
                startText = startButton == null ? null : startButton.GetComponentInChildren<Text>();
                unreadBadge = toolbar.transform.Find("记录册/Unread badge")?.gameObject;
            }
            for (int i = 0; i < nameplates.Length; i++)
            {
                Transform plate = root.Find("Name " + SeatRules.Ids[i]);
                nameplates[i] = plate == null ? null : plate.GetComponent<RectTransform>();
                nameBacks[i] = plate == null ? null : plate.GetComponent<Image>();
                explorationBubbles[i] = root.Find("Explore " + SeatRules.Ids[i])?.GetComponent<Button>();
            }
        }

        GameObject Panel(string name, Vector2 position, Vector2 size, Color color, bool bottom)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(canvas.transform, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = bottom ? new Vector2(.5f, 0) : new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = bottom ? new Vector2(position.x - 960, position.y)
                : position - new Vector2(960, 540);
            rect.sizeDelta = size;
            Image image = obj.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = bottom;
            return obj;
        }

        Text Label(Transform parent, string value, Vector2 center, Vector2 size, int fontSize)
        {
            GameObject obj = new GameObject(value, typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = center - size * .5f;
            rect.sizeDelta = size;
            Text label = obj.GetComponent<Text>();
            label.font = font;
            label.fontSize = fontSize;
            label.color = TextColor;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            label.resizeTextForBestFit = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.text = value;
            return label;
        }

        Button Button(string value, UnityEngine.Events.UnityAction click, float width)
        {
            GameObject obj = new GameObject(value, typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(toolbar.transform, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, 60);
            LayoutElement element = obj.AddComponent<LayoutElement>();
            element.minWidth = element.preferredWidth = width;
            element.minHeight = element.preferredHeight = 60;
            var image = obj.GetComponent<Image>();
            image.color = Accent;
            var button = obj.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => {
                var tutorial = game.GetComponent<DDayTutorialController>();
                if (tutorial != null && !tutorial.AllowsToolbar(value)) return;
                DDayAudioManager.Play(SfxCue.Click);
                click();
            });
            Label(obj.transform, value, new Vector2(width * .5f, 30), new Vector2(width - 12, 54), 23);
            return button;
        }

        public bool IsPointerOverExplorationBubble(Vector2 screenPoint)
        {
            foreach (Button bubble in explorationBubbles)
                if (bubble != null && bubble.gameObject.activeInHierarchy
                    && RectTransformUtility.RectangleContainsScreenPoint(
                        bubble.GetComponent<RectTransform>(), screenPoint, null)) return true;
            return false;
        }

        void LateUpdate()
        {
            if (game == null || game.Data == null) return;
            if (toolbar == null || canvasRect == null || nameplates[0] == null
                || startButton == null || startText == null)
                RebindAfterReload();
            if (toolbar == null || canvasRect == null || nameplates[0] == null
                || startButton == null || startText == null) return;
            bool layout = game.CurrentPhase == Phase.Layout || game.CurrentPhase == Phase.SecondLoop;
            bool officeOpen = layout && !game.TasksOpen && !game.BoardOpen && !game.SaveSlotsOpen
                && (ui == null || !ui.OfficeSettingsOpen);
            var guide = game.GetComponent<DDayTutorialController>();
            var hosts = officeOpen && game.DragAnimal < 0 && (guide == null || guide.AllowsExploration)
                ? game.FindSeatExplorations().Select(game.ExplorationHost).Distinct().ToArray()
                : System.Array.Empty<int>();
            toolbar.SetActive(layout && !game.TasksOpen && !game.BoardOpen
                && !game.SaveSlotsOpen && (ui == null || !ui.OfficeSettingsOpen));
            if (layout)
            {
                var tutorial = game.GetComponent<DDayTutorialController>();
                startButton.interactable = tutorial == null || tutorial.AllowsToolbar("开始工作");
                startText.text = "开始工作";
                if (unreadBadge != null) unreadBadge.SetActive(game.HasUnreadRecords);
                toolbar.GetComponent<RectTransform>().anchoredPosition = new Vector2(0,
                    54 + DDayUiSpace.Offset.y / DDayUiSpace.Scale);
            }
            for (int i = 0; i < 7; i++)
            {
                Button bubble = explorationBubbles[i];
                if (bubble != null)
                {
                    bool available = hosts.Contains(i) && game.Seats[i] >= 0;
                    bubble.gameObject.SetActive(available);
                    if (available)
                    {
                        Vector3 head = Camera.main.WorldToScreenPoint(game.AnimalWorldPoint(i));
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,
                            head, null, out Vector2 bubblePoint);
                        bubble.GetComponent<RectTransform>().anchoredPosition = bubblePoint + new Vector2(0, 28);
                    }
                }
                if (nameplates[i] == null || nameBacks[i] == null) continue;
                bool show = layout && !game.TasksOpen && !game.BoardOpen
                    && !game.SaveSlotsOpen && game.Seats[i] >= 0
                    && (ui == null || !ui.OfficeSettingsOpen)
                    && game.HoveredAnimal == i && game.DragAnimal != i;
                nameplates[i].gameObject.SetActive(show);
                if (!show) continue;
                Vector3 point = Camera.main.WorldToScreenPoint(game.AnimalWorldPoint(i));
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, point, null,
                    out Vector2 local);
                nameplates[i].anchoredPosition = local + new Vector2(0, 25);
                nameBacks[i].color = new Color(.39f, .33f, .21f, .98f);
            }
        }
    }
}
