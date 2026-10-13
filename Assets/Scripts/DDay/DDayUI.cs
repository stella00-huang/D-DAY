using System.Linq;
using UnityEngine;

namespace DDay
{
    internal static class DDayUiSpace
    {
        public static float Scale => Mathf.Max(.001f, Mathf.Min(Screen.width / 1920f, Screen.height / 1080f));
        public static Vector2 Offset => new Vector2((Screen.width - 1920f * Scale) * .5f, (Screen.height - 1080f * Scale) * .5f);
        public static Matrix4x4 Matrix => Matrix4x4.TRS(new Vector3(Offset.x, Offset.y, 0), Quaternion.identity, new Vector3(Scale, Scale, 1));
        public static Vector2 FromScreen(Vector3 point) => (new Vector2(point.x, Screen.height - point.y) - Offset) / Scale;
    }

    public sealed partial class DDayUI : MonoBehaviour
    {
        DDayGame game;
        Texture2D white;
        GUIStyle heading, title, body, small, tiny, button, center, clue;
        bool day1InfoOpen;
        bool revealPublicProfile;
        int day1InfoAnimal = -1;
        public bool SeatDebugVisible { get; set; }
        int rightTab;
        int selectedTaskIndex;
        bool taskHistoryOpen;
        string selectedItem = "hire_form";
        Vector2 recordScroll;
        Vector2 recordDetailScroll;
        bool recordPageOpened;
        public readonly System.Collections.Generic.List<string> TextLayoutIssues =
            new System.Collections.Generic.List<string>();
        bool settingsOpen;
        string pendingExit;
        bool titleCreditsOpen;
        public bool TitleSettingsOpen => settingsOpen && game.CurrentPhase == Phase.Title;
        public bool TitleCreditsOpen => titleCreditsOpen && game.CurrentPhase == Phase.Title;
        public void OpenTitleSettings() { if (game.CurrentPhase == Phase.Title) { pendingExit = null; settingsOpen = true; } }
        public void OpenTitleCredits() { if (game.CurrentPhase == Phase.Title) titleCreditsOpen = true; }
        public bool OfficeSettingsOpen => settingsOpen
            && (game.CurrentPhase == Phase.Layout || game.CurrentPhase == Phase.SecondLoop);
        public void OpenOfficeSettings()
        {
            if (game.CurrentPhase != Phase.Layout && game.CurrentPhase != Phase.SecondLoop) return;
            if (game.TasksOpen || game.BoardOpen || game.SaveSlotsOpen) return;
            pendingExit = null;
            settingsOpen = true;
        }
        string selectedRecordId;
        int pendingOverwriteSlot;
        string revealKey;
        float revealStart;
        bool revealAll;
        public void SetRightTab(int tab) => rightTab = Mathf.Clamp(tab, 0, 1);
        readonly Color ink = new Color(.13f, .20f, .23f);
        readonly Color paper = new Color(.97f, .95f, .87f);
        readonly Color pale = new Color(.83f, .91f, .87f);
        readonly Color gold = new Color(.99f, .77f, .40f);

        void Awake()
        {
            game = GetComponent<DDayGame>();
            if (GetComponent<DDayComicPage>() == null) gameObject.AddComponent<DDayComicPage>();
            if (GetComponent<HEEndingPlayer>() == null) gameObject.AddComponent<HEEndingPlayer>();
            if (GetComponent<DDayFinalDecisionUI>() == null) gameObject.AddComponent<DDayFinalDecisionUI>();
            DDaySettings.Load();
            white = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            white.SetPixel(0, 0, Color.white);
            white.Apply();
        }

        void OnDestroy()
        {
            if (white != null) Destroy(white);
            if (suspectRing != null) Destroy(suspectRing);
        }

        void Styles()
        {
            if (heading != null) return;
            heading = new GUIStyle(GUI.skin.label) { fontSize = 42, fontStyle = FontStyle.Bold,
                normal = { textColor = paper } };
            title = new GUIStyle(heading) { fontSize = 83, alignment = TextAnchor.MiddleCenter };
            body = new GUIStyle(GUI.skin.label) { fontSize = 31, wordWrap = true,
                normal = { textColor = paper } };
            small = new GUIStyle(body) { fontSize = 26 };
            tiny = new GUIStyle(body) { fontSize = 23 };
            button = new GUIStyle(body) { fontSize = 30, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter, wordWrap = true,
                padding = new RectOffset(), margin = new RectOffset(),
                normal = { textColor = paper }, hover = { textColor = Color.white } };
            center = new GUIStyle(body) { alignment = TextAnchor.MiddleCenter };
            clue = new GUIStyle(body) { fontSize = 26, normal = { textColor = ink } };
        }

        void Fill(float x, float y, float w, float h, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(new Rect(x, y, w, h), white);
            GUI.color = Color.white;
        }

        bool Tap(string label, float x, float y, float w, float h, Color color)
        {
            Fill(x, y, w, h, color);
            Fill(x, y + h - 5, w, 5, gold);
            DrawText(new Rect(x + 8, y + 4, w - 16, h - 9), label, button);
            return UiButton(new Rect(x, y, w, h), label);
        }

        bool UiButton(Rect rect, string label = "")
        {
            var tutorial = GetComponent<DDayTutorialController>();
            if (tutorial != null && !tutorial.AllowsUiRect(rect)) return false;
            if (!GUI.Button(rect, "", GUIStyle.none)) return false;
            var cue = label.StartsWith("返回") || label == "取消" || label == "关闭"
                ? SfxCue.Back : label == "窗口" || label == "全屏" || label == "慢"
                    || label == "中" || label == "快" ? SfxCue.Toggle : SfxCue.Click;
            DDayAudioManager.Play(cue);
            return true;
        }

        void OnGUI()
        {
            if (game == null || game.Data == null) return;
            if (GetComponent<HEEndingPlayer>().OwnsUI) return;
            if (GetComponent<DDayFinalDecisionUI>().OwnsUI) return;
            Styles();
            if (Event.current.type == EventType.Repaint) TextLayoutIssues.Clear();
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = DDayUiSpace.Matrix;
            if (!game.BoardOpen) recordPageOpened = false;
            switch (game.CurrentPhase)
            {
                case Phase.Title: DrawTitle(); break;
                case Phase.Narrative: DrawNarrative(); break;
                case Phase.Layout:
                    GUI.enabled = !game.TasksOpen && !game.BoardOpen
                        && !game.SaveSlotsOpen && !OfficeSettingsOpen;
                    DrawLayout();
                    GUI.enabled = true;
                    if (game.TasksOpen) DrawTaskOverlay();
                    if (game.BoardOpen) DrawBoard();
                    if (game.SaveSlotsOpen) DrawSaveSlots();
                    if (OfficeSettingsOpen) DrawSettings();
                    break;
                case Phase.Conversation: DrawConversation(); break;
                case Phase.Transition: DrawTransition(); break;
                case Phase.Work: DrawWorkInProgress(); break;
                case Phase.Fatigue: DrawFatigue(); break;
                case Phase.Death: DrawDeath(); break;
                case Phase.Suspects: DrawSuspects(); break;
                case Phase.SuspectResult: DrawSuspectResult(); break;
                case Phase.Everyone: DrawEveryone(); break;
                case Phase.EveryoneResult: DrawEveryoneResult(); break;
                case Phase.Replay: DrawReplay(); break;
                case Phase.Credits: DrawCredits(); break;
                case Phase.SecondLoop:
                    GUI.enabled = !game.TasksOpen && !OfficeSettingsOpen;
                    DrawLayout();
                    GUI.enabled = true;
                    if (game.TasksOpen) DrawTaskOverlay();
                    if (OfficeSettingsOpen) DrawSettings();
                    break;
                case Phase.RescueFailure: DrawRescueFailure(); break;
                case Phase.RescueApproval: DrawRescueApproval(); break;
                case Phase.TrueEnding: DrawTrueEnding(); break;
                case Phase.AfterHours: DrawAfterHoursTransition(); break;
                case Phase.Epilogue: DrawEpilogue(); break;
                case Phase.Dinner: DrawDinner(); break;
            }
            DrawTutorialOverlay();
            GUI.matrix = previous;
        }

        void DrawTitle()
        {
            // The authored Canvas owns title controls; settings reuse the existing panel.
            if (titleCreditsOpen) { DrawCredits(); return; }
            if (!settingsOpen) return;
            var menu = FindFirstObjectByType<DDayMainMenu>();
            if (menu != null) GUI.DrawTexture(new Rect(0, 0, 1920, 1080), menu.BackgroundTexture, ScaleMode.ScaleAndCrop);
            DrawSettings();
        }

