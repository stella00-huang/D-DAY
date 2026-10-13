using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DDay
{
    [Serializable] public sealed class ComicFocusRect
    {
        public float x, y, width, height;
        public Rect Rect => new Rect(x, y, width, height);
    }
    [Serializable] public sealed class ComicPagePanel
    {
        public string id, narration, effect, ambience, sound;
        // Source pixels, top-left origin. Polygon follows the real sloping ink gutters.
        public Vector2[] outline;
        public ComicFocusRect focus;
        public ComicFocusRect accent;
        public bool focusOnly;
        // Optional original B2 line indices: several captions can share one drawn panel.
        public int[] sourceLines;
    }

    [Serializable] public sealed class ComicPagePresentation
    {
        public string eventId, presentationId, resource;
        public int width, height;
        public ComicPagePanel[] panels;
        public int[] legacyLinePanels;
        static ComicPagePresentation cached;
        static ComicPagePresentation day2;
        static ComicPagePresentation day4, day6;
        DialogueFrame[] frames;
        Dictionary<string, int> authoredFrameIndices;
        public static ComicPagePresentation Current => cached ?? (cached = JsonUtility.FromJson<ComicPagePresentation>(
            Resources.Load<TextAsset>("DDay/Story/Day1/Day1ComicPresentation").text));
        public static ComicPagePresentation ForEvent(string eventId)
        {
            if (eventId == "B2_L1_D1") return Current;
            if (eventId == "B2_L1_D2") return day2 ?? (day2 = Load(2));
            if (eventId == "B2_L1_D4") return day4 ?? (day4 = Load(4));
            if (eventId == "B2_L1_D6") return day6 ?? (day6 = Load(6));
            return null;
        }
        public string ContentPresentationId => presentationId +
            (ABConfig.Current.fixedEvents.First(e => e.id == eventId).comicTexts?.Length == panels.Length
                ? ":Excel20261012" : "");
        public int ResumeIndex(string savedPresentation, int savedIndex)
        {
            Frames();
            if (savedPresentation == ContentPresentationId) return Mathf.Clamp(savedIndex, 0, frames.Length - 1);
            if (savedPresentation == presentationId)
                return Mathf.Max(0, Array.FindIndex(frames, f => f.sourceLineIndex ==
                    Mathf.Clamp(savedIndex, 0, panels.Length - 1)));
            return Mathf.Max(0, LegacyPanel(savedIndex));
        }
        static ComicPagePresentation Load(int day) => JsonUtility.FromJson<ComicPagePresentation>(
            Resources.Load<TextAsset>("DDay/Story/Day" + day + "/Day" + day + "ComicPresentation").text);
        public DialogueFrame[] Frames()
        {
            if (frames != null) return frames;
            authoredFrameIndices = new Dictionary<string, int>();
            var result = new List<DialogueFrame>();
            ABComicText[] currentText = ABConfig.Current.FixedFor(
                eventId.StartsWith("B2_L1_") ? 1 : 2,
                int.Parse(eventId.Substring(eventId.LastIndexOf('D') + 1)))?.comicTexts;
            DialogueFrame[] original = panels.Any(p => p.sourceLines?.Length > 0)
                ? DialoguePresentation.Current.Frames(eventId,
                    ABConfig.Current.fixedEvents.First(e => e.id == eventId).lines, "", 0) : null;
            for (int i = 0; i < panels.Length; i++)
            {
                ComicPagePanel panel = panels[i];
                if (currentText != null && currentText.Length == panels.Length)
                {
                    string[] lines = currentText[i].lines ?? Array.Empty<string>();
                    if (lines.Length == 0) lines = new[] { "" };
                    int part = 0;
                    foreach (string line in lines)
                        foreach (string text in CaptionParts(line))
                            result.Add(new DialogueFrame { eventId = eventId, sourceLineIndex = i,
                                dialogueId = presentationId + ":" + panel.id + ":content:" + part++,
                                text = text, expressionId = "neutral", effect = panel.effect });
                    continue;
                }
                if (panel.sourceLines == null || panel.sourceLines.Length == 0)
                    result.Add(new DialogueFrame { eventId = eventId, sourceLineIndex = i,
                        dialogueId = presentationId + ":" + panel.id, text = panel.narration ?? "",
                        expressionId = "neutral", effect = panel.effect });
                else foreach (DialogueFrame source in original.Where(f => panel.sourceLines.Contains(f.sourceLineIndex)))
                {
                    authoredFrameIndices[source.dialogueId] = result.Count;
                    int part = 0;
                    foreach (string text in CaptionParts(source.text))
                        result.Add(new DialogueFrame { eventId = eventId, sourceLineIndex = i,
                            dialogueId = presentationId + ":" + panel.id + ":" + source.dialogueId + ":" + part++,
                            text = text, speakerId = source.speakerId, focusId = source.focusId,
                            expressionId = source.expressionId, effect = panel.effect });
                }
            }
            return frames = result.ToArray();
        }
        // The original words are only paginated to fit the shared two-line caption area.
        static IEnumerable<string> CaptionParts(string text)
        {
            do
            {
                int length = 0;
                while (length < text.Length)
                {
                    string shown = SeatRules.PublicText(text.Substring(0, length + 1));
                    if (shown.Sum(c => c < 256 ? .6f : 1f) > 72) break;
                    length++;
                }
                length = Math.Max(1, length);
                if (length < text.Length)
                {
                    int punctuation = text.LastIndexOfAny(new[] {'。','！','？','；','，'}, length - 1, length);
                    if (punctuation >= 20) length = punctuation + 1;
                }
                yield return text.Substring(0, Math.Min(length, text.Length));
                text = text.Substring(Math.Min(length, text.Length));
            } while (text.Length > 0);
        }
        public ComicShot Shot(int index) => new ComicShot { eventId = eventId, shotId = panels[index].id,
            firstLine = index, lastLine = index, kind = "page", background = resource, effect = panels[index].effect };
        // Legacy prose save indices are migrated by source line, never applied as new panel indices.
        public int LegacyPanel(int index)
        {
            var source = ABConfig.Current.fixedEvents.First(x => x.id == eventId);
            var frames = DialoguePresentation.Current.Frames(eventId, source.lines, "", 0);
            var oldFrame = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
            int line = oldFrame.sourceLineIndex;
            Frames();
            if (authoredFrameIndices.TryGetValue(oldFrame.dialogueId, out int exact)) return exact;
            if (legacyLinePanels != null && legacyLinePanels.Length > 0)
                return Array.FindIndex(this.frames, f => f.sourceLineIndex ==
                    legacyLinePanels[Mathf.Clamp(line, 0, legacyLinePanels.Length - 1)]);
            return line == 0 ? 0 : line <= 4 ? 1 : line == 5 ? 2 : 4;
        }
    }

    // Presentation only: one unmodified page, ink-aware dimming masks and quiet placeholder audio.
    // Progress/input/save remain owned by the existing DDayGame narrative state machine.
    public sealed class DDayComicPage : MonoBehaviour
    {
        DDayGame game;
        AudioSource ambience, effects;
        readonly Dictionary<string, AudioClip> sounds = new Dictionary<string, AudioClip>();
        readonly Dictionary<string, Texture2D> masks = new Dictionary<string, Texture2D>();
        readonly List<string> soundHistory = new List<string>();
        Texture2D glow;
        string key, maskPageId;
        int previousIndex = -1, index = -1;
        float started;
        bool cuePlayed;
        public IReadOnlyList<string> SoundHistory => soundHistory;
        public float Elapsed => Time.unscaledTime - started;
        public int PanelIndex => index;
        public bool AudioActive => ambience.isPlaying || effects.isPlaying;
        public static readonly Rect PageRect = new Rect(330, 24, 1260, 840);
        public static readonly Rect CaptionRect = new Rect(330, 886, 1260, 160);
        public Rect DisplayRect
        {
            get
            {
                var page = game.CurrentPagePresentation;
                float scale = Mathf.Min(PageRect.width / page.width, PageRect.height / page.height);
                float width = page.width * scale, height = page.height * scale;
                return new Rect(PageRect.center.x - width / 2, PageRect.center.y - height / 2, width, height);
            }
        }

        void Awake()
        {
            game = GetComponent<DDayGame>();
            ambience = gameObject.AddComponent<AudioSource>(); effects = gameObject.AddComponent<AudioSource>();
            foreach (var audio in new[] { ambience, effects })
            { audio.playOnAwake = false; audio.spatialBlend = 0; audio.ignoreListenerPause = true; }
            glow = new Texture2D(64, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[4096];
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                float d = new Vector2((x - 31.5f) / 31.5f, (y - 31.5f) / 31.5f).magnitude;
                pixels[y * 64 + x] = new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1 - d), 1.5f));
            }
            glow.SetPixels(pixels); glow.Apply();
        }

        void Update()
        {
            if (game.CurrentPagePanel == null)
            {
                if (index >= 0) { ambience.Stop(); effects.Stop(); index = previousIndex = -1; key = null; }
                return;
            }
            int panelIndex = game.CurrentDialogue.sourceLineIndex;
            string next = game.DialogueSerial + ":" + panelIndex;
            if (key != next)
            {
                string pageId = game.CurrentPagePresentation.presentationId;
                previousIndex = maskPageId == pageId ? index : -1;
                maskPageId = pageId; index = panelIndex; key = next; started = Time.unscaledTime;
                cuePlayed = false;
                ambience.Stop(); effects.Stop(); effects.loop = false;
                ComicPagePanel panel = game.CurrentPagePanel;
                if (!masks.ContainsKey(MaskKey(index))) masks[MaskKey(index)] = MakeMask(panel);
                if (!string.IsNullOrEmpty(panel.ambience))
                { ambience.clip = Sound(panel.ambience); ambience.loop = true; ambience.volume = .09f; ambience.Play();
                    soundHistory.Add(panel.id + ":" + panel.ambience); }
            }
            var current = game.CurrentPagePanel;
            if (!cuePlayed && Elapsed >= (current.effect == "lightning" ? .38f : .04f))
            {
                cuePlayed = true;
                if (!string.IsNullOrEmpty(current.sound))
                { effects.clip = Sound(current.sound); effects.loop = current.effect == "ambulance";
                    effects.volume = .10f; effects.Play(); soundHistory.Add(current.id + ":" + current.sound); }
            }
            float volume = current.effect == "impact" && Elapsed < .55f ? 0 :
                current.effect == "fatigue" || current.effect == "somber" ? .035f : .09f;
            ambience.volume = volume * (1 - game.PageComicFade) * DDaySettings.SfxPercent / 100f;
            effects.volume = .10f * (1 - game.PageComicFade) * DDaySettings.SfxPercent / 100f;
        }

        public bool IsHighlighted(int panel, Vector2 point)
        {
            ComicPagePanel data = game.CurrentPagePresentation.panels[panel];
            if (data.focusOnly && !data.focus.Rect.Contains(point)) return false;
            bool inside = false;
            for (int i = 0, j = data.outline.Length - 1; i < data.outline.Length; j = i++)
            {
                Vector2 a = data.outline[i], b = data.outline[j];
                if ((a.y > point.y) != (b.y > point.y)
                    && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
            }
            return inside;
        }

        Texture2D MakeMask(ComicPagePanel panel)
        {
            var page = game.CurrentPagePresentation;
            var texture = new Texture2D(page.width, page.height, TextureFormat.RGBA32, false) {
                name = "Comic focus mask " + panel.id, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[page.width * page.height];
            for (int y = 0; y < page.height; y++) for (int x = 0; x < page.width; x++)
                pixels[(page.height - 1 - y) * page.width + x] = new Color32(0, 0, 0,
                    (byte)(IsHighlighted(index, new Vector2(x + .5f, y + .5f)) ? 0 : 138));
            texture.SetPixels32(pixels); texture.Apply(false, true); return texture;
        }

        Rect OnPage(Rect source)
        {
            var page = game.CurrentPagePresentation; Rect stage = DisplayRect;
            return new Rect(stage.x + source.x * stage.width / page.width,
                stage.y + source.y * stage.height / page.height,
                source.width * stage.width / page.width, source.height * stage.height / page.height);
        }

        string MaskKey(int panel) => maskPageId + ":" + panel;

        public void Draw()
        {
            if (index < 0 || game.CurrentPagePanel == null) return;
            var page = game.CurrentPagePresentation;
            if (page.presentationId != maskPageId) return; // Wait for Update to prepare this page's own mask.
            var panel = page.panels[index];
            Texture2D image = Resources.Load<Texture2D>(page.resource);
            if (image == null) return;
            Rect stage = DisplayRect;
            if (panel.effect == "impact" && Elapsed < .26f)
                stage.position += new Vector2(Mathf.Sin(Elapsed * 94), Mathf.Cos(Elapsed * 83))
                    * (2.3f * (1 - Elapsed / .26f));
            GUI.color = panel.effect == "cool" ? new Color(.92f, .96f, 1, 1)
                : panel.effect == "somber" ? new Color(.90f, .92f, .95f, 1) : Color.white;
            GUI.DrawTexture(stage, image, ScaleMode.StretchToFill, false);
            float blend = Mathf.SmoothStep(0, 1, Mathf.Clamp01(Elapsed / .24f));
            if (previousIndex >= 0 && blend < 1)
            { GUI.color = new Color(1, 1, 1, 1 - blend); GUI.DrawTexture(stage, masks[MaskKey(previousIndex)]); }
            GUI.color = new Color(1, 1, 1, blend); GUI.DrawTexture(stage, masks[MaskKey(index)]);
            Rect focus = OnPage(panel.focus.Rect);
            // A gentle narrowing light focuses the eye without cropping, enlarging or moving any panel.
            float lightSize = Mathf.Lerp(1.16f, 1f, Mathf.Clamp01(Elapsed / (panel.effect == "phone" ? .6f : 2.8f)));
            Rect light = new Rect(focus.center.x - focus.width * lightSize / 2,
                focus.center.y - focus.height * lightSize / 2, focus.width * lightSize, focus.height * lightSize);
            GUI.color = new Color(1, .96f, .87f, .055f * blend); GUI.DrawTexture(light, glow);
            if (panel.accent != null)
            { GUI.color = new Color(1, .92f, .78f, .13f * blend); GUI.DrawTexture(OnPage(panel.accent.Rect), glow); }
            if (panel.effect == "lightning")
            {
                float flash = Mathf.Clamp01(1 - Mathf.Abs(Elapsed - .46f) / .12f);
                GUI.color = new Color(.81f, .90f, 1, flash * .42f); GUI.DrawTexture(focus, glow);
            }
            if (panel.effect == "ambulance")
            {
                float pulse = Mathf.Sin(Elapsed * 5.6f);
                GUI.color = pulse > 0 ? new Color(1, .15f, .12f, pulse * .15f)
                    : new Color(.13f, .42f, 1, -pulse * .15f);
                GUI.DrawTexture(focus, glow);
            }
            GUI.color = Color.white;
        }

        AudioClip Sound(string id)
        {
            if (sounds.TryGetValue(id, out AudioClip clip)) return clip;
            // Optional authored recordings can replace these quiet, explicitly temporary synthesis cues.
            clip = Resources.Load<AudioClip>("DDay/Story/Day1/Audio/" + id);
            if (clip == null)
            {
                const int rate = 22050;
                float duration = id == "impact" ? .48f : id == "dial" ? 1.2f : id == "paper" ? .7f : 4f;
                var samples = new float[Mathf.CeilToInt(rate * duration)];
                var random = new System.Random(37); float low = 0;
                for (int i = 0; i < samples.Length; i++)
                {
                    float t = i / (float)rate, noise = (float)random.NextDouble() * 2 - 1;
                    low = Mathf.Lerp(low, noise, id == "paper" ? .36f : id == "rain" ? .24f : .018f);
                    float value = low * 2.2f;
                    if (id == "impact") value = (Mathf.Sin(2 * Mathf.PI * 74 * t) * .55f + low) * Mathf.Exp(-t * 13);
                    else if (id == "dial") value = (Mathf.Sin(2 * Mathf.PI * 440 * t) + Mathf.Sin(2 * Mathf.PI * 350 * t))
                        * .20f * ((t % .30f) < .17f ? 1 : 0);
                    else if (id == "siren") value = Mathf.Sin(2 * Mathf.PI * 770 * t - 92 * Mathf.Cos(2 * Mathf.PI * .5f * t)) * .24f;
                    else if (id == "thunder") value *= Mathf.Exp(-t * .95f) * Mathf.Min(t * 8, 1);
                    else if (id == "crowd") value *= .7f + .25f * Mathf.Sin(t * 14);
                    else if (id == "paper") value *= .22f * Mathf.Pow(Mathf.Sin(t / duration * Mathf.PI), 2);
                    float envelope = Mathf.Min(1, t / .06f) * Mathf.Min(1, (duration - t) / .08f);
                    samples[i] = Mathf.Clamp(value * envelope, -.85f, .85f);
                }
                clip = AudioClip.Create("Temporary comic cue: " + id, samples.Length, 1, rate, false);
                clip.SetData(samples, 0);
            }
            sounds[id] = clip; return clip;
        }

        void OnDestroy()
        {
            foreach (var mask in masks.Values) Destroy(mask);
            foreach (var sound in sounds.Values) if (sound.name.StartsWith("Temporary comic cue:")) Destroy(sound);
            if (glow != null) Destroy(glow);
        }
    }
}
