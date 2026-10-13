using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DDay;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Runs the real Day1 campaign with an isolated save, real drag-release callback and UGUI listeners.
[InitializeOnLoad]
public static class DDayTutorialVerification
{
    const string Key = "DDay.TutorialVerification";
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
    static string Output => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Docs/TutorialRepairVerification");
    static string Save => Path.Combine(Output, "TestAutoSave.json");
    static IEnumerator run;
    static DDayGame game;
    static DDayTutorialController guide;
    [Serializable] sealed class TutorialSave { public DDayTutorialState tutorial; }
    static readonly List<string> evidence = new List<string>();
    static DateTime started;
    static DDayTutorialVerification()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        if (!SessionState.GetBool(Key, false)) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.update += Tick;
        else Release(); // An interrupted verification must not own the next ordinary Play.
    }
    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if ((state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode)
            && SessionState.GetBool(Key, false)) Release();
    }
    static void Release()
    {
        EditorApplication.update -= Tick; SessionState.SetBool(Key, false); run = null;
        Environment.SetEnvironmentVariable("DDAY_SAVE_OVERRIDE", SessionState.GetString(Key + ".Override", ""));
        if (SessionState.GetBool(Key + ".SizeStored", false))
        { var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            type.GetProperty("selectedSizeIndex", Hidden).SetValue(EditorWindow.GetWindow(type), SessionState.GetInt(Key + ".Size", 0));
            SessionState.SetBool(Key + ".SizeStored", false); }
    }
    [MenuItem("D-DAY/Tutorial/Verify Real Day1 in Play Mode")]
    public static void Start()
    {
        if (EditorApplication.isPlaying) return;
        Directory.CreateDirectory(Output); run = null; evidence.Clear();
        SessionState.SetString(Key + ".Override", Environment.GetEnvironmentVariable("DDAY_SAVE_OVERRIDE") ?? "");
        Environment.SetEnvironmentVariable("DDAY_SAVE_OVERRIDE", Save);
        SessionState.SetBool(Key, true); EditorApplication.update -= Tick; EditorApplication.update += Tick;
        EditorApplication.isPlaying = true;
    }
    static void Tick()
    {
        if (!SessionState.GetBool(Key, false)) { EditorApplication.update -= Tick; return; }
        if (!EditorApplication.isPlaying) return;
        try
        {
            if (run == null) { started = DateTime.UtcNow; run = Sequence(); }
            if ((DateTime.UtcNow - started).TotalSeconds > 180) throw new Exception("Tutorial test timeout");
            if (!run.MoveNext()) Finish(true, "Real Day1 through B2 to Day2 passed. UI callbacks/rendering tested; native physical mouse not claimed.");
        }
        catch (Exception e) { Finish(false, e.ToString()); }
    }
    static void Check(bool met, string text) { if (!met) throw new Exception(text); evidence.Add("PASS | " + text); }
    static IEnumerable<object> Wait(float seconds)
    { double end = Time.realtimeSinceStartupAsDouble + seconds; while (Time.realtimeSinceStartupAsDouble < end) yield return null; }
    static void Stage(string id) => Check(guide.State.stepId == id && guide.Active, "Tutorial stage " + id);
    static IEnumerable<object> SpeakTo(string id)
    {
        int attempts = 0;
        while (guide.State.stepId != id)
        {
            Check(guide.CanAdvance, "Real dialogue/profile gate can advance " + guide.State.stepId);
            guide.Advance(); foreach (var t in Wait(.23f)) yield return t;
            if (++attempts > 25) throw new Exception("Tutorial advance guard");
        }
    }
    static IEnumerable<object> ReadAction()
    {
        string step = guide.State.stepId;
        foreach (var t in Wait(.25f)) yield return t;
        Check(guide.SpeechVisible && guide.GuideRect == DDayUI.PortraitDialogueRect, "Operation speech uses the official bottom dialogue: " + step);
        int attempts = 0;
        while (guide.SpeechVisible)
        {
            game.GetComponent<DDayUI>().AdvanceDialogueInput();
            foreach (var t in Wait(.25f)) yield return t;
            if (++attempts > 6) throw new Exception("Operation speech did not retract: " + step);
        }
        Check(guide.State.stepId == step && guide.Visible, "Reading speech does not complete actual operation: " + step);
    }
    static void Drain()
    { int guard = 0; while (game.CurrentPhase == Phase.Narrative && game.CurrentPagePanel == null)
        { game.AdvanceNarrative(); if (++guard > 200) throw new Exception("Narrative failed to finish"); } }
    static IEnumerable<object> Capture(string name)
    {
        foreach (var t in Wait(.25f)) yield return t;
        // Inspect the complete authored sentence, not only the initial typewriter fragment.
        typeof(DDayUI).GetField("revealAll", Hidden).SetValue(game.GetComponent<DDayUI>(), true);
        foreach (var t in Wait(.15f)) yield return t;
        Check(game.GetComponent<DDayUI>().TextLayoutIssues.Count == 0,
            "No text overflow in " + name + ": " + string.Join(" | ", game.GetComponent<DDayUI>().TextLayoutIssues));
        ScreenCapture.CaptureScreenshot(Path.Combine(Output, name + ".png"));
        foreach (var t in Wait(.20f)) yield return t;
        evidence.Add("SCREENSHOT | " + name);
    }
    static void Drag(int animal, int seat, bool moved = true)
    {
        var renderers = (SpriteRenderer[])typeof(DDayGame).GetField("animalRenderers", Hidden).GetValue(game);
        typeof(DDayGame).GetField("draggedStartPosition", Hidden).SetValue(game, renderers[animal].transform.position);
        typeof(DDayGame).GetMethod("MarkSeatDragMoved", Hidden).Invoke(game, null);
        typeof(DDayGame).GetMethod("FinishSeatDrag", Hidden).Invoke(game, new object[] { animal, seat, moved });
    }
    static void SolveRealSeats()
    {
        object[] args = { game.CurrentDay, 1, 1, null };
        int count = (int)typeof(DDayABPlayVerification).GetMethod("CountSolutions", Hidden).Invoke(null, args);
        Check(count == 44, "Formal Day1 still has 44 exhaustive solutions");
        int[] answer = (int[])args[3];
        for (int i = 1; i < 7; i++) if (game.Seats[i] != answer[i]) Drag(i, answer[i]);
        Check(game.Seats.SequenceEqual(answer) && game.SeatsReady, "Real swaps/empty-seat moves satisfy every Day1 rule");
    }
    static void ButtonClick(string name)
    {
        var button = game.transform.Find("D-DAY Canvas/Bottom controls/" + name).GetComponent<Button>();
        ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current)
            { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
    }
    static IEnumerator Sequence()
    {
        do { yield return null; game = UnityEngine.Object.FindFirstObjectByType<DDayGame>(); } while (game == null || game.Data == null);
        guide = game.GetComponent<DDayTutorialController>();
        Check(guide != null && guide.Configuration.steps.Length > 0, "Scene binds one configured tutorial controller");
        Check(game.GetComponents<DDayTutorialController>().Length == 1, "No duplicate controller");
        Resolution(1280, 720); foreach (var t in Wait(.6f)) yield return t;
        game.GetComponent<DDayMainMenu>();
        var menu = UnityEngine.Object.FindFirstObjectByType<DDayMainMenu>();
        menu.start.onClick.Invoke(); foreach (var t in Wait(.9f)) yield return t;
        Stage("T00"); Check(game.CurrentPhase == Phase.Layout && game.Day == 1 && game.Loop == 1, "Start menu enters formal Day1 directly");
        Check(game.Records.Count == 0 && game.Tasks.Count == 0, "Teaching does not grant records or owners");
        foreach (var t in Capture("01_Opening_1280x720")) yield return t;
        string startStep = guide.State.stepId; int line = guide.State.line;
        guide.Advance(); guide.Advance();
        Check(guide.State.stepId == startStep && guide.State.line == line + 1, "Rapid double advance only moves one teaching sentence");
        foreach (var t in Wait(.23f)) yield return t;
        foreach (var t in SpeakTo("T01")) yield return t;
        int beforeSpace = guide.State.line;
        SendViewEvent(new Event { type = EventType.KeyDown, keyCode = KeyCode.Space });
        foreach (var t in Wait(.25f)) yield return t;
        if (guide.State.line == beforeSpace) {
            SendViewEvent(new Event { type = EventType.KeyDown, keyCode = KeyCode.Space });
            foreach (var t in Wait(.25f)) yield return t;
        }
        Check(guide.State.line == beforeSpace + 1, "GameView Space reveals then advances shared SISY dialogue");
        foreach (var t in SpeakTo("T02")) yield return t;
        Check(!guide.CanAdvance, "Employee operation cannot be skipped with Space/Continue");
        SendViewEvent(new Event { type = EventType.KeyDown, keyCode = KeyCode.Space });
        foreach (var t in Wait(.25f)) yield return t;
        Check(guide.State.stepId == "T02" && guide.State.viewedEmployees == 0,
            "Space cannot replace the required public-profile operation");
        game.SelectAnimal(1); Check(guide.State.viewedEmployees == 2 && guide.CanAdvance, "One actual public profile suffices; employee red dot state recorded");
        game.SaveGame(); string employeeSave = File.ReadAllText(Save); game.ReturnToTitle(); game.ContinueGame();
        Check(guide.State.stepId == "T02" && guide.State.viewedEmployees == 2, "Continue restores employee step and viewed red dot");
        foreach (var t in Capture("02_Employees_1280x720")) yield return t;
        foreach (var t in SpeakTo("T03a")) yield return t;
        Stage("T03a");
        foreach (var t in Capture("03_DragInstructions_1280x720")) yield return t;
        foreach (var t in ReadAction()) yield return t;
        Check(!guide.CanAdvance, "After retracting speech, demonstration still requires a real drag");
        int originalSisy = game.Seats[0]; Check(!game.TryPlaceAnimal(0, 0) && game.Seats[0] == originalSisy, "Formal fixed B3 remains locked");
        int wolfBefore = game.Seats[1]; Drag(1, -1); Check(game.Seats[1] == wolfBefore && guide.State.stepId == "T03a", "Invalid drop leaves demo incomplete and seat unchanged");
        Drag(1, 1); Check(!guide.DemoRuleSatisfied() && guide.State.stepId == "T03a", "Non-edge legal placement keeps teaching gate unsatisfied");
        foreach (var t in Capture("03_DragTargets_1280x720")) yield return t;
        int legal = Enumerable.Range(0, 8).First(s => guide.DemoSeatAllowed(s) && s != game.Seats[1]); Drag(1, legal);
        Stage("T03b"); foreach (var t in ReadAction()) yield return t;
        Check(!game.SeatsReady && !guide.BlocksOfficePointer(new Vector2(900, 500)), "Free seating removes forced world input restrictions");
        typeof(DDayGame).GetProperty("DragAnimal", Hidden).SetValue(game, 1);
        typeof(DDayTutorialController).GetMethod("StopIdleTimer", Hidden).Invoke(guide, null);
        var idle = (IEnumerator)typeof(DDayTutorialController).GetMethod("IdleHint", Hidden).Invoke(guide, null);
        Check(idle.MoveNext(), "Idle reminder starts with its realtime wait"); idle.MoveNext();
        Check(!guide.ShowIdleHint && !guide.SpeechVisible, "Idle reminder never blocks a held drag or loses its mouse release");
        typeof(DDayGame).GetProperty("DragAnimal", Hidden).SetValue(game, -1);
        game.ToggleTasks(); game.StartWork(); Check(!game.TasksOpen && game.CurrentPhase == Phase.Layout, "Unmet formal A1 blocks B1 and work");
        game.SaveGame(); string freeSave = File.ReadAllText(Save); game.ReturnToTitle(); game.ContinueGame();
        Check(guide.State.stepId == "T03b" && guide.State.demoPlaced, "Continue restores completed drag and free seating");
        foreach (var t in Capture("04_FreeSeats_1280x720")) yield return t;
        // Real optional A2: exact dialogue completes before award, with no synthetic clue.
        ABClue clue = game.FindSeatExplorations().First(); int host = game.ExplorationHost(clue);
        foreach (var t in Wait(.15f)) yield return t;
        var bubble = game.transform.Find("D-DAY Canvas/Explore " + SeatRules.Ids[host]).GetComponent<Button>();
        Check(bubble.gameObject.activeInHierarchy, "Actual A2 bubble remains available before A1 solved");
        ExecuteEvents.Execute(bubble.gameObject, new PointerEventData(EventSystem.current)
            { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
        Check(game.CurrentPhase == Phase.Narrative && game.Records.Count == 0 && !guide.Visible, "A2 owns its dialogue; tutorial hides and awards nothing on opening");
        Drain(); Check(game.Records.Count == 1 && game.Records[0].id == clue.id && guide.State.firstDiscoveryShown,
            "Actual A2 completion records the first discovery exactly once");
        Check(!game.FindSeatExplorations().Any(c => c.id == clue.id), "Real A2 claim still deduplicates");
        foreach (var t in Capture("05_ActualDiscovery_1280x720")) yield return t;
        game.ToggleBoard(); game.SetBoardTab(1); game.ToggleBoard();
        Check(guide.State.bookTabs == 0 && !guide.State.bookOpened, "Early optional book browsing cannot replace the required post-task visit");
        SolveRealSeats(); Stage("T04");
        foreach (var t in Capture("06_AssignHint_1280x720")) yield return t;
        foreach (var t in ReadAction()) yield return t;
        game.ToggleTasks();
        Check(game.TasksOpen && !guide.SpeechVisible, "Assignment panel has no tutorial dialogue over its controls");
        foreach (var t in Capture("06b_AssignmentPanel_1280x720")) yield return t;
        string task = game.Tasks.Cards.First().id; game.AssignTask(task, 1);
        Check(game.Tasks.Owner(task) == 1 && game.CurrentPhase == Phase.Narrative && !guide.Visible,
            "Non-SISY owner allowed; original B1 feedback owns dialogue");
        foreach (var t in Capture("07_FormalB1Feedback_1280x720")) yield return t;
        if (game.NarrativeCount > 1) game.AdvanceNarrative();
        int feedbackIndex = game.NarrativeIndex; string feedbackLine = game.NarrativeLine;
        game.SaveGame(); game.ReturnToTitle(); game.ContinueGame();
        Check(game.CurrentPhase == Phase.Narrative && game.NarrativeIndex == feedbackIndex
            && game.NarrativeLine == feedbackLine && game.Tasks.Owner(task) == 1 && guide.State.stepId == "T04",
            "Mid-B1 Continue restores exact formal feedback line without reassigning task");
        Drain(); Stage("T08");
        Check(!game.TasksOpen && !game.BoardOpen, "Formal B1 feedback automatically returns to the office before record teaching");
        Check(!guide.State.bookOpened && guide.State.bookTabs == 0, "Book stage starts with actual post-task actions still required");
        foreach (var t in Capture("07b_RecordGuideOffice_1280x720")) yield return t;
        foreach (var t in ReadAction()) yield return t;
        ButtonClick("开始工作"); Check(game.CurrentPhase == Phase.Layout, "Start Work awaits real book operation during tutorial");
        ButtonClick("记录册"); Check(game.BoardOpen, "Actual toolbar opens record book");
        game.SetBoardTab(0); game.ToggleBoard(); foreach (var t in Wait(.15f)) yield return t;
        Check(guide.State.stepId == "T08", "One book category alone cannot complete teaching");
        ButtonClick("记录册"); game.SetBoardTab(1);
        foreach (var t in Capture("08_RecordBook_1280x720")) yield return t;
        Check(game.Records.Count == 1, "Book teaching does not issue rewards");
        Check(!game.HasUnreadRecords, "Viewing the actual new record clears its existing unread badge");
        game.ToggleBoard(); Stage("T09");
        foreach (var t in SpeakTo("T05")) yield return t;
        Resolution(1920, 1080); foreach (var t in Wait(.6f)) yield return t;
        foreach (var t in Capture("09_StartWorkSpeech_1920x1080")) yield return t;
        foreach (var t in ReadAction()) yield return t;
        foreach (var t in Capture("09b_StartWorkTarget_1920x1080")) yield return t;
        ButtonClick("开始工作"); Check(game.CurrentPhase == Phase.Work && guide.State.stepId == "T06" && !guide.Visible,
            "Real Start Work starts original work; no teaching mask over animation");
        game.SaveGame(); game.ReturnToTitle(); game.ContinueGame();
        Check(game.CurrentPhase == Phase.Work && guide.State.stepId == "T06" && !guide.Visible,
            "Mid-work Continue restores original work playback without repeating B1 response");
        double until = Time.realtimeSinceStartupAsDouble + 8;
        while (game.CurrentPagePanel == null && Time.realtimeSinceStartupAsDouble < until)
        { Check(game.CurrentPhase != Phase.WorkSummary, "No extra work-finished popup"); yield return null; }
        Check(guide.State.done && !guide.Active && game.CurrentPagePanel != null && game.NarrativeTitle.Contains("18:15"),
            "Original work completion finishes tutorial and automatically starts 18:15 B2");
        foreach (var t in Capture("10_Original1815Comic_1920x1080")) yield return t;
        int b2Frames = 0;
        while (game.Day == 1 && game.CurrentPagePanel != null)
        {
            while (Time.frameCount <= game.DialogueInputBlockedUntilFrame) yield return null;
            int index = game.NarrativeIndex; game.GetComponent<DDayUI>().AdvanceDialogueInput();
            foreach (var t in Wait(.12f)) yield return t;
            if (game.NarrativeIndex != index || game.Day != 1) b2Frames++;
            if (b2Frames > 25) throw new Exception("Day1 B2 frame guard");
        }
        until = Time.realtimeSinceStartupAsDouble + 3;
        while (game.Day == 1 && Time.realtimeSinceStartupAsDouble < until) yield return null;
        Check(game.Day == 2 && game.CompletedFixedEvents.Contains("B2_L1_D1") && !guide.Active,
            "Original B2 completes once, enters Day2 without repeating tutorial");
        Check(game.Seats[0] < 0 && !game.Seats.Contains(6) && !game.Records.Any(r => r.id == "orin_photo"),
            "Day2 death/B3 empty/photo not auto-granted remain unchanged");
        // Skipping and empty book are separate real Day1 runs, not fabricated teaching tasks.
        game.StartGame(); guide.Skip(); Check(guide.State.done && game.Day == 1 && !game.SeatsReady && game.Tasks.Count == 0,
            "Skip at opening removes guidance only");
        game.SaveGame(); game.ReturnToTitle(); game.ContinueGame(); Check(!guide.Active && guide.State.done, "Skipped flag persists via normal save");
        SolveRealSeats(); game.ToggleTasks(); game.AssignTask(game.Tasks.Cards.First().id, 3); Drain();
        game.ToggleTasks(); ButtonClick("开始工作");
        Check(game.CurrentPhase == Phase.Work && !guide.Active, "Opening skip still allows real seats, assignment and work");
        game.SaveGame(); game.ReturnToTitle(); game.ContinueGame();
        Check(game.CurrentPhase == Phase.Work && !guide.Active, "Work save after skipping at the opening resumes work instead of reassigning");
        double skippedUntil = Time.realtimeSinceStartupAsDouble + 8;
        while (game.CurrentPagePanel == null && Time.realtimeSinceStartupAsDouble < skippedUntil) yield return null;
        Check(game.CurrentPagePanel != null && game.NarrativeTitle.Contains("18:15"), "Skipped teaching still enters the original 18:15 B2");
        game.StartGame(); foreach (var t in SpeakTo("T02")) yield return t; game.SelectAnimal(0);
        foreach (var t in SpeakTo("T03a")) yield return t;
        foreach (var t in ReadAction()) yield return t; Drag(1, 0); Stage("T03b");
        foreach (var t in ReadAction()) yield return t;
        SolveRealSeats(); foreach (var t in ReadAction()) yield return t;
        game.ToggleTasks(); game.AssignTask(game.Tasks.Cards.First().id, 0); Drain(); Stage("T08");
        Check(!game.TasksOpen, "Empty-record run also returns automatically to office");
        foreach (var t in ReadAction()) yield return t;
        Check(game.Records.Count == 0, "No A2 discovery required to reach book teaching");
        game.ToggleBoard(0); game.SetBoardTab(1);
        foreach (var t in Capture("11_EmptyRecordBook_1920x1080")) yield return t;
        Check(game.SaveManualSlot(15, true), "Existing manual slot 15 stores tutorial alongside campaign");
        string manual = File.ReadAllText(Path.Combine(Output, "DDayManualSlot15.json"));
        Check(JsonUtility.FromJson<TutorialSave>(manual).tutorial.bookTabs == 3, "Manual slot keeps real book category state");
        File.WriteAllText(Save, manual); game.ContinueGame(); Stage("T08");
        Check(guide.State.bookTabs == 3 && !game.BoardOpen && !guide.State.bookClosed,
            "Mid-book save restores viewed categories without faking the required close");
        game.ToggleBoard(); game.ToggleBoard(); Stage("T09"); guide.Skip(); Check(!guide.Active && game.Tasks.Complete && game.SeatsReady,
            "Skip near completion retains solved seats and real owner");
        File.WriteAllText(Save, freeSave); game.ContinueGame(); Stage("T03b");
        guide.Skip(); Check(!guide.Active && guide.AllowsToolbar("开始工作"), "Skip in free seating releases all tutorial locks");
        File.WriteAllText(Save, employeeSave); game.ContinueGame(); Stage("T02");
        game.GetComponent<DDayUI>().OpenOfficeSettings();
        Check(game.GetComponent<DDayUI>().OfficeSettingsOpen && !guide.Visible, "Settings suspend overlay without changing campaign stage");
        typeof(DDayUI).GetField("tutorialReviewOpen", Hidden).SetValue(game.GetComponent<DDayUI>(), true);
        int recordsBeforeReview = game.Records.Count, ownersBeforeReview = game.Tasks.Count;
        foreach (var t in Capture("12_StaticReview_1920x1080")) yield return t;
        Check(game.Records.Count == recordsBeforeReview && game.Tasks.Count == ownersBeforeReview && guide.State.stepId == "T02",
            "Static settings review fires no tasks, clues or tutorial replay");
        typeof(DDayUI).GetField("tutorialReviewOpen", Hidden).SetValue(game.GetComponent<DDayUI>(), false);
        typeof(DDayUI).GetField("settingsOpen", Hidden).SetValue(game.GetComponent<DDayUI>(), false);
        File.WriteAllText(Save, freeSave); game.ContinueGame(); SolveRealSeats();
        foreach (var t in ReadAction()) yield return t; game.ToggleTasks();
        string assignedId = game.Tasks.Cards.First().id; game.AssignTask(assignedId, 2);
        string formalLine = game.NarrativeLine; guide.Skip();
        Check(game.CurrentPhase == Phase.Narrative && game.NarrativeLine == formalLine && game.Tasks.Owner(assignedId) == 2,
            "Skipping while B1 feedback plays preserves its dialogue and owner");
        Drain(); Check(game.CurrentPhase == Phase.Layout && game.TasksOpen && !guide.Active,
            "B1 still returns to original task panel after teaching is skipped");
        File.WriteAllText(Save, "{\"loop\":2,\"day\":1,\"phase\":\"Layout\",\"sisyAlive\":true,\"seats\":[0,1,2,3,4,5,6]}"); game.ContinueGame();
        Check(!guide.Active && game.Loop == 2, "Second-loop Day1 never starts tutorial");
        // Real BeginDay for both loops: no basic-teaching narrative, Day7 remains its original choice phase.
        for (int loop = 1; loop <= 2; loop++)
        {
            typeof(DDayGame).GetProperty("Loop", Hidden).SetValue(game, loop);
            typeof(DDayGame).GetProperty("SisyAlive", Hidden).SetValue(game, loop == 2);
            for (int day = loop == 1 ? 2 : 1; day <= 7; day++)
            {
                typeof(DDayGame).GetMethod("BeginDay", Hidden).Invoke(game, new object[] { day });
                Check(game.CurrentPhase == (day < 7 ? Phase.Layout : loop == 1 ? Phase.Suspects : Phase.FinalDecision)
                    && !guide.Active && !guide.SpeechVisible, "Direct daily entry with original phase, no repeated tutorial: loop " + loop + " day " + day);
                if (day < 7) Check(game.CurrentDay.intro.Length == 0 && game.CurrentDay.rules.Length > 0
                    && game.Tasks.Cards.Any(), "Daily task/seat requirements remain present: loop " + loop + " day " + day);
                if (loop == 1 && day == 2) foreach (var t in Capture("13_Day2_DirectOffice_1920x1080")) yield return t;
            }
        }
        // Saved completion stays respected even on Day1; old deleted-line positions remain compatible.
        File.WriteAllText(Save, freeSave); game.ContinueGame(); guide.Skip(); game.SaveGame();
        game.ReturnToTitle(); game.ContinueGame();
        Check(game.Day == 1 && game.CurrentPhase == Phase.Layout && !guide.Active, "Completed tutorial Day1 save never repeats guidance");
        File.WriteAllText(Save, manual.Replace("\"stepId\":\"T08\"", "\"stepId\":\"T09\""));
        game.ContinueGame(); guide.State.line = 1; game.SaveGame(); game.ReturnToTitle(); game.ContinueGame();
        Check(guide.State.stepId == "T05", "Old T09 save pointing at the deleted sentence resumes the next existing step");
        Check(guide.StageCount == 10 && !guide.Configuration.steps.SelectMany(x => x.lines)
            .Any(x => x.Contains("记录册只记您亲眼看到的")), "Deleted sentence absent, progress counts ten spoken steps without blank pages");
                Check(game.GetComponent<DDayUI>().TextLayoutIssues.Count == 0, "Final rendered text has no measured overflow");
    }
    static void SendViewEvent(Event input)
    {
        var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        var view = EditorWindow.GetWindow(type); view.Focus(); view.SendEvent(input); view.Repaint();
    }
    static void Resolution(int width, int height)
    {
        var assembly = typeof(EditorWindow).Assembly; var type = assembly.GetType("UnityEditor.GameView");
        var view = EditorWindow.GetWindow(type); var selected = type.GetProperty("selectedSizeIndex", Hidden);
        if (!SessionState.GetBool(Key + ".SizeStored", false))
        { SessionState.SetInt(Key + ".Size", (int)selected.GetValue(view)); SessionState.SetBool(Key + ".SizeStored", true); }
        var sizes = assembly.GetType("UnityEditor.GameViewSizes"); var singleton = sizes.BaseType.GetProperty("instance", Hidden).GetValue(null);
        var group = sizes.GetProperty("currentGroup", Hidden).GetValue(singleton);
        for (int i = 0; i < (int)group.GetType().GetMethod("GetTotalCount", Hidden).Invoke(group, null); i++)
        {
            var size = group.GetType().GetMethod("GetGameViewSize", Hidden).Invoke(group, new object[] { i }); var st = size.GetType();
            if ((int)st.GetProperty("width", Hidden).GetValue(size) == width && (int)st.GetProperty("height", Hidden).GetValue(size) == height)
            { selected.SetValue(view, i); view.Focus(); view.Repaint(); return; }
        }
        throw new Exception("Missing configured GameView size");
    }
    static void Finish(bool success, string message)
    {
        if (game != null) game.ReturnToTitle();
        Release();
        File.WriteAllLines(Path.Combine(Output, "Results.txt"), new[] { success ? "PASS" : "FAIL", message }.Concat(evidence.Distinct()));
        run = null; EditorApplication.isPlaying = false; Debug.Log("D-DAY Day1 tutorial verification " + (success ? "PASS" : "FAIL"));
    }
}
