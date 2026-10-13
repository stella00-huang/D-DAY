using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DDay
{
    public sealed partial class DDayUI
    {
        public Rect TutorialTaskRect { get; private set; }
        public Rect TutorialRulesRect { get; private set; }
        bool tutorialReviewOpen;
        static readonly Rect TutorialSkipRect = new Rect(1710, 18, 186, 50);
        readonly DialogueFrame tutorialFrame = new DialogueFrame();
        readonly Rect employeeStrip = new Rect(318, 135, 1132, 211);

        void DrawTutorialOverlay()
        {
            var guide = GetComponent<DDayTutorialController>();
            if (guide == null || !guide.Active) return;
            if (guide.Visible)
            {
                var holes = TutorialTargets(guide);
                if (!guide.FreeSeating) DrawTutorialSpotlight(holes);
                foreach (Rect target in holes) TutorialOutline(target, gold);
                if (guide.IsEmployeeStep) DrawTutorialEmployees(guide);
                if (TutorialButton(TutorialSkipRect, guide.Configuration.skip, true)) { guide.Skip(); return; }
                if (guide.SpeechVisible) DrawTutorialGuide(guide);
            }
            else
            {
                // Formal dialogue owns its UI; the separate skip control never advances it.
                if (TutorialButton(TutorialSkipRect, guide.Configuration.skip, true)) guide.Skip();
            }
            if (!string.IsNullOrEmpty(guide.Discovery) && !game.BoardOpen && !game.TasksOpen
                && !game.SaveSlotsOpen && !OfficeSettingsOpen)
            {
                var style = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter };
                float textHeight = style.CalcHeight(new GUIContent(guide.Discovery), 740) + 2;
                Rect popup = new Rect(570, 112, 780, textHeight + 24);
                PixelFrame(popup.x, popup.y, popup.width, popup.height, warmDark);
                DrawText(new Rect(590, 123, 740, textHeight), guide.Discovery, style);
            }
        }

        List<Rect> TutorialTargets(DDayTutorialController guide)
        {
            var holes = new List<Rect>();
            string target = guide.Step.targetUiId;
            if (target == "today_task") holes.Add(TutorialTaskRect);
            if (target == "employees")
            { holes.Add(employeeStrip); holes.Add(TutorialRulesRect); }
            if (target == "seats_rules")
            { holes.Add(new Rect(280, 285, 1195, 439)); holes.Add(TutorialRulesRect); }
            if (target == "rules") holes.Add(TutorialRulesRect);
            if (target == "demo_seat")
            {
                Vector2 animal = WorldUi(game.AnimalWorldPoint(guide.DemoAnimal));
                holes.Add(new Rect(animal.x - 70, animal.y - 78, 140, 200));
                for (int seat = 0; seat < 8; seat++)
                    if (guide.DemoSeatAllowed(seat))
                    {
                        Vector2 point = WorldUi(game.TutorialSeatWorldPoint(seat));
                        holes.Add(new Rect(point.x - 81, point.y - 135, 162, 238));
                    }
            }
            if (target == "assignment") holes.Add(game.TasksOpen
                ? new Rect(305, 154, 1310, 770) : TutorialTaskRect);
            if (target == "book") holes.Add(game.BoardOpen
                ? new Rect(260, 108, 1400, 855) : TutorialToolbarRect("记录册"));
            if (target == "start_work") holes.Add(TutorialToolbarRect("开始工作"));
            return holes.Where(r => r.width > 0 && r.height > 0).ToList();
        }

        Rect TutorialToolbarRect(string name)
        {
            Transform buttonTransform = transform.Find("D-DAY Canvas/Bottom controls/" + name);
            if (buttonTransform == null) return Rect.zero;
            var corners = new Vector3[4];
            buttonTransform.GetComponent<RectTransform>().GetWorldCorners(corners);
            Vector2 top = DDayUiSpace.FromScreen(corners[1]), bottom = DDayUiSpace.FromScreen(corners[3]);
            return Rect.MinMaxRect(top.x - 5, top.y - 5, bottom.x + 5, bottom.y + 5);
        }

        void DrawTutorialSpotlight(List<Rect> holes)
        {
            // Split the screen around actual target edges, so intersecting holes stay undimmed.
            var xs = new List<float> { 0, 1920 }; var ys = new List<float> { 0, 1080 };
            foreach (Rect r in holes)
            { xs.Add(Mathf.Clamp(r.xMin, 0, 1920)); xs.Add(Mathf.Clamp(r.xMax, 0, 1920));
                ys.Add(Mathf.Clamp(r.yMin, 0, 1080)); ys.Add(Mathf.Clamp(r.yMax, 0, 1080)); }
            xs = xs.Distinct().OrderBy(x => x).ToList(); ys = ys.Distinct().OrderBy(y => y).ToList();
            for (int x = 0; x + 1 < xs.Count; x++) for (int y = 0; y + 1 < ys.Count; y++)
            {
                var cell = Rect.MinMaxRect(xs[x], ys[y], xs[x + 1], ys[y + 1]);
                if (!holes.Any(r => r.Contains(cell.center)))
                    Fill(cell.x, cell.y, cell.width, cell.height, new Color(0, 0, 0, .40f));
            }
        }

        void TutorialOutline(Rect r, Color color)
        {
            Fill(r.x, r.y, r.width, 2, color); Fill(r.x, r.yMax - 2, r.width, 2, color);
            Fill(r.x, r.y, 2, r.height, color); Fill(r.xMax - 2, r.y, 2, r.height, color);
        }
        bool TutorialButton(Rect r, string text, bool enabled)
        {
            PixelFrame(r.x, r.y, r.width, r.height, enabled ? new Color(.31f, .38f, .30f) : warmDark);
            DrawText(new Rect(r.x + 8, r.y + 4, r.width - 16, r.height - 8), text,
                new GUIStyle(button) { fontSize = 23, normal = { textColor = enabled ? warmText : Color.gray } });
            return enabled && GUI.Button(r, "", GUIStyle.none);
        }
        void DrawTutorialEmployees(DDayTutorialController guide)
        {
            PixelFrame(employeeStrip.x, employeeStrip.y, employeeStrip.width, employeeStrip.height, warmDark);
            for (int i = 0; i < 7; i++)
            {
                float x = 336 + i * 160;
                var face = new Rect(x + 31, 154, 95, 109);
                if (DDayArt.EmployeeSheet != null) GUI.DrawTextureWithTexCoords(face,
                    DDayArt.EmployeeSheet, DDayArt.TaskPortraitUv(i, false), true);
                DrawText(new Rect(x, 278, 152, 57), SeatRules.DisplayName(SeatRules.Ids[i]),
                    new GUIStyle(tiny) { fontSize = 22, alignment = TextAnchor.MiddleCenter });
                if ((guide.State.viewedEmployees & (1 << i)) == 0)
                    Fill(x + 134, 150, 9, 9, new Color(.91f, .27f, .20f));
                if (GUI.Button(new Rect(x, 148, 152, 186), "", GUIStyle.none))
                { DDayAudioManager.Play(SfxCue.Click); game.SelectAnimal(i); SetPublicProfile(i); game.BlockDialogueInputForTutorial(); }
            }
        }
        static string TutorialSpeech(DDayTutorialController guide) => guide.ShowIdleHint
            ? string.Format(guide.Configuration.idleHint, guide.GetComponent<DDayGame>().FirstUnmetSeatRule)
            : guide.CurrentLine;
        void DrawTutorialGuide(DDayTutorialController guide)
        {
            tutorialFrame.speakerId = tutorialFrame.focusId = guide.Step.speaker;
            tutorialFrame.text = TutorialSpeech(guide);
            tutorialFrame.expressionId = "neutral"; tutorialFrame.portraitPosition = "left";
            DrawDialogueFrame(tutorialFrame, "tutorial:" + guide.Step.stepId + ":" + guide.State.line
                + ":" + guide.ShowIdleHint, guide.StageNumber + " / " + guide.StageCount, false);
        }

        void DrawTutorialReviewButton()
        {
            var cfg = GetComponent<DDayTutorialController>()?.Configuration;
            if (!OfficeSettingsOpen || cfg == null) { tutorialReviewOpen = false; return; }
            if (Tap(cfg.reviewTitle, 565, 724, 250, 60, new Color(.29f, .38f, .39f))) tutorialReviewOpen = true;
            if (!tutorialReviewOpen) return;
            DrawTutorialReviewPanel();
        }
        void DrawTutorialReviewPanel()
        {
            var cfg = GetComponent<DDayTutorialController>().Configuration;
            Fill(0, 0, 1920, 1080, new Color(0, 0, 0, .5f));
            PixelFrame(515, 290, 890, 490, warmDark);
            DrawText(new Rect(565, 326, 790, 62), cfg.reviewTitle, heading);
            DrawText(new Rect(565, 407, 790, 252), cfg.reviewText, small);
            if (TutorialButton(new Rect(825, 691, 270, 60), cfg.next, true))
            { DDayAudioManager.Play(SfxCue.Back); tutorialReviewOpen = false; }
        }
    }
}
