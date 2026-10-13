using UnityEngine;
using UnityEngine.UI;

namespace DDay
{
    // A presentation layer for the existing Epilogue/TrueEnding phases, not another campaign.
    [RequireComponent(typeof(DDayGame))]
    public sealed class HEEndingPlayer : MonoBehaviour
    {
        [SerializeField] HEEndingConfig configuration;
        [SerializeField] HEEndingConfig badConfiguration;
        DDayGame game;
        Canvas canvas;
        RawImage bottom, top, light;
        RawImage[] dust;
        Text subtitle, endingTitle, openingTitle;
        CanvasGroup subtitleGroup, titleGroup, openingGroup;
        Button returnButton;
        AudioSource ambience;
        Texture2D glow, subtitleGradient;
        Font font;
        double origin;
        float elapsed;
        int audioShot = -1;
        bool started, preview, bad;
        float lastAdvance, captionStart, captionEnd;
        string captionText = "";
        bool revealCaption;
        int captionKey = -1;

        public HEEndingConfig Configuration => bad ? badConfiguration : configuration;
        public HEEndingConfig HEConfiguration => configuration;
        public HEEndingConfig BEConfiguration => badConfiguration;
        public float Elapsed => elapsed;
        public int ShotIndex { get; private set; } = -1;
        public float ImageScale { get; private set; } = 1;
        public string Subtitle => subtitle == null ? "" : subtitle.text;
        public float SubtitleAlpha => subtitleGroup == null ? 0 : subtitleGroup.alpha;
        public bool Completed => started && Configuration != null && elapsed >= Configuration.Duration;
        public bool OwnsUI => preview || game != null && game.Loop == 2 && game.Day == 7
            && (game.CurrentPhase == Phase.BadEnding || game.YesCount == 2
                && (game.CurrentPhase == Phase.Epilogue || game.CurrentPhase == Phase.TrueEnding));
        public bool AudioConfigured => DDayAudioManager.Instance != null
            && DDayAudioManager.Instance.Configuration != null;
        public bool IsOpening => started && elapsed < Configuration.OpeningDuration;
        public string FullSubtitle => captionText;
        public float CaptionStart => captionStart;
        public float CaptionEnd => captionEnd;
        public RectTransform CanvasRect => canvas == null ? null : (RectTransform)canvas.transform;

        void Awake()
        {
            game = GetComponent<DDayGame>();
            if (configuration == null) configuration = Resources.Load<HEEndingConfig>("DDay/Endings/HE/HEEndingConfig");
            if (badConfiguration == null) badConfiguration = Resources.Load<HEEndingConfig>("DDay/Endings/BE/BEEndingConfig");
        }

        void Update()
        {
            if (!OwnsUI)
            {
                if (started) { started = false; audioShot = -1; ambience.Stop(); }
                if (canvas != null) canvas.gameObject.SetActive(false);
                return;
            }
            if (!preview) bad = game.CurrentPhase == Phase.BadEnding;
            if (!ValidConfiguration()) return;
            if (!started)
            {
                bool complete = bad ? game.BECompleted : game.CurrentPhase == Phase.TrueEnding;
                float saved = game.EndingPresentationId == Configuration.presentationId
                    ? (bad ? game.BEElapsed : game.HEElapsed) : 0;
                StartAt(complete ? Configuration.Duration : saved);
            }
            canvas.gameObject.SetActive(true);
            elapsed = Mathf.Max(0, (float)(Time.unscaledTimeAsDouble - origin));
            Render();
            if (Time.unscaledTime > lastAdvance && (Input.GetMouseButtonDown(0)
                || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))) AdvanceSubtitle();
            if (preview) return;
            game.RememberEndingTime(bad, Mathf.Min(elapsed, Configuration.Duration), Configuration.presentationId);
            if (elapsed >= Configuration.Duration) game.CompleteIllustratedEnding(bad);
        }

        bool ValidConfiguration()
        {
            if (Configuration != null && Configuration.shots != null && Configuration.shots.Length == 3) return true;
            Debug.LogError("D-DAY: three-shot ending configuration is missing. Use D-DAY/Endings/Configure Formal Narration.");
            enabled = false;
            return false;
        }

        void StartAt(float seconds)
        {
            if (canvas == null) BuildCanvas();
            elapsed = Mathf.Clamp(seconds, 0, Configuration.Duration);
            origin = Time.unscaledTimeAsDouble - elapsed;
            started = true; audioShot = -1; captionKey = -1; ShotIndex = -1;
            lastAdvance = Time.unscaledTime + .35f;
            openingTitle.text = Configuration.openingTitle;
        }

        [ContextMenu("Preview HE in Play Mode")]
        public void PlayPreview()
        {
            StartPreview(false);
        }