        void DrawSettings()
        {
            if (tutorialReviewOpen) { DrawTutorialReviewPanel(); return; }
            Fill(0, 0, 1920, 1080, new Color(.04f, .07f, .08f, .67f));
            PixelFrame(515, 160, 890, 760, warmDark);
            DrawText(new Rect(565, 202, 790, 70), "设置", heading);
            DrawText(new Rect(565, 290, 360, 50), "BGM 音量", body);
            DrawText(new Rect(1185, 290, 170, 50), DDaySettings.BgmPercent + "%", body);
            int volume = Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(565, 354, 790, 32),
                DDaySettings.BgmPercent, 0, 100));
            if (volume != DDaySettings.BgmPercent) DDaySettings.SetBgmPercent(volume);
            DrawText(new Rect(565, 401, 360, 50), "音效音量", body);
            DrawText(new Rect(1185, 401, 170, 50), DDaySettings.SfxPercent + "%", body);
            int soundVolume = Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(565, 465, 790, 32),
                DDaySettings.SfxPercent, 0, 100));
            if (soundVolume != DDaySettings.SfxPercent) DDaySettings.SetSfxPercent(soundVolume);
            DrawText(new Rect(565, 535, 350, 50), "显示模式", body);
            if (Tap("窗口", 920, 530, 207, 60, DDaySettings.Fullscreen
                ? new Color(.24f, .37f, .38f) : new Color(.24f, .52f, .45f)))
                DDaySettings.SetFullscreen(false);
            if (Tap("全屏", 1147, 530, 208, 60, DDaySettings.Fullscreen
                ? new Color(.24f, .52f, .45f) : new Color(.24f, .37f, .38f)))
                DDaySettings.SetFullscreen(true);
            DrawText(new Rect(565, 640, 350, 50), "文字速度", body);
            string[] speeds = { "慢", "中", "快" };
            for (int i = 0; i < speeds.Length; i++)
                if (Tap(speeds[i], 920 + i * 152, 635, 131, 60,
                    DDaySettings.TextSpeed == i ? new Color(.24f, .52f, .45f)
                        : new Color(.24f, .37f, .38f))) DDaySettings.SetTextSpeed(i);
            if (OfficeSettingsOpen)
            {
                if (Tap("返回办公室", 565, 806, 250, 60, new Color(.26f, .42f, .41f)))
                    settingsOpen = false;
                if (Tap("返回标题", 835, 806, 250, 60, new Color(.29f, .38f, .39f)))
                    pendingExit = "title";
                if (Tap("退出游戏", 1105, 806, 250, 60, new Color(.40f, .34f, .32f)))
                    pendingExit = "quit";
            }
            else if (Tap("返回主菜单", 745, 806, 430, 60, new Color(.26f, .42f, .41f)))
                settingsOpen = false;
            DrawTutorialReviewButton();
            if (pendingExit != null) DrawExitConfirmation();
        }

        void DrawExitConfirmation()
        {
            Fill(0, 0, 1920, 1080, new Color(.02f, .04f, .05f, .74f));
            PixelFrame(525, 325, 870, 430, warmDark);
            DrawText(new Rect(585, 365, 750, 65),
                pendingExit == "title" ? "返回标题？" : "退出游戏？", heading);
            DrawText(new Rect(585, 465, 750, 120),
                pendingExit == "title"
                    ? "返回标题前会写入自动存档。若当天还有未完成的操作，请先确认。"
                    : "尚未保存的当天进度可能丢失。建议先返回办公室存档。", body);
            if (Tap("取消", 585, 641, 318, 60, new Color(.26f, .42f, .41f)))
                pendingExit = null;
            if (pendingExit != null && Tap(pendingExit == "title" ? "确认返回标题" : "确认退出游戏",
                925, 641, 408, 60, new Color(.43f, .33f, .29f)))
            {
                string action = pendingExit;
                pendingExit = null;
                settingsOpen = false;
                if (action == "title") game.ReturnToTitle();
                else Application.Quit();
            }
        }

        string VisibleText(string key, string value)
        {
            if (revealKey != key)
            {
                revealKey = key;
                revealStart = Time.unscaledTime;
                revealAll = false;
            }
            int count = revealAll ? value.Length : Mathf.Min(value.Length,
                Mathf.FloorToInt((Time.unscaledTime - revealStart) * DDaySettings.CharactersPerSecond));
            return value.Substring(0, count);
        }

        bool TextRevealed(string value) => revealAll ||
            Time.unscaledTime - revealStart >= value.Length / (float)DDaySettings.CharactersPerSecond;

        void DrawNarrative()
        {
            DrawPortraitDialogue();
        }

        static readonly int[] SeptemberDates = { 0, 5, 8, 9, 10, 11, 12, 13 };

        void DrawHeader()
        {
            Fill(0, 0, 1920, 83, new Color(.18f, .15f, .13f, .91f));
            DrawText(new Rect(28, 14, 710, 62),
                "DAY " + game.Day + " · 9月" + SeptemberDates[Mathf.Clamp(game.Day, 1, 7)] + "日",
                new GUIStyle(heading) { fontSize = 35, normal = { textColor = warmText } });
        }

        void DrawLayout()
        {
            DrawDay1Layout();
        }

        readonly Color warmDark = new Color(.19f, .16f, .14f, .94f);
        readonly Color warmPanel = new Color(.25f, .20f, .17f, .94f);
        readonly Color warmEdge = new Color(.57f, .45f, .32f, 1f);
        readonly Color warmText = new Color(.98f, .92f, .80f, 1f);

        void PixelFrame(float x, float y, float w, float h, Color fill)
        {
            Fill(x + 5, y + 6, w, h, new Color(.04f, .04f, .04f, .35f));
            Fill(x, y, w, h, warmEdge);
            Fill(x + 3, y + 3, w - 6, h - 6, fill);
            Fill(x + 6, y + 6, w - 12, 2, new Color(.85f, .72f, .53f, .65f));
        }

        bool PixelButton(string label, float x, float y, float w, float h, bool enabled)
        {
            Rect rect = new Rect(x, y, w, h);
            Vector2 mouse = DDayUiSpace.FromScreen(Input.mousePosition);
            bool hover = enabled && rect.Contains(mouse);
            PixelFrame(x, y, w, h, enabled
                ? (hover ? new Color(.39f, .44f, .31f, .97f) : new Color(.31f, .38f, .30f, .96f))
                : new Color(.31f, .29f, .26f, .86f));
            DrawText(new Rect(x + 12, y + 4, w - 24, h - 8), label,
                new GUIStyle(button) { fontSize = 27, normal = { textColor = warmText } });
            return enabled && UiButton(rect, label);
        }

        void DrawDay1Layout()
        {
            var warmBody = new GUIStyle(small) { normal = { textColor = warmText } };
            // The office continues behind each lightweight HUD element.
            DrawHeader();
            DrawDay1FacilityHover();
            DrawDay1StaffCards();
            if (SeatDebugVisible && Application.isEditor) DrawSeatDebug();
            if (!string.IsNullOrEmpty(game.FlashClue) && Time.unscaledTime < game.FlashUntil
                && string.IsNullOrEmpty(GetComponent<DDayTutorialController>()?.Discovery))
            {
                string message = SeatRules.PublicText(game.FlashClue);
                float width = message == "记录册已更新" ? 300 : 770;
                float height = Mathf.Max(58, warmBody.CalcHeight(new GUIContent(message), width - 32) + 20);
                float x = 960 - width * .5f, y = 942 - height;
                PixelFrame(x, y, width, height, new Color(.27f, .37f, .27f, .97f));
                DrawText(new Rect(x + 16, y + 10, width - 32, height - 20), message,
                    new GUIStyle(warmBody) { alignment = TextAnchor.MiddleCenter });
            }
        }

        public void SetPublicProfile(int animal)
        {
            day1InfoAnimal = animal;
            day1InfoOpen = animal >= 0 && animal < SeatRules.Ids.Length && game.Seats[animal] >= 0;
            revealPublicProfile = day1InfoOpen;
        }

        void DrawSeatDebug()
        {
            int seat = game.SelectedSeat;
            if (seat < 0 || seat >= 8) return;
            PixelFrame(25, 168, 270, 235, new Color(.14f, .17f, .19f, .96f));
            DrawText(new Rect(39, 180, 235, 47), "Seat " + seat + " · DEBUG",
                new GUIStyle(small) { normal = { textColor = warmText } });
            int coffee = SeatRules.FacilityDistance(seat, "coffee");
            int printer = SeatRules.FacilityDistance(seat, "printer");
            DrawText(new Rect(39, 229, 235, 155),
                (coffee <= 1 ? "✓" : "×") + " 咖啡区  " + coffee + "\n" +
                (printer <= 1 ? "✓" : "×") + " 打印区  " + printer + "\n" +
                (SeatRules.IsWindowSeat(seat) ? "✓" : "×") + " 窗边",
                new GUIStyle(tiny) { fontSize = 23, normal = { textColor = warmText } });
        }

        void DrawDay1FacilityHover()
        {
            Vector2 mouse = DDayUiSpace.FromScreen(Input.mousePosition);
            if (new Rect(500, 982, 920, 84).Contains(mouse)) return;
            string[] labels = { "休息区", "咖啡区", "打印区", "老板办公室" };
            Rect[] regions = {
                new Rect(0, 380, 260, 555),
                new Rect(630, 155, 285, 215), new Rect(960, 155, 315, 215),
                new Rect(1285, 900, 310, 120)
            };
            for (int i = 0; i < regions.Length; i++)
            {
                if (!regions[i].Contains(mouse)) continue;
                float width = 160;
                float x = Mathf.Min(mouse.x + 18, 1910 - width);
                float y = Mathf.Min(mouse.y + 14, 932);
                PixelFrame(x, y, width, 38,
                    new Color(.26f, .20f, .16f, .94f));
                DrawText(new Rect(x + 3, y + 3, width - 6, 31), labels[i],
                    new GUIStyle(tiny) { fontSize = 20, alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = warmText } });
                break;
            }
        }

        void DrawDay1AnimalNames()
        {
            for (int i = 0; i < 7; i++)
            {
                if (game.Seats[i] < 0 || game.DragAnimal == i) continue;
                Vector2 p = WorldUi(game.AnimalWorldPoint(i));
                if (p.x < 250 || p.x > 1595 || p.y < 260 || p.y > 850) continue;
                Vector2 mouse = DDayUiSpace.FromScreen(Input.mousePosition);
                if (game.SelectedAnimal != i && Vector2.Distance(mouse, p) > 67f) continue;
                float w = 145;
                PixelFrame(p.x - w / 2, p.y - 22, w, 30,
                    game.SelectedAnimal == i ? new Color(.39f, .37f, .24f, .96f) : warmDark);
                DrawText(new Rect(p.x - w / 2 + 3, p.y - 20, w - 6, 25),
                    SeatRules.DisplayName(SeatRules.Ids[i]),
                    new GUIStyle(tiny) { fontSize = 18, alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = warmText } });
            }
        }

        void DrawDay1StaffCards()
        {
            const int panelTop = 300; // Below the restroom door; central appliances remain unobstructed.
            int count = game.Tasks.Cards.Length;
            var taskStyle = new GUIStyle(tiny) { fontSize = 20, wordWrap = true,
                normal = { textColor = warmText } };
            bool compact = count > 1;
            string[] titles = game.Tasks.Cards.Select(card => {
                int owner = game.Tasks.Owner(card.id);
                string taskTitle = compact ? card.title.Replace("接手SISY的", "") : card.title;
                return taskTitle + (compact ? " → " + (owner < 0 ? "未分配"
                    : SeatRules.DisplayName(SeatRules.Ids[owner])) : "");
            }).ToArray();
            int[] titleHeights = titles.Select(text => Mathf.Max(27,
                Mathf.CeilToInt(taskStyle.CalcHeight(new GUIContent(text), 373)))).ToArray();
            int taskHeight = 132 + titleHeights.Sum() + count * (compact ? 6 : 36) + (count == 0 ? 20 : 0);
            TutorialTaskRect = new Rect(1494, panelTop, 410, taskHeight);
            PixelFrame(1494, panelTop, 410, taskHeight, warmDark);
            DrawText(new Rect(1511, panelTop + 8, 375, 42), "今日任务", new GUIStyle(body)
                { fontSize = 25, normal = { textColor = warmText } });
            if (count == 0)
                DrawText(new Rect(1511, panelTop + 45, 373, 53), "今日没有普通工作",
                    new GUIStyle(tiny) { fontSize = 20, normal = { textColor = warmText } });
            int taskY = panelTop + 52;
            for (int t = 0; t < count; t++)
            {
                var card = game.Tasks.Cards[t];
                int owner = game.Tasks.Owner(card.id);
                DrawText(new Rect(1511, taskY, 373, titleHeights[t]), titles[t], taskStyle);
                if (!compact) DrawText(new Rect(1511, taskY + titleHeights[t], 373, 33),
                    "负责人：" + (owner < 0 ? "未分配" : SeatRules.DisplayName(SeatRules.Ids[owner])),
                    new GUIStyle(tiny) { fontSize = 18, normal = { textColor = warmText } });
                taskY += titleHeights[t] + (compact ? 6 : 36);
            }
            if (count > 0 && PixelButton("分配任务", 1511, panelTop + taskHeight - 72,
                375, 60, true)) game.ToggleTasks();
            DrawSeatRuleList(panelTop + 12 + taskHeight);
        }

        Vector2 seatRuleScroll;
        int rulesLoop, rulesDay;

        void DrawSeatRuleList(int y)
        {
            if (rulesLoop != game.Loop || rulesDay != game.Day)
            {
                seatRuleScroll = Vector2.zero; SetPublicProfile(-1);
                rulesLoop = game.Loop; rulesDay = game.Day;
            }
            var groups = game.CurrentDay.rules.GroupBy(rule => rule.animal).ToArray();
            GUIStyle ruleStyle = new GUIStyle(tiny) { fontSize = 19, wordWrap = true,
                normal = { textColor = warmText } };
            string[] texts = groups.Select(group => SeatRules.DisplayName(group.Key) + "："
                + string.Join("；", group.Select(rule => rule.text).Distinct())).ToArray();
            int[] rowHeights = texts.Select(text => Mathf.Max(50,
                Mathf.CeilToInt(ruleStyle.CalcHeight(new GUIContent(text), 314)) + 12)).ToArray();
            // The original row already supplies the name and today's public seat preferences.
            string[] profiles = groups.Select(group => day1InfoOpen
                && group.Key == SeatRules.Ids[day1InfoAnimal]
                ? game.Data.profiles.FirstOrDefault(p => p.id == group.Key)?.@public ?? "" : "").ToArray();
            for (int i = 0; i < profiles.Length; i++)
            {
                string[] parts = profiles[i].Split('｜');
                if (parts.Length == 2) profiles[i] = "职位：" + parts[0] + "\n性格：" + parts[1];
            }
            int[] profileHeights = profiles.Select(text => string.IsNullOrEmpty(text) ? 0
                : Mathf.CeilToInt(ruleStyle.CalcHeight(new GUIContent(text), 314)) + 16).ToArray();
            var tutorial = GetComponent<DDayTutorialController>();
            bool allowProfile = tutorial == null || !tutorial.SpeechVisible || tutorial.IsEmployeeStep;
            int viewportHeight = Mathf.Max(80, Mathf.Min(rowHeights.Sum(), 970 - y - 58));
            if (revealPublicProfile)
            {
                int row = System.Array.FindIndex(profiles, text => !string.IsNullOrEmpty(text));
                if (row >= 0)
                {
                    int bottom = rowHeights.Take(row + 1).Sum() + profileHeights[row];
                    seatRuleScroll.y = Mathf.Max(0, bottom - viewportHeight);
                }
                revealPublicProfile = false;
            }
            int height = 58 + viewportHeight;
            TutorialRulesRect = new Rect(1494, y, 410, height);
            PixelFrame(1494, y, 410, height, warmDark);
            DrawText(new Rect(1511, y + 9, 375, 36), "今日座位要求",
                new GUIStyle(small) { fontSize = 23,
                    normal = { textColor = warmText } });
            seatRuleScroll = GUI.BeginScrollView(new Rect(1511, y + 49, 375, viewportHeight),
                seatRuleScroll, new Rect(0, 0, 353, rowHeights.Sum() + profileHeights.Sum()), false, false);
            int rowY = 0;
            for (int row = 0; row < groups.Length; row++)
            {
                var group = groups[row].ToArray();
                bool met = group.All(rule => SeatRules.Satisfied(rule, game.Seats, game.RabbitSeat));
                int rowHeight = rowHeights[row];
                if (row % 2 == 0) Fill(0, rowY, 353, rowHeight - 2,
                    new Color(.31f, .27f, .22f, .62f));
                DrawText(new Rect(0, rowY + 4, 28, 36), met ? "✓" : "×",
                    new GUIStyle(tiny) { fontSize = 21,
                        normal = { textColor = met ? new Color(.57f, .84f, .60f)
                            : new Color(.66f, .65f, .62f) } });
                DrawText(new Rect(36, rowY + 4, 314, rowHeight - 8),
                    texts[row], ruleStyle);
                if (allowProfile && GUI.Button(new Rect(0, rowY, 353, rowHeight - 2), "", GUIStyle.none))
                {
                    int animal = SeatRules.Index(groups[row].Key);
                    game.SelectAnimal(animal); SetPublicProfile(animal);
                    if (tutorial != null && tutorial.IsEmployeeStep) game.BlockDialogueInputForTutorial();
                    DDayAudioManager.Play(SfxCue.Click);
                }
                int detailHeight = profileHeights[row];
                if (detailHeight > 0)
                {
                    Fill(0, rowY + rowHeight, 353, detailHeight - 2, warmPanel);
                    DrawText(new Rect(36, rowY + rowHeight + 6, 314, detailHeight - 12),
                        profiles[row], ruleStyle);
                }
                rowY += rowHeight + detailHeight;
            }
            GUI.EndScrollView();
        }

        void DrawDay1RuleTooltip(int animal, int y)
        {
            PixelFrame(1235, y, 350, 104, new Color(.23f, .19f, .16f, .98f));
            DrawText(new Rect(1250, y + 11, 312, 30),
                SeatRules.DisplayName(SeatRules.Ids[animal]) + " · 工位需求",
                new GUIStyle(small) { fontSize = 22, normal = { textColor = warmText } });
            string requirements = string.Join("\n", game.CurrentDay.rules
                .Where(rule => rule.animal == SeatRules.Ids[animal])
                .Select(rule => (SeatRules.Satisfied(rule, game.Seats, game.RabbitSeat) ? "✓ " : "! ") + rule.text));
            DrawText(new Rect(1250, y + 46, 315, 51), requirements,
                new GUIStyle(tiny) { fontSize = 21, normal = { textColor = warmText } });
        }

        void DrawDay1CluesOrInfo()
        {
            if (!day1InfoOpen) return;
            PixelFrame(1602, 883, 300, 185, warmDark);
            if (day1InfoOpen && day1InfoAnimal >= 0)
            {
                int index = day1InfoAnimal;
                var profile = game.Data.profiles.FirstOrDefault(p => p.id == SeatRules.Ids[index]);
                DrawText(new Rect(1616, 894, 270, 33),
                    SeatRules.DisplayName(SeatRules.Ids[index]) + " · "
                    + (game.Seats[index] >= 0 ? "在岗" : "缺席"),
                    new GUIStyle(body) { fontSize = 21, normal = { textColor = warmText } });
                DrawText(new Rect(1616, 927, 270, 48), profile == null ? "" : profile.@public,
                    new GUIStyle(tiny) { fontSize = 18, normal = { textColor = warmText } });
                string rules = string.Join("\n", game.CurrentDay.rules
                    .Where(rule => rule.animal == SeatRules.Ids[index])
                    .Select(rule => (SeatRules.Satisfied(rule, game.Seats, game.RabbitSeat) ? "✓ " : "! ") + rule.text));
                DrawText(new Rect(1616, 976, 270, 84), rules,
                    new GUIStyle(tiny) { fontSize = 18, normal = { textColor = warmText } });
            }
        }

        Vector2 WorldUi(Vector3 world)
        {
            Vector3 point = Camera.main.WorldToScreenPoint(world);
            return DDayUiSpace.FromScreen(point);
        }

        void DrawDay2Layout(bool interactive = true)
        {
            Color navy = new Color(.055f, .105f, .135f, .97f);
            Fill(0, 0, DDayGame.OfficeSafeLeft, 1080, navy);
            Fill(DDayGame.OfficeSafeRight, 0, 1920 - DDayGame.OfficeSafeRight, 1080, navy);
            Fill(DDayGame.OfficeSafeLeft - 4, 0, 4, 1080, gold);
            Fill(DDayGame.OfficeSafeRight, 0, 4, 1080, gold);
            DrawText(new Rect(28, 35, 230, 85), "D-DAY", new GUIStyle(title) { fontSize = 64 });
            DrawText(new Rect(31, 121, 220, 42), "动物办公室", small);
            Fill(22, 192, 233, 114, new Color(.13f, .23f, .27f));
            DrawText(new Rect(38, 205, 200, 49), "DAY 2", heading);
            DrawText(new Rect(38, 257, 203, 38), "兔子的空工位", tiny);
            Fill(22, 335, 233, 215, new Color(.13f, .23f, .27f));
            DrawText(new Rect(38, 351, 205, 46), "今日目标", body);
            DrawText(new Rect(38, 406, 205, 67), "安排座位\n并分配工作", tiny);
            Fill(22, 579, 233, 227, new Color(.13f, .23f, .27f));
            DrawText(new Rect(38, 592, 210, 46), "规则提示", body);
            DrawText(new Rect(38, 653, 205, 135), "● 绿色：满足\n● 红色：未满足\n● 拖动：换座 / 交换", tiny);
            DrawText(new Rect(25, 930, 245, 111), "拖动员工调整位置。\n相邻时留意对话气泡。", tiny);

            DrawFacilityLabels();
            DrawAnimalLabels();
            // Seat discoveries resolve immediately when a character moves.
            DrawDay2RightPanel(interactive);

            Fill(DDayGame.OfficeSafeLeft, 962,
                DDayGame.OfficeSafeRight - DDayGame.OfficeSafeLeft, 118,
                new Color(.06f, .13f, .16f, .95f));
            DrawText(new Rect(312, 984, 530, 62),
                "已满足 " + game.Satisfied + " / " + game.TotalRules + " 项规则", body);
            if (game.Tasks.Complete)
            {
                if (interactive && Tap("结束工作  →", 1110, 978, 332, 82,
                    new Color(.22f, .47f, .40f))) game.StartWork();
            }
            else
            {
                Fill(1110, 978, 332, 82, new Color(.19f, .27f, .29f));
                DrawText(new Rect(1118, 989, 316, 58),
                    "先分配任务", button);
            }
            if (!string.IsNullOrEmpty(game.FlashClue) && Time.unscaledTime < game.FlashUntil)
            {
                Fill(520, 856, 700, 76, new Color(.09f, .30f, .28f, .94f));
                DrawText(new Rect(545, 867, 650, 58), game.FlashClue, small);
            }
        }

        void DrawFacilityLabels()
        {
            string[] names = { "洗手间", "打印机", "咖啡机" };
            for (int i = 0; i < 3; i++)
            {
                Vector2 p = WorldUi(game.FacilityWorldPoint(i));
                Fill(p.x - 54, p.y - 20, 108, 39, new Color(.07f, .13f, .16f, .95f));
                DrawText(new Rect(p.x - 48, p.y - 16, 96, 33), names[i],
                    new GUIStyle(tiny) { alignment = TextAnchor.MiddleCenter });
            }
        }

        void DrawAnimalLabels()
        {
            for (int i = 0; i < 7; i++)
            {
                if (game.Seats[i] < 0 || game.DragAnimal == i) continue;
                Vector2 p = WorldUi(game.AnimalWorldPoint(i));
                if (p.y < 220 || p.y > 860) continue;
                float x = Mathf.Clamp(p.x, DDayGame.OfficeSafeLeft + 57,
                    DDayGame.OfficeSafeRight - 57);
                Fill(x - 42, p.y - 25, 84, 35,
                    game.SelectedAnimal == i ? new Color(.22f, .44f, .40f, .97f)
                        : new Color(.08f, .15f, .18f, .94f));
                DrawText(new Rect(x - 78, p.y - 23, 156, 32),
                    SeatRules.DisplayName(SeatRules.Ids[i]),
                    new GUIStyle(tiny) { alignment = TextAnchor.MiddleCenter });
            }
        }

        void DrawBubbles()
        {
            foreach (var ev in game.AvailableConversations)
            {
                int a = SeatRules.Index(ev.a), b = SeatRules.Index(ev.b);
                Vector2 ap = WorldUi(game.AnimalWorldPoint(a));
                Vector2 bp = WorldUi(game.AnimalWorldPoint(b));
                float x = Mathf.Clamp((ap.x + bp.x) * .5f,
                    DDayGame.OfficeSafeLeft + DDayGame.OfficeSafeMargin + 33f,
                    DDayGame.OfficeSafeRight - DDayGame.OfficeSafeMargin - 33f);
                float y = Mathf.Min(ap.y, bp.y) - 68;
                if (y < 185 || y > 850) continue;
                Fill(x - 31, y - 3, 62, 47, new Color(.96f, .92f, .80f));
                DrawText(new Rect(x - 26, y - 13, 52, 51), "...",
                    new GUIStyle(heading) { fontSize = 34, alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = ink } });
                if (UiButton(new Rect(x - 33, y - 5, 66, 52)))
                    game.OpenConversation(ev.id);
            }
        }

        void DrawDay2RightPanel(bool interactive)
        {
            string[] tabs = { "员工状态", "线索 (" + game.ItemClueCount + ")" };
            for (int i = 0; i < 2; i++)
                if (Tap(tabs[i], 1497 + i * 206, 22, 195, 62,
                    rightTab == i ? new Color(.24f, .43f, .43f) : new Color(.12f, .23f, .28f)))
                    rightTab = i;
            if (rightTab == 1) { DrawItemPanel(); return; }

            int row = 0;
            for (int i = 0; i < 7; i++)
            {
                if (game.Seats[i] < 0) continue;
                int y = 102 + row * 115;
                Fill(1498, y, 402, 106, game.SelectedAnimal == i
                    ? new Color(.21f, .39f, .41f) : new Color(.12f, .23f, .28f));
                Texture2D sheet = DDayArt.EmployeeSheet;
                if (sheet != null)
                {
                    Rect uv = DDayArt.EmployeeUv(i);
                    GUI.DrawTextureWithTexCoords(new Rect(1509, y + 5, 73, 93), sheet,
                        new Rect(uv.x, uv.y + uv.height * .37f, uv.width, uv.height * .63f), true);
                }
                DrawText(new Rect(1598, y + 9, 260, 35),
                    SeatRules.DisplayName(SeatRules.Ids[i]), small);
                int line = 0;
                foreach (var rule in game.CurrentDay.rules)
                {
                    if (rule.animal != SeatRules.Ids[i]) continue;
                    bool ok = SeatRules.Satisfied(rule, game.Seats, game.RabbitSeat);
                    var style = new GUIStyle(tiny) { fontSize = 20,
                        normal = { textColor = ok ? new Color(.58f, .94f, .66f) : new Color(1f, .52f, .47f) } };
                    DrawText(new Rect(1598, y + 44 + line * 27, 282, 29),
                        (ok ? "✓ " : "✗ ") + rule.text, style);
                    line++;
                }
                if (interactive && UiButton(new Rect(1498, y, 402, 106)))
                    game.SelectAnimal(i);
                row++;
            }
            DrawText(new Rect(1512, 823, 365, 210),
                "规则随座位即时变化。\n绿灯满足，红灯待调整。\n\n兔子的工位保持空置。", tiny);
        }

        void DrawItemPanel()
        {
            DrawText(new Rect(1507, 109, 370, 45), "已发现的物品", body);
            string[] ids = { "hire_form", "coffee_cup" };
            int row = 0;
            foreach (string id in ids)
            {
                if (!game.HasClue(id) && !(id == "coffee_cup" && game.HasClue("last_coffee"))) continue;
                var item = game.Data.clues.FirstOrDefault(c => c.id == id);
                if (item == null) continue;
                int y = 180 + row * 116;
                if (Tap("▣  " + item.title, 1502, y, 390, 99,
                    selectedItem == id ? new Color(.27f, .44f, .43f) : new Color(.14f, .26f, .30f)))
                    selectedItem = id;
                row++;
            }
            if (row == 0)
                DrawText(new Rect(1510, 193, 365, 130), "还没有物品线索。\n留意相邻员工的谈话。", small);
            if (!game.HasClue(selectedItem) && !(selectedItem == "coffee_cup" && game.HasClue("last_coffee"))) return;
            var selected = game.Data.clues.FirstOrDefault(c => c.id == selectedItem);
            if (selected == null) return;
            Fill(1502, 574, 390, 310, new Color(.13f, .25f, .29f));
            DrawText(new Rect(1520, 596, 350, 53), selected.title, body);
            string detail = selected.description;
            if (selectedItem == "coffee_cup")
            {
                if (game.HasClue("coffee_source")) detail += "\n咖啡来自柴犬。";
                if (game.HasClue("coffee_clear")) detail += "\n检测未发现异常。";
            }
            if (selectedItem == "hire_form" && game.HasClue("hire_secret"))
                detail += "\n招聘筛选曾被绕过。";
            DrawText(new Rect(1520, 665, 350, 185), detail, small);
        }

        void DrawConversation()
        {
            DrawLayout();
            Fill(0, 0, 1920, 1080, new Color(.07f, .06f, .05f, .42f));
            PixelFrame(350, 705, 1060, 268, warmDark);
            DrawText(new Rect(389, 728, 780, 50),
                SeatRules.PublicText(game.ActiveConversation.title), heading);
            string rawLine = SeatRules.PublicText(game.ConversationLine);
            string line = System.Text.RegularExpressions.Regex.Replace(
                VisibleText("c:" + game.Day + ":" + game.ActiveConversation.id
                    + ":" + game.ConversationIndex, rawLine),
                "【([^】]+)】", "<color=#ff6360>【$1】</color>");
            DrawText(new Rect(389, 800, 915, 103), line,
                new GUIStyle(body) { richText = true, fontSize = 32 });
            DrawText(new Rect(1165, 730, 210, 42),
                (game.ConversationIndex + 1) + " / " + game.ActiveConversation.lines.Length, small);
            if (Tap(!TextRevealed(rawLine) ? "显示全部"
                    : game.ConversationIndex == game.ActiveConversation.lines.Length - 1
                        ? "结束对话" : "继续  ›", 1140, 900, 226, 60,
                    new Color(.23f, .46f, .42f)))
            {
                if (TextRevealed(rawLine)) game.AdvanceConversation();
                else revealAll = true;
            }
        }

        void DrawTransition()
        {
            DrawLayout();
            Fill(0, 0, 1920, 1080, new Color(.025f, .07f, .10f, game.TransitionAlpha));
            if (game.TransitionAlpha > .45f)
                DrawText(new Rect(600, 470, 720, 130), "DAY 2  →  DAY 3", title);
        }

        void DrawSidebar()
        {
            Fill(1495, 118, 425, 962, new Color(.11f, .23f, .27f, .95f));
            DrawText(new Rect(1526, 143, 340, 58), "员工状态", heading);
            for (int i = game.Loop == 1 && game.Day >= 2 ? 1 : 0; i < 7; i++)
            {
                if (game.Seats[i] < 0) continue;
                int count = 0, total = 0;
                foreach (var rule in game.CurrentDay.rules)
                {
                    if (rule.animal != SeatRules.Ids[i]) continue;
                    total++;
                    if (SeatRules.Satisfied(rule, game.Seats, game.RabbitSeat)) count++;
                }
                int y = 213 + (game.Loop == 1 && game.Day >= 2 ? i - 1 : i) * 63;
                Fill(1518, y, 375, 55, game.SelectedAnimal == i
                    ? new Color(.32f, .53f, .47f) : new Color(.19f, .34f, .36f));
                DrawText(new Rect(1535, y + 8, 350, 45),
                    SeatRules.DisplayName(SeatRules.Ids[i]) + "  "
                    + (count == total ? "✓" : count == 0 ? "!" : "·")
                    + "  " + count + "/" + total, small);
                if (UiButton(new Rect(1518, y, 375, 55))) game.SelectAnimal(i);
            }
            int chosen = game.SelectedAnimal;
            if (chosen < 0 || game.Seats[chosen] < 0)
            {
                DrawText(new Rect(1530, 670, 350, 180), "点击员工查看需求。\n拖动角色至空位或其他工位。", small);
                return;
            }
            Fill(1518, 654, 375, 2, gold);
            DrawText(new Rect(1530, 668, 350, 54),
                SeatRules.DisplayName(SeatRules.Ids[chosen]) + " · 工位 "
                + game.Seats[chosen], body);
            int line = 0;
            foreach (var rule in game.CurrentDay.rules)
            {
                if (rule.animal != SeatRules.Ids[chosen]) continue;
                bool ok = SeatRules.Satisfied(rule, game.Seats, game.RabbitSeat);
                string message = (ok ? "✓ " : "× ")
                    + (rule.group == "心理需求" ? "心·" : "工·") + rule.text;
                DrawText(new Rect(1533, 721 + line * 50, 345, 50), message, tiny);
                line++;
            }
            if (game.Day >= 4)
            {
                var profile = game.Data.profiles.FirstOrDefault(p => p.id == SeatRules.Ids[chosen]);
                if (profile != null)
                {
                    DrawText(new Rect(1530, 890, 350, 39), profile.@public, tiny);
                    DrawText(new Rect(1530, 932, 350, 125),
                        game.HasClue(profile.clue) ? profile.hidden : "隐藏档案：？？？", tiny);
                }
            }
        }

        void DrawBoard()
        {
            Fill(0, 0, 1920, 1080, new Color(.06f, .08f, .08f, .73f));
            PixelFrame(240, 105, 1440, 870, warmDark);
            DrawText(new Rect(282, 133, 650, 70), "记录册",
                new GUIStyle(heading) { normal = { textColor = warmText } });
            if (Tap("×", 1560, 139, 80, 60, new Color(.32f, .40f, .39f))) game.ToggleBoard();
            string[] tabs = { "物品", "情报" };
            for (int t = 0; t < 2; t++)
            {
                int found = game.Records.Where(record => record.category == tabs[t])
                    .Select(record => record.id).Distinct().Count();
                string tabLabel = tabs[t] + "（" + found + "/"
                    + game.RecordTotal(tabs[t]) + "）";
                if (Tap(tabLabel, 282 + t * 270, 221, 251, 60,
                    t == game.BoardTab ? new Color(.30f, .44f, .35f)
                        : new Color(.26f, .34f, .32f)))
                { game.SetBoardTab(t); recordPageOpened = false; }
            }
            string category = tabs[game.BoardTab];
            var entries = game.Records.Where(c => c.category == category).ToArray();
            if (!recordPageOpened || !entries.Any(item => item.id == selectedRecordId))
            {
                selectedRecordId = (entries.FirstOrDefault(item => item.unread) ?? entries.FirstOrDefault())?.id;
                recordDetailScroll = Vector2.zero; recordPageOpened = true;
            }
            var listStyle = new GUIStyle(small) { fontSize = 25, wordWrap = true,
                alignment = TextAnchor.MiddleLeft, normal = { textColor = warmText } };
            // One row rhythm for the directory; long headings wrap without changing type size.
            int rowHeight = Mathf.Max(84, entries.Length == 0 ? 84 : entries.Max(item =>
                Mathf.CeilToInt(listStyle.CalcHeight(new GUIContent(RecordListTitle(item.title)), 455)) + 24));
            int[] heights = entries.Select(item => rowHeight).ToArray();
            recordScroll = GUI.BeginScrollView(new Rect(282, 306, 540, 621), recordScroll,
                new Rect(0, 0, 513, Mathf.Max(621, heights.Sum())));
            int rowY = 0;
            for (int i = 0; i < entries.Length; i++)
            {
                var item = entries[i];
                int y = rowY, height = heights[i];
                Fill(0, y, 513, height - 6, item.id == selectedRecordId
                    ? new Color(.38f, .43f, .31f) : i % 2 == 0
                        ? warmPanel : new Color(.22f, .19f, .17f, .98f));
                DrawText(new Rect(18, y + 9, 455, height - 24), RecordListTitle(item.title), listStyle);
                if (UiButton(new Rect(0, y, 513, height - 6)))
                { selectedRecordId = item.id; recordDetailScroll = Vector2.zero; }
                rowY += height;
            }
            GUI.EndScrollView();
            PixelFrame(850, 306, 780, 621, warmPanel);
            var selected = entries.FirstOrDefault(item => item.id == selectedRecordId);
            if (selected == null)
                DrawText(new Rect(888, 354, 700, 100), "尚未获得" + category,
                    new GUIStyle(small) { normal = { textColor = warmText } });
            else
            {
                var detailTitle = new GUIStyle(heading) { fontSize = 34, wordWrap = true,
                    normal = { textColor = warmText } };
                var detailBody = new GUIStyle(small) { fontSize = 27, wordWrap = true,
                    normal = { textColor = warmText } };
                string fullTitle = SeatRules.PublicText(selected.title);
                string description = SeatRules.PublicText(selected.description);
                float titleHeight = detailTitle.CalcHeight(new GUIContent(fullTitle), 677);
                float bodyHeight = detailBody.CalcHeight(new GUIContent(description), 677);
                float bodyY = titleHeight + 85;
                recordDetailScroll = GUI.BeginScrollView(new Rect(880, 332, 718, 567), recordDetailScroll,
                    new Rect(0, 0, 691, Mathf.Max(567, bodyY + bodyHeight + 24)));
                DrawText(new Rect(0, 0, 677, titleHeight), fullTitle, detailTitle);
                DrawText(new Rect(0, titleHeight + 22, 677, 40),
                    "9月" + SeptemberDates[Mathf.Clamp(selected.day, 1, 7)] + "日",
                    new GUIStyle(small) { normal = { textColor = pale } });
                DrawText(new Rect(0, bodyY, 677, bodyHeight), description, detailBody);
                GUI.EndScrollView();
                if (Event.current.type == EventType.Repaint) game.MarkRecordRead(selected.id);
            }
        }

        static string RecordListTitle(string fullTitle)
        {
            // Display the authored short heading only; keep the complete title in details/save.
            int separator = fullTitle.IndexOf('：');
            string titleText = separator >= 4 ? fullTitle.Substring(0, separator) : fullTitle;
            return SeatRules.PublicText(titleText);
        }

        void DrawText(Rect rect, string text, GUIStyle style)
        {
            if (Event.current.type == EventType.Repaint && !string.IsNullOrEmpty(text))
            {
                float required = style.CalcHeight(new GUIContent(text), rect.width);
                if (required > rect.height + 2)
                    TextLayoutIssues.Add(text + " [needs " + required + ", has " + rect.height + "]");
            }
            GUI.Label(rect, text, style);
        }

        void DrawSaveSlots()
        {
            Fill(0, 0, 1920, 1080, new Color(.06f, .13f, .16f, .75f));
            PixelFrame(240, 105, 1440, 870, new Color(.18f, .29f, .28f, .99f));
            DrawText(new Rect(282, 133, 920, 70), "手动存档 · 15 个槽位", heading);
            if (PixelButton("返回办公室", 1390, 139, 250, 60, true))
            {
                pendingOverwriteSlot = 0;
                game.ToggleSaveSlots();
            }
            for (int slot = 1; slot <= DDayGame.ManualSlotCount; slot++)
            {
                ManualSlotInfo info = game.GetManualSlotInfo(slot);
                int column = (slot - 1) % 3, row = (slot - 1) / 3;
                float x = 300 + column * 445, y = 247 + row * 119;
                string label = "槽位 " + slot.ToString("D2") + " · " +
                    (info.exists ? info.loop + "周目 Day " + info.day + "\n" + info.savedAt : "空槽位");
                if (PixelButton(label, x, y, 413, 101, true))
                {
                    if (info.exists) pendingOverwriteSlot = slot;
                    else if (game.SaveManualSlot(slot, false)) game.ToggleSaveSlots();
                }
            }
            if (pendingOverwriteSlot > 0)
            {
                Fill(270, 846, 1380, 106, new Color(.10f, .20f, .21f, .98f));
                DrawText(new Rect(305, 858, 710, 75),
                    "覆盖槽位 " + pendingOverwriteSlot.ToString("D2") + "？旧内容会保留备份。", small);
                if (PixelButton("确认覆盖", 1080, 865, 220, 60, true))
                {
                    if (game.SaveManualSlot(pendingOverwriteSlot, true)) game.ToggleSaveSlots();
                    pendingOverwriteSlot = 0;
                }
                if (PixelButton("取消", 1330, 865, 220, 60, true)) pendingOverwriteSlot = 0;
            }
            else DrawText(new Rect(300, 862, 1050, 62),
                "自动进度按天保存，与此处的 15 个手动槽位分开。", small);
        }

        void DrawFatigue()
        {
            DrawHeader();
            Fill(125, 145, 1280, 112, new Color(.12f, .24f, .25f, .94f));
            DrawText(new Rect(160, 165, 1200, 85), "任务已经启动。兔子明显不舒服，工作却仍在继续。", body);
            Fill(0, 952, 1920, 128, new Color(.09f, .19f, .22f, .96f));
            DrawText(new Rect(125, 976, 850, 75), game.SelectedAnimal == 0
                ? "兔子仍在工位，老板要求继续工作。" : "点击兔子，检查 TA 的工作状态。", body);
            if (game.SelectedAnimal == 0 && Tap("继续工作", 1175, 970, 600, 89,
                new Color(.28f, .49f, .43f))) game.ContinueFatigue();
        }

        void DrawDeath()
        {
            Fill(0, 0, 1920, 1080, game.DeathStage == 0
                ? new Color(.04f, .10f, .13f, .46f) : Color.black);
            if (game.DeathStage == 0)
                DrawText(new Rect(400, 440, 1120, 180),
                    "工作仍在继续", title);
            if (game.DeathStage >= 1)
                DrawText(new Rect(650, 305, 620, 110), "18:15", title);
            if (game.DeathStage >= 2)
                DrawText(new Rect(510, 498, 900, 100), "兔子倒下了。", center);
            if (game.DeathStage >= 3)
            {
                DrawText(new Rect(440, 587, 1040, 100),
                    "同事拨通急救电话。TA 没有再醒来。", center);
                if (Tap("DAY 1 END  ·  继续", 680, 748, 560, 84,
                    new Color(.28f, .39f, .43f))) game.ContinueDeath();
            }
        }

        void DrawSuspectResult()
        {
            Fill(0, 0, 1920, 1080, new Color(.06f, .13f, .17f, .82f));
            Fill(365, 245, 1190, 615, new Color(.15f, .31f, .34f, .97f));
            DrawText(new Rect(450, 290, 1020, 110), "可能的动机与证据", heading);
            DrawText(new Rect(450, 420, 1000, 150), game.SuspectEvidence(game.SuspectId), body);
            DrawText(new Rect(450, 620, 1000, 90), "但没有证据表明此人杀死兔子。", heading);
            if (Tap("排除并继续", 700, 750, 520, 78, new Color(.22f, .48f, .47f)))
                game.ExcludeSuspect();
        }

        void DrawEveryone()
        {
            Fill(0, 0, 1920, 1080, new Color(.05f, .11f, .14f, .90f));
            DrawText(new Rect(385, 230, 1150, 120), "谁杀死了兔子？", title);
            DrawText(new Rect(450, 410, 1020, 105), "六个人，都有各自的理由和未说出的事。", center);
            if (Tap("所有人", 720, 635, 480, 105, new Color(.39f, .43f, .43f)))
                game.ChooseEveryone();
        }

        void DrawEveryoneResult()
        {
            Fill(0, 0, 1920, 1080, new Color(.04f, .10f, .13f, .94f));
            DrawText(new Rect(330, 330, 1260, 120), "这仍然不是完整答案。", title);
            DrawText(new Rect(440, 490, 1040, 90), "把责任平均分给所有人，仍然是在寻找凶手。", center);
            if (Tap("没有凶手", 710, 705, 500, 100, new Color(.23f, .47f, .46f)))
                game.ChooseNoCulprit();
        }

        void DrawReplay()
        {
            Fill(0, 0, 1920, 1080, new Color(.03f, .08f, .11f, .48f));
            Fill(300, 700, 1320, 260, new Color(.09f, .20f, .23f, .97f));
            DrawText(new Rect(350, 725, 1150, 54), "DAY 1 · 重看", heading);
            DrawText(new Rect(350, 798, 1080, 100), game.ReplayLine, body);
            if (Tap("下一幕", 1345, 868, 230, 72, new Color(.21f, .46f, .45f)))
                game.AdvanceReplay();
        }

        void DrawCredits()
        {
            Fill(0, 0, 1920, 1080, new Color(.05f, .11f, .14f, .93f));
            DrawText(new Rect(390, 270, 1140, 150), "D-DAY", title);
            DrawText(new Rect(370, 460, 1180, 200),
                "有些结果，从来没有一个真正的凶手。", center);
            if (Tap("返回主菜单", 735, 735, 450, 91, new Color(.22f, .47f, .44f)))
            {
                if (game.CurrentPhase == Phase.Title) titleCreditsOpen = false;
                else game.ReturnToTitle();
            }
        }

        void DrawTaskButton()
        {
            if (PixelButton(game.TasksOpen
                    ? "关闭任务  ×" : "任务分配  " + game.Tasks.Count + "/" + game.Tasks.Cards.Length,
                842, 135, 296, 64, true)) game.ToggleTasks();
        }

        void DrawTaskOverlay()
        {
            Fill(0, 0, 1920, 1080, new Color(.07f, .06f, .05f, .72f));
            PixelFrame(305, 154, 1310, 770, warmDark);
            DrawText(new Rect(348, 183, 850, 65), "分配任务", heading);
            var cards = game.Tasks.Cards;
            if (cards.Length == 0)
                DrawText(new Rect(365, 300, 620, 90), "今日无普通任务", body);
            selectedTaskIndex = Mathf.Clamp(selectedTaskIndex, 0, Mathf.Max(0, cards.Length - 1));
            var taskTitleStyle = new GUIStyle(small) { fontSize = 25 };
            var taskDescriptionStyle = new GUIStyle(tiny) { fontSize = 21 };
            var taskOwnerStyle = new GUIStyle(tiny) { fontSize = 20 };
            float taskY = 267;
            for (int i = 0; i < cards.Length; i++)
            {
                float y = taskY;
                var card = cards[i];
                int owner = game.Tasks.Owner(card.id);
                string taskTitle = SeatRules.PublicText(card.title);
                string taskDescription = SeatRules.PublicText(card.description);
                string taskOwner = "负责人：" + (owner < 0 ? "未分配"
                        : SeatRules.DisplayName(SeatRules.Ids[owner]));
                float titleHeight = taskTitleStyle.CalcHeight(new GUIContent(taskTitle), 634);
                float descriptionHeight = string.IsNullOrWhiteSpace(taskDescription) ? 0
                    : taskDescriptionStyle.CalcHeight(new GUIContent(taskDescription), 634);
                float ownerHeight = taskOwnerStyle.CalcHeight(new GUIContent(taskOwner), 634);
                float rowHeight = titleHeight + descriptionHeight + ownerHeight + 26;
                Fill(348, y, 673, rowHeight, selectedTaskIndex == i
                    ? new Color(.39f, .38f, .28f) : warmPanel);
                DrawText(new Rect(365, y + 8, 634, titleHeight), taskTitle, taskTitleStyle);
                DrawText(new Rect(365, y + 12 + titleHeight, 634, descriptionHeight),
                    taskDescription, taskDescriptionStyle);
                DrawText(new Rect(365, y + 16 + titleHeight + descriptionHeight, 634, ownerHeight),
                    taskOwner, new GUIStyle(taskOwnerStyle) {
                        normal = { textColor = owner < 0 ? warmText
                            : new Color(.62f, .87f, .62f) } });
                if (UiButton(new Rect(348, y, 673, rowHeight)))
                    selectedTaskIndex = i;
                taskY += rowHeight + 8;
            }
            if (game.Loop == 2 && cards.Length > 0)
            {
                if (PixelButton(taskHistoryOpen ? "收起历史" : "上次负责人",
                    348, 730, 300, 60, true)) taskHistoryOpen = !taskHistoryOpen;
                if (taskHistoryOpen)
                {
                    string id = cards[selectedTaskIndex].id;
                    var previous = game.TaskHistory.Where(entry => entry.taskId == id)
                        .Reverse().Take(2).ToArray();
                    DrawText(new Rect(355, 795, 655, 84), previous.Length == 0
                        ? "暂无这项任务的历史分配"
                        : string.Join("\n", previous.Select(entry => "第" + entry.loop + "周目 DAY "
                            + entry.day + " → " + SeatRules.DisplayName(SeatRules.Ids[entry.owner]))),
                        new GUIStyle(small) { fontSize = 23 });
                }
            }
            Fill(1043, 267, 528, 628, warmPanel);
            DrawText(new Rect(1065, 282, 470, 49), "负责人", body);
            int visibleRow = 0;
            for (int i = 0; i < 7; i++)
            {
                if (game.Seats[i] < 0) continue;
                int y = 333 + visibleRow++ * 74;
                bool assigned = cards.Length > 0
                    && game.Tasks.Owner(cards[selectedTaskIndex].id) == i;
                PixelFrame(1063, y, 483, 67, assigned
                    ? new Color(.37f, .46f, .31f, .98f)
                    : new Color(.27f, .32f, .27f, .98f));
                Texture2D portrait = assigned && DDayArt.TaskReactionSheet != null
                    ? DDayArt.TaskReactionSheet : DDayArt.EmployeeSheet;
                if (portrait != null)
                    GUI.DrawTextureWithTexCoords(new Rect(1076, y + 3, 64, 60), portrait,
                        DDayArt.TaskPortraitUv(i, assigned && DDayArt.TaskReactionSheet != null), true);
                DrawText(new Rect(1155, y + 11, 258, 44),
                    SeatRules.DisplayName(SeatRules.Ids[i]),
                    new GUIStyle(small) { fontSize = 25,
                        normal = { textColor = warmText } });
                if (assigned) DrawText(new Rect(1415, y + 15, 115, 38), "✓ 已负责",
                    new GUIStyle(tiny) { fontSize = 19,
                        normal = { textColor = new Color(.65f, .91f, .66f) } });
                if (cards.Length > 0 && GUI.Button(new Rect(1063, y, 483, 67), "",
                    GUIStyle.none)) game.AssignTask(cards[selectedTaskIndex].id, i);
            }
            if (PixelButton("返回办公室", 1321, 184, 250, 60, true)) game.ToggleTasks();
        }

        void DrawSecondLoop()
        {
            Color navy = new Color(.055f, .105f, .135f, .97f);
            Fill(0, 0, DDayGame.OfficeSafeLeft, 1080, navy);
            Fill(DDayGame.OfficeSafeRight, 0, 1920 - DDayGame.OfficeSafeRight, 1080, navy);
            Fill(DDayGame.OfficeSafeLeft - 4, 0, 4, 1080, gold);
            Fill(DDayGame.OfficeSafeRight, 0, 4, 1080, gold);
            DrawText(new Rect(27, 31, 235, 76), "D-DAY", new GUIStyle(title) { fontSize = 62 });
            PixelFrame(20, 183, 235, 202, new Color(.14f, .25f, .28f));
            DrawText(new Rect(36, 198, 207, 42), "第二周目", body);
            DrawText(new Rect(36, 260, 205, 110), "请让兔子活下来。", small);
            PixelFrame(20, 417, 235, 270, new Color(.14f, .25f, .28f));
            DrawText(new Rect(34, 435, 205, 230),
                "调整座位，重新分配工作。\n\n相邻的人可能发现异常，也可能制造压力。", tiny);
            DrawAnimalLabels();
            DrawText(new Rect(1517, 34, 365, 70), "工作流", heading);
            var cards = game.Tasks.Cards;
            for (int i = 0; i < cards.Length; i++)
            {
                int y = 112 + i * 116;
                Fill(1505, y, 393, 105, new Color(.13f, .25f, .28f));
                int owner = game.Tasks.Owner(cards[i].id);
                DrawText(new Rect(1520, y + 10, 360, 38),
                    SeatRules.PublicText(cards[i].title), small);
                DrawText(new Rect(1520, y + 56, 360, 38),
                    owner < 0 ? "未分配" : "交给 "
                        + SeatRules.DisplayName(SeatRules.Ids[owner]), tiny);
            }
            Fill(DDayGame.OfficeSafeLeft, 960,
                DDayGame.OfficeSafeRight - DDayGame.OfficeSafeLeft, 120, navy);
            DrawText(new Rect(307, 987, 610, 61),
                "已分配 " + game.Tasks.Count + "/" + cards.Length + " 项任务", body);
            if (PixelButton(game.Tasks.Complete ? "运行 D-DAY  →" : "先分配所有任务",
                1092, 977, 341, 80, game.Tasks.Complete)) game.RunSecondLoop();
        }

        void DrawRescueFailure()
        {
            Fill(0, 0, 1920, 1080, new Color(.04f, .10f, .13f, .86f));
            PixelFrame(360, 222, 1200, 640, new Color(.15f, .27f, .29f));
            DrawText(new Rect(428, 267, 1080, 95), SeatRules.PublicText(game.Day == 1
                ? "这一次，SISY 仍然倒下了" : "信息交接尚未完成"), heading);
            DrawText(new Rect(428, 390, 1050, 100),
                SeatRules.PublicText("这条工作链仍会让 SISY 独自承受风险。"), body);
            DrawText(new Rect(428, 512, 1035, 153),
                SeatRules.PublicText(game.RescueFailureText), body);
            DrawText(new Rect(428, 689, 1035, 62), "调整布局与分工，再运行当天工作。", small);
            if (Tap("重新安排当天", 718, 765, 486, 74,
                new Color(.25f, .48f, .43f))) game.RetrySecondLoop();
        }

        void DrawRescueApproval()
        {
            Fill(0, 0, 1920, 1080, new Color(.04f, .10f, .13f, .82f));
            PixelFrame(360, 203, 1200, 670, new Color(.15f, .27f, .29f));
            DrawText(new Rect(430, 257, 1060, 80), "18:15 · 改变结果的机会", heading);
            DrawText(new Rect(430, 380, 1050, 180), SeatRules.PublicText(
                "ORIN 借工作接触发现 SISY 的疲惫。\nTHANE 劝 TA 离开；这一次你可以批准。"), body);
            DrawText(new Rect(430, 590, 1050, 120),
                SeatRules.PublicText("SISY 离开后，今天的工作无法全部完成。是否批准提前离开？"), body);
            if (Tap("批准提前离开", 468, 754, 432, 78,
                new Color(.24f, .49f, .42f))) game.ApproveRabbitLeave(true);
            if (Tap("要求继续工作", 999, 754, 432, 78,
                new Color(.41f, .35f, .30f))) game.ApproveRabbitLeave(false);
        }

        void DrawTrueEnding()
        {
            Fill(0, 0, 1920, 1080, Color.black);
            DrawText(new Rect(700, 750, 520, 110), "D-DAY", title);
            if (Tap("返回主菜单", 746, 918, 430, 76,
                new Color(.17f, .33f, .34f))) game.ReturnToTitle();
        }

        void DrawGameMenu()
        {
            if (Tap("记录册", 23, 837, 230, 48, new Color(.17f, .36f, .37f))) game.ToggleBoard();
            if (Tap("保存进度", 23, 892, 230, 48, new Color(.17f, .36f, .37f))) game.SaveGame();
            if (Tap("主菜单", 23, 947, 230, 48, new Color(.17f, .36f, .37f))) game.ReturnToTitle();
        }

        void DrawDinner()
        {
            Fill(0, 0, 1920, 1080, new Color(.09f, .08f, .08f, .90f));
            PixelFrame(260, 140, 1400, 810, new Color(.24f, .20f, .17f, .98f));
            DrawText(new Rect(350, 206, 1220, 78), "9.12 · 公司聚餐", heading);
            DrawText(new Rect(360, 335, 1200, 215), SeatRules.PublicText(game.Loop == 1
                ? "SISY 的椅子空着。\n同事们第一次把分散的接触与工作放在同一张桌上。"
                : "SISY 坐在桌边。\n晋升、旧日争执和 X 项目成为今晚的话题。"), body);
            DrawText(new Rect(360, 608, 1200, 110),
                "这一天，大家终于坐在同一张桌前。", small);
            if (PixelButton("开始聚餐", 744, 806, 430, 77, true)) game.CompleteDinner();
        }

        void DrawWorkInProgress()
        {
            DrawHeader();
            Fill(315, 925, 1160, 115, new Color(.09f, .20f, .23f, .93f));
            DrawText(new Rect(350, 947, 1080, 80),
                string.IsNullOrEmpty(game.WorkLabel) ? "今天的工作开始了。"
                    : SeatRules.PublicText(game.WorkLabel), body);
            if (game.WorkActor >= 0 && game.Seats[game.WorkActor] >= 0)
            {
                Vector2 p = WorldUi(game.AnimalWorldPoint(game.WorkActor));
                Fill(p.x - 50, p.y - 73, 100, 42, new Color(.12f, .29f, .28f, .94f));
                DrawText(new Rect(p.x - 39, p.y - 68, 80, 35), "工作中…",
                    new GUIStyle(tiny) { fontSize = 20 });
            }
        }

        void DrawEpilogue()
        {
            string[] moments = {
                "APIS 的地下业务被揭露。旧公司的体系逐渐瓦解。",
                "SISY 把未做完的文件留在桌上，和 ORIN 一起走向下班的街道。TA 不再需要靠推石头证明自己。",
                "ORIN 想起曾经不敢回头的雨夜。这次 TA 停下来等 SISY，并肩走过路口。",
                "NARCIS 把晋升通知收进抽屉，认真写下自己真正想做的事。",
                "ECHO 没有再等父亲或 NARCIS 替 TA 回答。TA 自己接过电话，说出了决定。",
                "CASSIA 把一直不敢说出的异常告诉 METIS。信息终于有了安全的出口。",
                "METIS 把证据交出，不必再把智慧藏在 APIS 内部。",
                "THANE 面对新的命令，第一次说了‘不’。TA 看着同伴走远，没有追上去。",
                "办公室空了。工位仍在，雨后的光慢慢暗下。"
            };
            Fill(0, 0, 1920, 1080, new Color(.02f, .06f, .08f, .72f));
            if (game.EpilogueIndex == moments.Length - 1)
                DrawText(new Rect(450, 180, 1020, 115), "APIS · 空办公室", heading);
            else DrawText(new Rect(450, 180, 1020, 115), "后来", heading);
            DrawText(new Rect(410, 380, 1100, 285),
                SeatRules.PublicText(moments[Mathf.Clamp(game.EpilogueIndex, 0,
                    moments.Length - 1)]), center);
            if (Tap("继续", 745, 865, 430, 80, new Color(.17f, .36f, .36f)))
                game.AdvanceEpilogue();
        }
    }
}
