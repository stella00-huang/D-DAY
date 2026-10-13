using UnityEngine;

namespace DDay
{
    // B2 uses the existing dialogue frames, typewriter, expression resolver and input handler.
    public sealed partial class DDayUI
    {
        string comicShotKey;
        float comicShotStarted;

        void DrawAfterHoursTransition()
        {
            Fill(0, 0, 1920, 1080, new Color(.025f, .035f, .05f, game.AfterHoursAlpha));
            GUI.color = new Color(1, 1, 1, game.AfterHoursAlpha);
            DrawText(new Rect(600, 472, 720, 120), game.AfterHoursCaption, title);
            GUI.color = Color.white;
        }

        void DrawComicDialogue()
        {
            if (game.CurrentPagePanel != null) { DrawPageComic(); return; }
            DialogueFrame frame = game.CurrentDialogue;
            ComicShot shot = game.CurrentComicShot;
            string key = game.DialogueSerial + ":" + shot.shotId;
            if (comicShotKey != key) { comicShotKey = key; comicShotStarted = Time.unscaledTime; }
            float elapsed = Time.unscaledTime - comicShotStarted;
            Fill(0, 0, 1920, 1080, new Color(.025f, .035f, .05f, .96f));
            Rect stage = new Rect(198, 128, 1524, 562);
            PixelFrame(180, 110, 1560, 598, warmDark);
            Texture2D background = Resources.Load<Texture2D>(shot.background);
            if (background != null)
            {
                float zoom = shot.effect == "push" ? 1f + .025f * Mathf.Clamp01(elapsed / 3f) : 1f;
                float uvHeight = (float)background.width / background.height / (stage.width / stage.height) / zoom;
                float uvWidth = 1f / zoom;
                float pan = shot.effect == "pan" ? .015f * Mathf.Clamp01(elapsed / 3f) : 0;
                Rect uv = new Rect((1f - uvWidth) * .5f, .32f + pan, uvWidth, uvHeight);
                GUI.color = new Color(shot.tint * .93f, shot.tint * .97f, shot.tint, 1);
                GUI.DrawTextureWithTexCoords(stage, background, uv, true);
                GUI.color = Color.white;
            }
            foreach (ComicActor actor in shot.actors ?? System.Array.Empty<ComicActor>())
            {
                string expression = actor.id == frame.focusId ? frame.expressionId : actor.expression;
                bool speaking = string.IsNullOrEmpty(frame.speakerId) || actor.id == frame.speakerId;
                float height = 474 * actor.scale;
                Rect bounds = new Rect(stage.x + stage.width * actor.x - 200,
                    stage.yMax - height, 400, height);
                Matrix4x4 previous = GUI.matrix;
                if (actor.tilt != 0) GUIUtility.RotateAroundPivot(actor.tilt, bounds.center);
                DrawPortraitAt(actor.id, expression, actor.facingLeft, speaking, 1f, bounds);
                GUI.matrix = previous;
            }
            if (shot.itemId == "orin_photo") DrawComicPhoto(stage, shot.kind == "photo");
            else if (shot.itemId == "letter")
            {
                Sprite folder = Resources.Load<Sprite>("ToiletBreakV02/Pixel/Folder");
                if (folder != null) GUI.DrawTexture(new Rect(790, 280, 340, 280), folder.texture,
                    ScaleMode.ScaleToFit, true);
            }
            if (!string.IsNullOrEmpty(shot.timeCaption))
                DrawText(new Rect(230, 144, 600, 52), shot.timeCaption,
                    new GUIStyle(small) { fontSize = 25, normal = { textColor = warmText } });
            float fade = Mathf.Clamp01(1f - elapsed / .18f);
            if (fade > 0) Fill(stage.x, stage.y, stage.width, stage.height,
                new Color(.025f, .035f, .05f, fade));
            PixelFrame(180, 728, 1560, 294, new Color(.10f, .15f, .16f, .99f));
            bool named = !string.IsNullOrEmpty(frame.speakerId);
            if (named) DrawText(new Rect(220, 744, 1460, 48), SeatRules.DisplayName(frame.speakerId),
                new GUIStyle(heading) { fontSize = 31 });
            string line = SeatRules.PublicText(frame.text);
            DrawText(new Rect(220, named ? 805 : 753, 1460, named ? 150 : 202),
                VisibleText("comic:" + game.DialogueSerial + ":" + frame.dialogueId, line),
                new GUIStyle(body) { fontSize = 29, wordWrap = true });
            DrawText(new Rect(1380, 971, 320, 36), TextRevealed(line) ? "点击 / 空格  ›" : "点击显示全文",
                new GUIStyle(tiny) { fontSize = 21, alignment = TextAnchor.MiddleRight });
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0
                || e.type == EventType.KeyDown && e.keyCode == KeyCode.Space)
            { e.Use(); AdvanceDialogueInput(); }
        }

        void DrawComicPhoto(Rect stage, bool closeup)
        {
            // Temporary photo composition from ORIN's existing pixel portrait, no new identity/art.
            float width = closeup ? 340 : 210, height = closeup ? 410 : 255;
            float x = closeup ? stage.center.x - width * .5f : stage.center.x + 160;
            float y = stage.yMax - height - 24;
            PixelFrame(x, y, width, height, new Color(.87f, .82f, .66f));
            Fill(x + 16, y + 16, width - 32, height - 64, new Color(.25f, .31f, .31f));
            DrawPortraitAt("wolf", "neutral", false, true, 1f,
                new Rect(x + 28, y + 28, width - 56, height - 80));
        }

        void DrawPageComic()
        {
            Fill(0, 0, 1920, 1080, new Color(.025f, .035f, .05f));
            GetComponent<DDayComicPage>().Draw();
            Rect caption = DDayComicPage.CaptionRect;
            PixelFrame(caption.x, caption.y, caption.width, caption.height, new Color(.10f, .15f, .16f, .99f));
            DialogueFrame frame = game.CurrentDialogue;
            bool named = !string.IsNullOrEmpty(frame.speakerId);
            if (named) DrawText(new Rect(caption.x + 30, caption.y + 8, caption.width - 378, 44),
                SeatRules.DisplayName(frame.speakerId), new GUIStyle(heading) { fontSize = 27 });
            DrawText(new Rect(caption.x + 30, caption.y + (named ? 56 : 20), caption.width - 60, 86),
                game.CurrentPagePanel.sourceLines?.Length > 0 ? SeatRules.PublicText(frame.text) : frame.text,
                new GUIStyle(body) { fontSize = 29, wordWrap = true });
            DrawText(new Rect(caption.xMax - 318, named ? caption.y + 12 : caption.yMax - 48, 288, 36), "点击 / 空格  ›",
                new GUIStyle(tiny) { fontSize = 21, alignment = TextAnchor.MiddleRight });
            if (game.PageComicFade > 0) Fill(0, 0, 1920, 1080, new Color(.025f, .035f, .05f, game.PageComicFade));
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0
                || e.type == EventType.KeyDown && e.keyCode == KeyCode.Space)
            { e.Use(); AdvanceDialogueInput(); }
        }
    }
}