        [ContextMenu("Preview BE in Play Mode")]
        public void PlayBEPreview() => StartPreview(true);
        void StartPreview(bool previewBad)
        {
            if (!Application.isPlaying) return;
            bad = previewBad;
            if (!ValidConfiguration()) return;
            preview = true; StartAt(0); canvas.gameObject.SetActive(true);
        }

        // Editor verification can inspect a precise frame without changing any campaign save.
        public void PreviewAt(float seconds)
        {
            if (!Application.isEditor || !Application.isPlaying) return;
            if (!preview) PlayPreview();
            origin = Time.unscaledTimeAsDouble - Mathf.Clamp(seconds, 0, Configuration.Duration);
            captionKey = -1;
        }

        // One click reveals the current text; the next advances one subtitle, never a whole ending.
        public void AdvanceSubtitle()
        {
            if (!started || IsOpening || Completed || captionKey < 0 || Time.unscaledTime < lastAdvance) return;
            DDayAudioManager.Play(SfxCue.Dialogue);
            lastAdvance = Time.unscaledTime + .22f;
            if (subtitle.text.Length < captionText.Length) { revealCaption = true; return; }
            origin = Time.unscaledTimeAsDouble - Mathf.Min(captionEnd + .02f, Configuration.Duration);
        }

        public void ReturnToMenu()
        {
            if (!Completed) return;
            bool wasPreview = preview;
            preview = false; started = false; audioShot = -1;
            DDayAudioManager.Play(SfxCue.Back);
            ambience.Stop(); canvas.gameObject.SetActive(false);
            if (!wasPreview) game.ReturnToTitle();
        }

        void Render()
        {
            var config = Configuration;
            bool ended = elapsed >= config.Duration;
            bool opening = elapsed < config.OpeningDuration;
            openingGroup.gameObject.SetActive(opening);
            openingGroup.alpha = opening ? Ease(elapsed / config.openingFadeIn)
                * Ease((config.OpeningDuration - elapsed) / config.openingFadeOut) : 0;
            titleGroup.gameObject.SetActive(ended);
            titleGroup.alpha = ended ? Ease((elapsed - config.Duration) / config.titleFade) : 0;
            returnButton.interactable = ended;
            if (ended || opening)
            {
                bottom.color = top.color = light.color = Color.clear;
                subtitleGroup.alpha = 0;
                foreach (var particle in dust) particle.color = Color.clear;
                ambience.volume = 0;
                captionKey = -1; captionText = "";
                return;
            }
            int index = 0;
            while (index < 2 && elapsed >= config.StartOf(index + 1)) index++;
            ShotIndex = index;
            HEEndingShot shot = config.shots[index];
            float local = elapsed - config.StartOf(index);
            float duration = config.ShotDuration(index);
            float alpha = index == 0 ? Ease(local / Mathf.Max(.01f, shot.fadeIn)) : 1;
            bool last = index == 2;
            if (last) alpha *= Ease((duration - local) / Mathf.Max(.01f, shot.fadeOut));

            if (index > 0 && local < shot.fadeIn)
            {
                var previous = config.shots[index - 1];
                SetIllustration(bottom, previous, config.ShotDuration(index - 1), 1);
                float blend = Ease((local + previous.fadeOut) / Mathf.Max(.01f, previous.fadeOut + shot.fadeIn));
                SetIllustration(top, shot, local, blend);
            }
            else if (!last && local > duration - shot.fadeOut)
            {
                var next = config.shots[index + 1];
                SetIllustration(bottom, shot, local, alpha);
                float blend = Ease((local - duration + shot.fadeOut) / Mathf.Max(.01f, shot.fadeOut + next.fadeIn));
                SetIllustration(top, next, 0, blend);
            }
            else
            {
                SetIllustration(bottom, shot, local, alpha);
                top.color = Color.clear;
            }
            RenderSubtitles(index, local, alpha);
            RenderAtmosphere(shot, local, alpha);
            UpdateAudio(index, shot, alpha);
        }

        void RenderSubtitles(int index, float local, float alpha)
        {
            var pages = Configuration.Pages(index);
            for (int i = 0; i < pages.Length; i++)
            {
                var page = pages[i];
                if (local < page.start || local >= page.start + page.duration) continue;
                int key = index * 1000 + i;
                if (captionKey != key) { captionKey = key; revealCaption = false; }
                captionText = page.text;
                captionStart = Configuration.StartOf(index) + page.start;
                captionEnd = captionStart + page.duration;
                int letters = revealCaption ? page.text.Length : Mathf.Clamp((int)((local - page.start) * 55), 0, page.text.Length);
                subtitle.text = page.text.Substring(0, letters);
                subtitleGroup.alpha = alpha * Ease((local - page.start) / .25f) * Ease((page.start + page.duration - local) / .25f);
                return;
            }
            subtitleGroup.alpha = 0; captionKey = -1;
        }

