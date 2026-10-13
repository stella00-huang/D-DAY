using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DDay
{
    [Serializable] public sealed class PortraitExpression
    {
        public string id, resource;
        public float x, y, width, height;
    }
    [Serializable] public sealed class CharacterPortrait
    {
        public string speakerId;
        public PortraitExpression[] expressions;
    }
    [Serializable] public sealed class DialogueDirection
    {
        public string eventId, speakerId, focusId, expressionId, portraitPosition, effect;
        public int lineIndex;
        public bool switchSpeaker = true;
        public bool narration;
    }
    [Serializable] public sealed class DialoguePresentation
    {
        public CharacterPortrait[] characters;
        public DialogueDirection[] directions;
        static DialoguePresentation cached;
        public static string[] SplitTurns(string text) => System.Text.RegularExpressions.Regex.Split(
            text ?? "", @"\r?\n|\s+(?=(?:SISY|ORIN|NARCIS|ECHO|METIS|CASSIA|THANE|老板)[：:])")
            .Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
        public static readonly string[] ExpressionIds = { "neutral", "happy", "sad", "angry",
            "surprised", "nervous", "serious" };
        public static DialoguePresentation Current
        {
            get
            {
                if (cached != null) return cached;
                TextAsset source = Resources.Load<TextAsset>("DDay/DialoguePresentation");
                if (source == null) throw new InvalidOperationException("D-DAY: DialoguePresentation.json missing");
                cached = JsonUtility.FromJson<DialoguePresentation>(source.text);
                if (cached?.characters?.Length != 7 || cached.directions == null)
                    throw new InvalidOperationException("D-DAY: invalid portrait configuration");
                return cached;
            }
        }

        public bool Portrait(string id, string expression, out Texture2D texture, out Rect uv,
            out bool fallback)
        {
            CharacterPortrait character = characters.FirstOrDefault(x => x.speakerId == id);
            PortraitExpression art = character?.expressions.FirstOrDefault(x => x.id == expression);
            fallback = art == null && !string.IsNullOrEmpty(id);
            art = art ?? character?.expressions.FirstOrDefault(x => x.id == "neutral");
            texture = art == null ? null : Resources.Load<Texture2D>(art.resource);
            uv = texture == null ? Rect.zero : new Rect(art.x / texture.width,
                (texture.height - art.y - art.height) / texture.height,
                art.width / texture.width, art.height / texture.height);
            return texture != null;
        }

        static int PageLength(string text)
        {
            float used=0; int lines=1, length=0;
            // A 28px, 1010px-wide textbox fits 36 full-width glyphs on four lines.
            // Leave margin for font fallback and the expanded public animal names.
            while(length<text.Length)
            {
                string shown=SeatRules.PublicText(text.Substring(0,length+1));
                used=0;lines=1;
                foreach(char glyph in shown)
                {
                    if(glyph=='\n') {lines++;used=0;}
                    else {used+=glyph<256?.6f:1f;if(used>34) {lines++;used=glyph<256?.6f:1f;}}
                }
                if(lines>4) break;
                length++;
            }
            return Math.Max(1,length);
        }

        // Only explicit character prefixes identify speakers. Emotion is never inferred from words.
        public DialogueFrame[] Frames(string eventId, string[] lines, string defaultFocus, int offset)
        {
            var result = new List<DialogueFrame>();
            for (int i = 0; i < lines.Length; i++)
            {
                string text = lines[i], speaker = "";
                foreach (string id in SeatRules.Ids)
                {
                    string name = SeatRules.Names[SeatRules.Index(id)];
                    string code = SeatRules.CharacterCode(SeatRules.Index(id));
                    string[] prefixes = { code + "：", code + ":", name + "：", name + ":",
                        SeatRules.DisplayName(id) + "：" };
                    string prefix = prefixes.FirstOrDefault(text.StartsWith);
                    if (prefix == null && text.StartsWith(code + "“")) prefix = code;
                    if (prefix == null) continue;
                    speaker = id; text = text.Substring(prefix.Length).Trim(); break;
                }
                DialogueDirection direction = directions.FirstOrDefault(x => x.eventId == eventId
                    && x.lineIndex == i + offset);
                if (direction?.narration == true) { speaker = ""; text = lines[i]; }
                if (!string.IsNullOrEmpty(direction?.speakerId)) speaker = direction.speakerId;
                string focus = !string.IsNullOrEmpty(speaker) ? speaker
                    : !string.IsNullOrEmpty(direction?.focusId) ? direction.focusId : defaultFocus;
                // Long authored paragraphs are paginated without changing their words.
                int page = 0;
                do
                {
                    int length = PageLength(text);
                    if (text.Length > length)
                    {
                        int punctuation = text.LastIndexOfAny(new[] { '。', '！', '？', '；', '，' }, length - 1, length);
                        if (punctuation >= 65) length = punctuation + 1;
                    }
                    result.Add(new DialogueFrame {
                        eventId = eventId, sourceLineIndex = i + offset,
                        dialogueId = eventId + ":" + (i + offset) + ":" + page++, speakerId = speaker,
                        focusId = focus, text = text.Substring(0, length),
                        expressionId = direction?.expressionId ?? "neutral",
                        portraitPosition = direction?.portraitPosition ?? "left",
                        switchSpeaker = direction?.switchSpeaker ?? true, effect = direction?.effect ?? "fade"
                    });
                    text = text.Substring(length);
                } while (text.Length > 0);
            }
            string[] speakers = result.Where(x => !string.IsNullOrEmpty(x.speakerId))
                .Select(x => x.speakerId).Distinct().ToArray();
            if (speakers.Length == 2)
                foreach (DialogueFrame frame in result)
                {
                    frame.partnerId = speakers.FirstOrDefault(x => x != frame.focusId);
                    frame.portraitPosition = frame.focusId == speakers[1] ? "right" : "left";
                }
            for (int i = 1; i < result.Count; i++)
                if (!result[i].switchSpeaker)
                {
                    result[i].focusId = result[i - 1].focusId;
                    result[i].portraitPosition = result[i - 1].portraitPosition;
                }
            return result.ToArray();
        }
    }
    public sealed class DialogueFrame
    {
        public string eventId;
        public int sourceLineIndex;
        public string dialogueId, speakerId, focusId, partnerId, text, expressionId, portraitPosition, effect;
        public bool switchSpeaker;
    }

    public sealed partial class DDayGame
    {
        DialogueFrame[] dialogueFrames = Array.Empty<DialogueFrame>();
        public DialogueFrame CurrentDialogue => narrativeIndex < dialogueFrames.Length
            ? dialogueFrames[narrativeIndex] : null;
        public int DialogueSerial { get; private set; }
        public int DialogueInputBlockedUntilFrame { get; private set; }
        public ComicPagePresentation CurrentPagePresentation => CurrentPhase == Phase.Narrative && CurrentDialogue != null
            ? ComicPagePresentation.ForEvent(CurrentDialogue.eventId) : null;
        public ComicPagePanel CurrentPagePanel => CurrentPagePresentation?.panels[CurrentDialogue.sourceLineIndex];
        public ComicShot CurrentComicShot => CurrentPhase == Phase.Narrative && CurrentDialogue != null
            ? (CurrentPagePanel != null ? CurrentPagePresentation.Shot(CurrentDialogue.sourceLineIndex)
                : ComicPresentation.Current.Find(CurrentDialogue.eventId, CurrentDialogue.sourceLineIndex)) : null;

        public float PageComicFade { get; private set; }
        bool pageComicEnding;

        System.Collections.IEnumerator FinishPageComic()
        {
            pageComicEnding = true;
            for (float time = 0; time < .85f; time += Time.unscaledDeltaTime)
            { PageComicFade = Mathf.SmoothStep(0, 1, time / .85f); yield return null; }
            PageComicFade = 1;
            CompleteNarrative();
        }

        void PrepareDialogue(string eventId, string[] lines, string focus, int offset)
        {
            PageComicFade = 0; pageComicEnding = false;
            var page = ComicPagePresentation.ForEvent(eventId);
            dialogueFrames = page != null
                ? page.Frames()
                : DialoguePresentation.Current.Frames(eventId, lines, focus, offset);
            narrativeLines = dialogueFrames.Select(x => x.text).ToArray();
            DialogueSerial++;
            // Opening mouse release and the closing click cannot reach the office or the next scene.
            DialogueInputBlockedUntilFrame = Time.frameCount + 1;
            DragAnimal = -1; HoverSeat = -1;
        }
    }
}
