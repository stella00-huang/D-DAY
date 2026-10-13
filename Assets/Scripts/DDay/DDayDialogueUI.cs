using UnityEngine;

namespace DDay
{
    // One shared renderer for A2, B1 and B2. The office sprites are never altered.
    public sealed partial class DDayUI
    {
        public static readonly Rect PortraitDialogueRect = new Rect(414, 742, 1092, 298);
        string portraitFrameKey;
        float portraitChangedAt;
        int expressionSerial = -1;
        readonly System.Collections.Generic.Dictionary<string, string> lastExpressions =
            new System.Collections.Generic.Dictionary<string, string>();

        void DrawPortraitDialogue()
        {
            DialogueFrame frame = game.CurrentDialogue;
            if (frame == null) return;
            if (game.CurrentComicShot != null) { DrawComicDialogue(); return; }
            if (expressionSerial != game.DialogueSerial)
            { expressionSerial = game.DialogueSerial; lastExpressions.Clear(); }
            if (!string.IsNullOrEmpty(frame.focusId)) lastExpressions[frame.focusId] = frame.expressionId;
            string key = game.DialogueSerial + ":" + frame.dialogueId;
            if (portraitFrameKey != key)
            { portraitFrameKey = key; portraitChangedAt = Time.unscaledTime; }
            float alpha = frame.effect == "fade"
                ? Mathf.Clamp01((Time.unscaledTime - portraitChangedAt) / .12f) : 1f;
            DrawDialogueFrame(frame, key, (game.NarrativeIndex + 1) + " / " + game.NarrativeCount, true, alpha);
        }

        // Tutorial and official dialogue share the same portrait, name, text and input renderer.
        void DrawDialogueFrame(DialogueFrame frame, string key, string progress, bool dim, float alpha = 1f)
        {
            if (dim) Fill(0, 0, 1920, 1080, new Color(.04f, .06f, .08f, .40f));
            bool right = frame.portraitPosition == "right";
            if (!string.IsNullOrEmpty(frame.partnerId))
                DrawDialoguePortrait(frame.partnerId, lastExpressions.TryGetValue(frame.partnerId,
                    out string previousExpression) ? previousExpression : "neutral", !right, false, 1f);
            DrawDialoguePortrait(frame.focusId, frame.expressionId, right, true, alpha);
            PixelFrame(PortraitDialogueRect.x, PortraitDialogueRect.y, PortraitDialogueRect.width,
                PortraitDialogueRect.height, new Color(.10f, .15f, .16f, .98f));
            Fill(437, 760, 5, 44, gold);
            DrawText(new Rect(452, 764, 705, 45), string.IsNullOrEmpty(frame.speakerId)
                ? "旁白" : SeatRules.DisplayName(frame.speakerId),
                new GUIStyle(heading) { fontSize = 31 });
            DrawText(new Rect(1193, 764, 275, 45),
                progress,
                new GUIStyle(tiny) { alignment = TextAnchor.MiddleRight });
            string line = SeatRules.PublicText(frame.text);
            string visible = VisibleText("portrait:" + key, line);
            DrawText(new Rect(452, 821, 1016, 155), visible,
                new GUIStyle(body) { fontSize = 28, wordWrap = true });
            DrawText(new Rect(1137, 984, 331, 36),
                TextRevealed(line) ? "点击 / 空格  ›" : "点击显示全文",
                new GUIStyle(tiny) { fontSize = 21, alignment = TextAnchor.MiddleRight });
            Event e = Event.current;
            bool skipPointer = GetComponent<DDayTutorialController>()?.Active == true
                && TutorialSkipRect.Contains(e.mousePosition);
            bool advance = e.type == EventType.MouseDown && e.button == 0 && !skipPointer
                || e.type == EventType.KeyDown && e.keyCode == KeyCode.Space;
            if (advance)
            {
                e.Use();
                AdvanceDialogueInput();
            }
        }

        public bool AdvanceDialogueInput()
        {
            var guide = GetComponent<DDayTutorialController>();
            if (guide != null && guide.SpeechVisible)
            {
                if (Time.frameCount <= game.DialogueInputBlockedUntilFrame) return false;
                if (TextRevealed(SeatRules.PublicText(TutorialSpeech(guide)))) guide.Advance();
                else { revealAll = true; game.BlockDialogueInputForTutorial(); }
                return true;
            }
            if (game.CurrentPhase != Phase.Narrative || game.CurrentDialogue == null
                || Time.frameCount <= game.DialogueInputBlockedUntilFrame) return false;
            string line = SeatRules.PublicText(game.CurrentDialogue.text);
            if (game.CurrentPagePanel != null) { game.AdvanceNarrative(); return true; }
            if (TextRevealed(line)) game.AdvanceNarrative();
            else revealAll = true;
            return true;
        }

        void DrawDialoguePortrait(string id, string expression, bool right, bool active, float alpha)
        {
            DrawPortraitAt(id, expression, right, active, alpha,
                new Rect(right ? 1535 : 35, 542, 350, 488));
        }

        void DrawPortraitAt(string id, string expression, bool right, bool active, float alpha, Rect bounds)
        {
            if (string.IsNullOrEmpty(id) || !DialoguePresentation.Current.Portrait(id,
                expression, out Texture2D texture, out Rect uv, out _)) return;
            // Upper-body slices keep ears, shoulders, shirt and tie. The plain badge
            // has no writing; mirroring makes this side-facing art look inward naturally.
            float height = bounds.height, width = height * uv.width * texture.width / (uv.height * texture.height);
            Rect rect = new Rect(bounds.center.x - width * .5f, bounds.y, width, height);
            if (right) uv = new Rect(uv.x + uv.width, uv.y, -uv.width, uv.height);
            GUI.color = active ? new Color(1, 1, 1, alpha) : new Color(.42f, .46f, .48f, .86f);
            GUI.DrawTextureWithTexCoords(rect, texture, uv, true);
            GUI.color = Color.white;
        }
    }
}