        static float Ease(float value) => Mathf.SmoothStep(0, 1, Mathf.Clamp01(value));

        void SetIllustration(RawImage image, HEEndingShot shot, float local, float alpha)
        {
            image.texture = shot.image;
            image.color = new Color(1, 1, 1, alpha);
            if (shot.image == null) return; // Slot remains editable; no unrelated art substituted.
            Vector2 viewport = CanvasRect.rect.size;
            int shotIndex = System.Array.IndexOf(Configuration.shots, shot);
            float duration = Configuration.ShotDuration(shotIndex);
            float t = Ease((local - shot.fadeIn) / Mathf.Max(.1f, duration - shot.fadeIn - shot.fadeOut));
            float scale = Mathf.Lerp(shot.startScale, shot.endScale, t);
            float fit = Mathf.Max(viewport.x / shot.image.width, viewport.y / shot.image.height);
            Vector2 size = new Vector2(shot.image.width, shot.image.height) * fit * scale;
            image.rectTransform.sizeDelta = size;
            Vector2 pan = Vector2.Scale(Vector2.Lerp(shot.startOffset, shot.endOffset, t), viewport);
            // Clamp to the available overscan: no moving image can expose black edges.
            Vector2 margin = (size - viewport) * .5f;
            image.rectTransform.anchoredPosition = new Vector2(
                Mathf.Clamp(pan.x, -Mathf.Max(0, margin.x), Mathf.Max(0, margin.x)),
                Mathf.Clamp(pan.y, -Mathf.Max(0, margin.y), Mathf.Max(0, margin.y)));
            if (shotIndex == ShotIndex) ImageScale = scale;
        }

        void RenderAtmosphere(HEEndingShot shot, float local, float alpha)
        {
            Vector2 viewport = CanvasRect.rect.size;
            light.rectTransform.anchoredPosition = Vector2.Scale(shot.lightPosition - Vector2.one * .5f, viewport);
            float pulse = .82f + .18f * Mathf.Sin(local * .9f);
            Color color = new Color(1, .76f, .40f, shot.lightIntensity * pulse * alpha);
            if (shot.atmosphere == HEAtmosphere.PoliceLights)
            {
                float signal = Mathf.Sin(local * 3.8f);
                color = signal >= 0 ? new Color(1, .18f, .12f) : new Color(.14f, .40f, 1);
                color.a = Mathf.Abs(signal) * shot.lightIntensity * alpha;
            }
            else if (shot.atmosphere == HEAtmosphere.CorridorPhone)
                color = new Color(.60f, .73f, 1, shot.lightIntensity * (.75f + .25f * Mathf.Sin(local * 1.8f)) * alpha);
            else if (shot.atmosphere == HEAtmosphere.RainNight)
                color = new Color(.45f, .62f, .88f, shot.lightIntensity * pulse * alpha);
            light.color = color;
            for (int i = 0; i < dust.Length; i++)
            {
                float seed = (i * .6180339f) % 1;
                float x = Mathf.Repeat(seed + local * .0018f, 1) - .5f;
                float y = Mathf.Repeat((i * .379f) % 1 + local * .003f, 1) - .5f;
                dust[i].rectTransform.anchoredPosition = Vector2.Scale(new Vector2(x, y), viewport);
                float flicker = .5f + .5f * Mathf.Sin(local * .4f + i);
                dust[i].color = new Color(1, .9f, .65f, shot.dustIntensity * flicker * alpha);
            }
        }

        void UpdateAudio(int index, HEEndingShot shot, float alpha)
        {
            if (audioShot != index)
            {
                if (audioShot >= 0 && !preview) game.SaveGame(); // Checkpoint only, not per-frame disk writes.
                audioShot = index;
                // Ending BGM is owned by the campaign audio service across all three shots.
                ambience.Stop(); ambience.clip = shot.ambience;
                if (shot.ambience != null) ambience.Play();
            }
            ambience.volume = shot.ambienceVolume * alpha * DDaySettings.SfxPercent / 100f;
        }

