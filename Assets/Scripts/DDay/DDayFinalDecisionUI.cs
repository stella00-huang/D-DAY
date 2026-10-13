using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DDay
{
    // Presentation of the existing two final votes. Only the second YES moves.
    [RequireComponent(typeof(DDayGame))]
    public sealed class DDayFinalDecisionUI : MonoBehaviour
    {
        [SerializeField] float reactionInterval = 1.4f;
        [SerializeField] float mouseRadius = 230;
        [SerializeField] float normalSpeed = 470;
        [SerializeField] float easySpeed = 135;
        DDayGame game;
        Canvas canvas;
        RectTransform area, question;
        CanvasGroup choices, questionGroup;
        Image shade;
        Button yes, no;
        Coroutine motion;
        int displayedQuestion = -1;
        bool submitted;
        float movingSince, nextReaction;
        Vector2 lastViewport;

        public bool OwnsUI => game != null && game.Loop == 2 && game.Day == 7
            && game.CurrentPhase == Phase.FinalDecision && !(game.GetComponent<HEEndingPlayer>()?.OwnsUI ?? false);
        public Button YesButton => yes;
        public Button NoButton => no;
        public RectTransform CanvasRect => area;
        public RectTransform QuestionRect => question;
        public bool IsPaused { get; private set; }
        public bool IsTransitioning => choices != null && !choices.interactable;
        public float MovingTime => displayedQuestion == 1 ? Mathf.Max(0, Time.unscaledTime - movingSince) : 0;
        public float CurrentSpeed => Mathf.Lerp(normalSpeed, easySpeed, Mathf.SmoothStep(0, 1,
            Mathf.InverseLerp(10, 15, MovingTime)));
        public int EscapeCount { get; private set; }
        public int MoveCount { get; private set; }

        void Awake() => game = GetComponent<DDayGame>();
        void Update()
        {
            if (!OwnsUI) { if (displayedQuestion >= 0) Leave(); return; }
            if (canvas == null) Build();
            if (displayedQuestion != game.FinalQuestion) Enter(game.FinalQuestion, displayedQuestion == 0);
            if (area.rect.width > 400 && area.rect.size != lastViewport)
            {
                lastViewport = area.rect.size;
                yes.GetComponent<RectTransform>().anchoredPosition = Clamp(yes.GetComponent<RectTransform>().anchoredPosition);
            }
            if (!choices.interactable || submitted) return;
            // Explicit navigation stays attached to the same Buttons as YES travels.
            var selected = EventSystem.current == null ? null : EventSystem.current.currentSelectedGameObject;
            if (Input.GetKeyDown(KeyCode.Tab))
                (selected == yes.gameObject ? no : yes).Select();
            if (Input.GetKeyDown(KeyCode.Space) && (selected == yes.gameObject || selected == no.gameObject))
                Choose(selected == yes.gameObject);
        }

        void Enter(int step, bool transition)
        {
            StopMotion();
            canvas.gameObject.SetActive(true);
            displayedQuestion = step; submitted = false;
            EscapeCount = MoveCount = 0; IsPaused = true;
            yes.GetComponent<RectTransform>().anchoredPosition = new Vector2(-270, -120);
            no.GetComponent<RectTransform>().anchoredPosition = new Vector2(270, -120);
            choices.alpha = questionGroup.alpha = 1;
            choices.interactable = choices.blocksRaycasts = true;
            shade.color = new Color(.03f, .08f, .11f, step == 0 ? .86f : .92f);
            yes.Select();
            movingSince = Time.unscaledTime;
            if (step == 1) motion = StartCoroutine(SecondQuestion(transition));
        }

        IEnumerator SecondQuestion(bool transition)
        {
            if (transition)
            {
                choices.interactable = choices.blocksRaycasts = false;
                float start = Time.unscaledTime;
                while (Time.unscaledTime - start < .65f)
                {
                    float t = Time.unscaledTime - start;
                    choices.alpha = questionGroup.alpha = t < .2f ? 1 - t / .2f
                        : Mathf.Clamp01((t - .4f) / .25f);
                    shade.color = new Color(.03f, .08f, .11f, t < .4f ? .97f : .92f);
                    yield return null;
                }
                choices.alpha = questionGroup.alpha = 1;
                choices.interactable = choices.blocksRaycasts = true;
                submitted = false; yes.Select();
            }
            movingSince = Time.unscaledTime;
            nextReaction = movingSince + reactionInterval;
            yield return new WaitForSecondsRealtime(.5f);
            var rect = yes.GetComponent<RectTransform>();
            while (OwnsUI && displayedQuestion == 1)
            {
                IsPaused = false;
                Vector2 from = rect.anchoredPosition, target = Destination(from, false);
                float began = Time.unscaledTime;
                float duration = Mathf.Max(.65f, Vector2.Distance(from, target) / CurrentSpeed);
                MoveCount++;
                while (Time.unscaledTime - began < duration)
                {
                    rect.anchoredPosition = Vector2.Lerp(from, target,
                        Mathf.SmoothStep(0, 1, (Time.unscaledTime - began) / duration));
                    if (Time.unscaledTime >= nextReaction && MouseNear(rect.anchoredPosition))
                    {
                        from = rect.anchoredPosition;
                        target = Destination(from, true);
                        began = Time.unscaledTime;
                        duration = Mathf.Max(.75f, Vector2.Distance(from, target) / CurrentSpeed);
                        nextReaction = began + Mathf.Lerp(reactionInterval, 2.8f, Mathf.InverseLerp(10, 15, MovingTime));
                        EscapeCount++;
                    }
                    yield return null;
                }
                rect.anchoredPosition = target;
                IsPaused = true;
                // Deliberate catch windows: the mouse cannot cancel these rests.
                yield return new WaitForSecondsRealtime(Mathf.Lerp(Random.Range(.65f, 1.05f), 2.1f,
                    Mathf.InverseLerp(10, 15, MovingTime)));
            }
        }

        bool MouseNear(Vector2 position)
        {
            if (!RectTransformUtility.RectangleContainsScreenPoint(area, Input.mousePosition, null)) return false;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(area, Input.mousePosition, null, out Vector2 mouse);
            return Vector2.Distance(position, mouse) < mouseRadius;
        }

        Vector2 Destination(Vector2 from, bool flee)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(area, Input.mousePosition, null, out Vector2 mouse);
            Rect limits = MovementBounds();
            Vector2 best = from; float bestScore = -1;
            for (int i = 0; i < 48; i++)
            {
                Vector2 candidate = new Vector2(Random.Range(limits.xMin, limits.xMax), Random.Range(limits.yMin, limits.yMax));
                if (!SafePath(from, candidate) || Vector2.Distance(from, candidate) < 160) continue;
                float score = flee ? Vector2.Distance(candidate, mouse) : Random.Range(0f, 1000f);
                if (score <= bestScore) continue;
                best = candidate; bestScore = score;
            }
            return best;
        }

        Rect MovementBounds()
        {
            Vector2 half = yes.GetComponent<RectTransform>().sizeDelta * .5f;
            return Rect.MinMaxRect(area.rect.xMin + half.x + 48, area.rect.yMin + half.y + 48,
                area.rect.xMax - half.x - 48, question.anchoredPosition.y - question.rect.height * .5f - half.y - 36);
        }
        Vector2 Clamp(Vector2 point)
        { Rect r = MovementBounds(); return new Vector2(Mathf.Clamp(point.x, r.xMin, r.xMax), Mathf.Clamp(point.y, r.yMin, r.yMax)); }

        bool SafePath(Vector2 from, Vector2 target)
        {
            // Inflate NO by YES's half-size; reject paths, not just destinations.
            var fixedRect = no.GetComponent<RectTransform>();
            Vector2 half = (fixedRect.sizeDelta + yes.GetComponent<RectTransform>().sizeDelta) * .5f + Vector2.one * 24;
            Rect obstacle = new Rect(fixedRect.anchoredPosition - half, half * 2);
            for (int i = 0; i <= 48; i++) if (obstacle.Contains(Vector2.Lerp(from, target, i / 48f))) return false;
            return true;
        }

        void Choose(bool vote)
        {
            if (!OwnsUI || submitted || !choices.interactable || displayedQuestion != game.FinalQuestion) return;
            submitted = true; choices.interactable = choices.blocksRaycasts = false;
            DDayAudioManager.Play(SfxCue.Confirmation);
            game.ChooseFinal(vote);
            if (OwnsUI) Enter(game.FinalQuestion, true); else Leave();
        }

        void StopMotion() { if (motion != null) StopCoroutine(motion); motion = null; }
        void Leave()
        {
            StopMotion(); displayedQuestion = -1; IsPaused = true; submitted = false;
            if (EventSystem.current != null && (EventSystem.current.currentSelectedGameObject == yes?.gameObject
                || EventSystem.current.currentSelectedGameObject == no?.gameObject))
                EventSystem.current.SetSelectedGameObject(null);
            if (canvas != null) canvas.gameObject.SetActive(false);
        }
        void OnDisable() => Leave();

        void Build()
        {
            var root = new GameObject("Final decision Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 900;
            area = root.GetComponent<RectTransform>();
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            shade = NewRect("Background shade", area, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            Stretch(shade.rectTransform); shade.raycastTarget = true;
            Font font = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 44);
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            question = NewRect("APIS question", area, new Vector2(0, 265), new Vector2(1180, 100));
            Label(question, "是否维持 APIS 运转？", font, 44);
            questionGroup = question.gameObject.AddComponent<CanvasGroup>(); questionGroup.blocksRaycasts = false;
            var layer = NewRect("Independent moving choices", area, Vector2.zero, Vector2.zero); Stretch(layer);
            choices = layer.gameObject.AddComponent<CanvasGroup>();
            yes = MakeButton("是", layer, font, new Color(.20f, .47f, .42f), true);
            no = MakeButton("否", layer, font, new Color(.39f, .34f, .34f), false);
            var nav = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = no, selectOnRight = no, selectOnUp = no, selectOnDown = no };
            yes.navigation = nav;
            nav.selectOnLeft = nav.selectOnRight = nav.selectOnUp = nav.selectOnDown = yes; no.navigation = nav;
            if (EventSystem.current == null) new GameObject("D-DAY EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            canvas.gameObject.SetActive(false);
        }

        Button MakeButton(string name, RectTransform parent, Font font, Color color, bool vote)
        {
            RectTransform rect = NewRect(name, parent, Vector2.zero, new Vector2(360, 88));
            Image background = rect.gameObject.AddComponent<Image>(); background.color = color;
            Button button = rect.gameObject.AddComponent<Button>();
            var colors = button.colors; colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f);
            colors.selectedColor = new Color(1.22f, 1.22f, 1.22f); button.colors = colors;
            button.onClick.AddListener(() => Choose(vote));
            Label(rect, name, font, 36);
            var edge = NewRect("Accent edge", rect, new Vector2(0, -41), new Vector2(360, 5)).gameObject.AddComponent<Image>();
            edge.color = new Color(.99f, .77f, .40f); edge.raycastTarget = false;
            return button;
        }
        static RectTransform NewRect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var obj = new GameObject(name, typeof(RectTransform)); obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
            rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }
        static void Stretch(RectTransform rect)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        static void Label(RectTransform parent, string content, Font font, int size)
        {
            var rect = NewRect("Text", parent, Vector2.zero, Vector2.zero); Stretch(rect);
            var text = rect.gameObject.AddComponent<Text>(); text.font = font; text.text = content; text.fontSize = size;
            text.color = new Color(.97f, .95f, .87f); text.alignment = TextAnchor.MiddleCenter;
            text.resizeTextForBestFit = false; text.raycastTarget = false;
        }
    }
}