        void BuildCanvas()
        {
            var root = new GameObject("HE Ending Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            var backdrop = NewRect("Input blocking backdrop", root.transform);
            Stretch(backdrop); var dark = backdrop.gameObject.AddComponent<Image>(); dark.color = Color.black;
            dark.raycastTarget = true;
            bottom = Picture("Illustration", root.transform);
            top = Picture("Crossfade illustration", root.transform);
            glow = new Texture2D(64, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[4096];
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                float distance = new Vector2((x - 31.5f) / 31.5f, (y - 31.5f) / 31.5f).magnitude;
                pixels[y * 64 + x] = new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1 - distance), 2));
            }
            glow.SetPixels(pixels); glow.Apply(false, true);
            light = Picture("Atmosphere light", root.transform); light.texture = glow;
            light.rectTransform.sizeDelta = new Vector2(680, 540);
            dust = new RawImage[20];
            for (int i = 0; i < dust.Length; i++)
            {
                dust[i] = Picture("Air mote " + i, root.transform); dust[i].texture = glow;
                dust[i].rectTransform.sizeDelta = Vector2.one * (6 + i % 4 * 2);
            }
            font = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 34);
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform caption = NewRect("Subtitle", root.transform);
            caption.anchorMin = caption.anchorMax = new Vector2(.5f, 0);
            caption.anchoredPosition = new Vector2(0, 107); caption.sizeDelta = new Vector2(1920, 200);
            subtitleGradient = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var gradientPixels = new Color[64];
            for (int i = 0; i < 64; i++) gradientPixels[i] = new Color(.02f, .035f, .045f, .84f * (1 - i / 63f));
            subtitleGradient.SetPixels(gradientPixels); subtitleGradient.Apply(false, true);
            var captionBack = caption.gameObject.AddComponent<RawImage>(); captionBack.texture = subtitleGradient; captionBack.raycastTarget = false;
            subtitleGroup = caption.gameObject.AddComponent<CanvasGroup>(); subtitleGroup.blocksRaycasts = false;
            subtitle = Label("Subtitle text", caption, "", 34); subtitle.color = new Color(1, .97f, .90f);
            subtitle.GetComponent<RectTransform>().offsetMin = new Vector2(165, 20);
            subtitle.GetComponent<RectTransform>().offsetMax = new Vector2(-165, -30);
            subtitle.lineSpacing = 1.25f;
            var shadow = subtitle.gameObject.AddComponent<Shadow>(); shadow.effectDistance = new Vector2(2, -2);
            RectTransform titles = NewRect("Ending title", root.transform); Stretch(titles);
            titleGroup = titles.gameObject.AddComponent<CanvasGroup>();
            endingTitle = Label("D-DAY", titles, "D-DAY", 94);
            PositionLabel(endingTitle, new Vector2(0, 88), new Vector2(1100, 130));
            RectTransform buttonRect = NewRect("Return to main menu", titles);
            buttonRect.anchoredPosition = new Vector2(0, -210); buttonRect.sizeDelta = new Vector2(400, 68);
            Image buttonBack = buttonRect.gameObject.AddComponent<Image>();
            buttonBack.color = new Color(.17f, .28f, .29f, 1);
            returnButton = buttonRect.gameObject.AddComponent<Button>();
            returnButton.onClick.AddListener(ReturnToMenu);
            Label("Return label", buttonRect, "返回主菜单", 29);
            titleGroup.gameObject.SetActive(false);
            var opening = NewRect("Opening title", root.transform); Stretch(opening);
            openingGroup = opening.gameObject.AddComponent<CanvasGroup>(); openingGroup.blocksRaycasts = false;
            openingTitle = Label("Scripture title", opening, "", 52);
            PositionLabel(openingTitle, Vector2.zero, new Vector2(1600, 150));
            ambience = gameObject.AddComponent<AudioSource>();
            ambience.playOnAwake = false; ambience.loop = true; ambience.spatialBlend = 0;
        }

        static RectTransform NewRect(string name, Transform parent)
        {
            var obj = new GameObject(name, typeof(RectTransform)); obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f); return rect;
        }
        static void Stretch(RectTransform rect)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        static RawImage Picture(string name, Transform parent)
        { var image = NewRect(name, parent).gameObject.AddComponent<RawImage>(); image.raycastTarget = false; return image; }
        Text Label(string name, Transform parent, string content, int size)
        {
            var text = NewRect(name, parent).gameObject.AddComponent<Text>(); Stretch(text.rectTransform);
            text.font = font; text.text = content; text.fontSize = size; text.color = new Color(.98f, .95f, .87f);
            text.alignment = TextAnchor.MiddleCenter; text.resizeTextForBestFit = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false; return text;
        }
        static void PositionLabel(Text label, Vector2 position, Vector2 size)
        {
            var rect = label.rectTransform; rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position; rect.sizeDelta = size;
        }
        void OnApplicationPause(bool paused) { if (paused && OwnsUI && !preview) game.SaveGame(); }
        void OnApplicationQuit() { if (OwnsUI && !preview) game.SaveGame(); }
        void OnDestroy() { if (glow != null) Destroy(glow); if (subtitleGradient != null) Destroy(subtitleGradient); }
    }
}
