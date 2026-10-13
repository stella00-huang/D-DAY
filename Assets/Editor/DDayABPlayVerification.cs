using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DDay;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Real Play Mode state-machine/UGUI callbacks and Game View IMGUI events, with an isolated save.
[InitializeOnLoad]
public static partial class DDayABPlayVerification
{
    const string Key = "DDayABPlayVerification";
    static IEnumerator run;
    static readonly Stack<IEnumerator> iterators = new Stack<IEnumerator>();
    static readonly List<string> evidence = new List<string>();
    static readonly Dictionary<string, int[]> answers = new Dictionary<string, int[]>();
    static string Root => Directory.GetParent(Application.dataPath).FullName;
    static string Output => Path.Combine(Root, "Docs", SessionState.GetBool(Key + ".Latest", false)
        ? "ContentUpdate_20261012/PlayVerification" : SessionState.GetBool(Key + ".Day7", false)
        ? "Day7SuspectsVerification" : "Day4Day6PageComicVerification");
    static DateTime started;
    static int requestedWidth = 1280, requestedHeight = 720;

    static DDayABPlayVerification()
    {
        if (SessionState.GetBool(Key, false))
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                Finish(false, "INTERRUPTED: Play verification stopped before native mouse cancellation and confirmation were completed.");
            else EditorApplication.update += Tick;
        }
    }

    [MenuItem("D-DAY/Verify AB and Portraits in Play Mode")]
    public static void Run()
    { StartVerification(false); }

    static void StartVerification(bool day7, bool latest = false)
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play before starting this test."); return; }
        SessionState.SetBool(Key + ".Day7", day7);
        SessionState.SetBool(Key + ".Latest", latest);
        Directory.CreateDirectory(Output);
        SessionState.SetString(Key + ".SaveOverride", Environment.GetEnvironmentVariable("DDAY_SAVE_OVERRIDE") ?? "");
        Environment.SetEnvironmentVariable("DDAY_SAVE_OVERRIDE", Path.Combine(Output, "TestAutoSave.json"));
        SessionState.SetBool(Key, true);
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.isPlaying = true;
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying) return;
        try
        {
            if (run == null) { started = DateTime.UtcNow; run = SessionState.GetBool(Key + ".Latest", false)
                ? LatestContentSequence() : SessionState.GetBool(Key + ".Day7", false)
                ? Day7Sequence() : Sequence(); iterators.Push(run); }
            if ((DateTime.UtcNow - started).TotalSeconds > 600)
                throw new InvalidOperationException("Play verification timed out.");
            if (iterators.Peek().MoveNext())
            {
                if (iterators.Peek().Current is IEnumerator child) iterators.Push(child);
            }
            else
            {
                iterators.Pop();
                if (iterators.Count == 0) Finish(true, "Dual-loop AB and portrait checks completed.");
            }
        }
        catch (Exception error) { Finish(false, error.ToString()); }
    }

    static void Check(bool pass, string description)
    {
        if (!pass) throw new InvalidOperationException(description);
        evidence.Add("PASS | " + description);
    }

    static IEnumerator Sequence()
    {
        DDayGame game = UnityEngine.Object.FindFirstObjectByType<DDayGame>();
        while (game == null || game.Data == null) { yield return null; game = UnityEngine.Object.FindFirstObjectByType<DDayGame>(); }
        evidence.Clear(); answers.Clear();
        SetResolution(1280,720); yield return null;
        Check(Application.unityVersion.StartsWith("6000.0"), "Unity 6 main project Play Mode");
        var expected = new[] {44,8,5,12,2,17,12,2,84,8,139,524};
        int resultIndex = 0;
        for (int loop = 1; loop <= 2; loop++)
        {
            DayData[] days = CampaignSchedule.MakeDays(game.Data, loop);
            for (int day = 1; day <= 6; day++)
            {
                int[] example = null;
                int count = CountSolutions(days[day - 1], loop, day, ref example);
                Check(count == expected[resultIndex++], "A1 " + loop + "/" + day + " exhaustive C# solutions=" + count);
                answers[loop + "/" + day] = example;
                ABSeatDay config = ABConfig.Current.SeatDay(loop, day);
                int rabbitSeat = loop == 1 ? 6 : config.initialSeats[0];
                int unmet = config.rules.Count(x => !SeatRules.Satisfied(x, config.initialSeats, rabbitSeat));
                Check(unmet == config.initialUnmet && unmet >= 3, "A1 " + loop + "/" + day + " initial unmet=" + unmet);
                yield return null;
            }
        }
        foreach (CharacterPortrait actor in DialoguePresentation.Current.characters)
            foreach (PortraitExpression art in actor.expressions)
            {
                Check(DialoguePresentation.Current.Portrait(actor.speakerId, art.id, out Texture2D tex,
                    out Rect uv, out bool fallback) && !fallback && uv.xMin >= 0 && uv.xMax <= 1,
                    "Portrait real asset " + actor.speakerId + "/" + art.id);
                Check(tex != null, "Portrait texture loaded");
            }
        Check(DialoguePresentation.Current.Portrait("rabbit", "angry", out _, out _, out bool missing)
            && missing, "Missing expression honestly falls back to neutral");
        var frames = DialoguePresentation.Current.Frames("A2_R04", new[] {
            "ORIN：我只是希望你别揽那么多工作。", "SISY：我工作也是为了我们有更好的生活。" }, "rabbit", 0);
        Check(frames[0].speakerId == "wolf" && frames[1].speakerId == "rabbit"
            && frames[0].partnerId == "rabbit" && frames[1].partnerId == "wolf"
            && frames[0].expressionId != frames[1].expressionId, "Explicit per-line expression + two-speaker switch");
        game.StartGame();
        while (game.CurrentPhase == Phase.Narrative) { game.AdvanceNarrative(); yield return null; }
        yield return null;
        Check(game.CurrentPhase == Phase.Layout && !game.SeatsReady,
            "Day1 starts as a puzzle, no A2 autoplay");
        Check(game.Seats[0] == 6 && !game.TryPlaceAnimal(0, 0), "L1 Day1 SISY locked B3");
        game.ToggleTasks();
        Check(!game.TasksOpen, "B1 blocked before A1 solved");
        game.StartWork();
        Check(game.CurrentPhase == Phase.Layout, "Start Work blocked by A1");
        yield return Capture("01_Day1_Office");
        int before = game.Records.Count;
        ABClue first = game.FindSeatExplorations().First();
        int host = game.ExplorationHost(first);
        var button = game.transform.Find("D-DAY Canvas/Explore " + SeatRules.Ids[host]).GetComponent<Button>();
        Check(button.gameObject.activeInHierarchy, "A2 UGUI bubble shown");
        ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current)
            { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
        Check(game.CurrentPhase == Phase.Narrative && game.Records.Count == before,
            "Bubble callback opens portrait; no record before dialogue completion");
        Check(game.CurrentDialogue != null && game.CurrentDialogue.focusId != "", "A2 portrait connected");
        yield return Capture("02_A2_Portrait");
        if (game.NarrativeCount >= 2)
        {
            var ui = game.GetComponent<DDayUI>();
            FieldInfo reveal = typeof(DDayUI).GetField("revealAll",BindingFlags.Instance|BindingFlags.NonPublic);
            int index = game.NarrativeIndex;
            string speaker = game.CurrentDialogue.speakerId;
            // GameView.SendEvent targets Editor GUI, not reliably the player input surface.
            // Exercise the same input handler; hardware input remains explicitly unverified.
            while(Time.frameCount<=game.DialogueInputBlockedUntilFrame) yield return null;
            reveal.SetValue(ui,false);
            typeof(DDayUI).GetField("revealStart",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(ui,Time.unscaledTime+.05f);
            Check(ui.AdvanceDialogueInput(), "Dialogue handler accepts input after frame guard");
            Check(game.NarrativeIndex == index && (bool)reveal.GetValue(ui), "Dialogue input handler reveals text before advancing");
            yield return null;
            ui.AdvanceDialogueInput();
            Check(game.NarrativeIndex == index + 1, "Second dialogue input advances exactly one line");
            Check(game.CurrentDialogue.speakerId != speaker, "Actual A2 frame switches speaking portrait");
            yield return Capture("02b_A2_SecondSpeaker");
        }
        while (game.CurrentPhase == Phase.Narrative) { game.AdvanceNarrative(); yield return null; }
        Check(game.Records.Count == before + 1 && game.TriggeredSeatEvents.Contains(first.id), "A2 completion records once");
        Check(game.HasUnreadRecords && game.FlashClue == "记录册已更新", "A2 completion only shows short record update, with unread state");
        yield return null;
        var badge=game.transform.Find("D-DAY Canvas/Bottom controls/记录册/Unread badge");
        Check(badge!=null && badge.gameObject.activeInHierarchy,"Record button unread badge visible");
        yield return Capture("02c_RecordUpdate");
        Check(!game.FindSeatExplorations().Any(x => x.id == first.id), "A2 acquired bubble removed and deduplicated");
        int[] startingSeats = (int[])game.Seats.Clone();
        // A2_R05 only depends on SISY/ECHO adjacency; use normal move operations to change availability.
        string flashBeforeMove=game.FlashClue;
        game.TryPlaceAnimal(3,5);
        Check(game.FlashClue==flashBeforeMove,"Legal seating move does not show a drag toast");
        Check(game.FindSeatExplorations().Any(x => x.id == "A2_R05"), "Unclaimed adjacency bubble appears after move");
        game.TryPlaceAnimal(3,0);
        Check(!game.FindSeatExplorations().Any(x => x.id == "A2_R05"), "Unclaimed bubble disappears when condition is lost");
        Check(game.Records.Count == before + 1 && game.CurrentPhase == Phase.Layout && game.DragAnimal == -1,
            "Move never autoplays/grants A2 and dialogue closing does not start a drag");
        for(int i=1;i<7;i++) game.TryPlaceAnimal(i,startingSeats[i]);
        Check(game.FindSeatExplorations().All(x => string.IsNullOrEmpty(x.requiredEvent)), "Future B2-dependent clues blocked");
        var toolbar = game.transform.Find("D-DAY Canvas/Bottom controls");
        Check(toolbar.GetComponent<HorizontalLayoutGroup>() != null && toolbar.childCount == 4,
            "Bottom four actions in one HorizontalLayoutGroup");
        for (int loop = 1; loop <= 2; loop++)
        {
            for (int day = 1; day <= 6; day++)
            {
                Check(game.Loop == loop && game.Day == day, "Sequential day entered " + loop + "/" + day);
                while (game.CurrentPhase == Phase.Narrative) { game.AdvanceNarrative(); yield return null; }
                Check(!game.SeatsReady, "Unsolved daily start " + loop + "/" + day);
                int[] solution = answers[loop + "/" + day];
                for (int animal = loop == 1 ? 1 : 0; animal < 7; animal++)
                    Check(game.TryPlaceAnimal(animal, solution[animal]), "Actual swap/move " + loop + "/" + day + " " + SeatRules.Ids[animal]);
                Check(game.SeatsReady, "Every employee A1 satisfied " + loop + "/" + day);
                if(loop==1 && day==3) yield return Capture("10_Day3_Office");

                if (loop == 1 && day == 1)
                {
                    Check(game.SaveManualSlot(1, false) || game.SaveManualSlot(1, true), "Manual save created");
                    Check(!game.SaveManualSlot(1, false) && game.SaveManualSlot(1, true), "Manual overwrite requires confirmation");
                    int[] saved = (int[])game.Seats.Clone();
                    game.ReturnToTitle(); game.ContinueGame();
                    Check(game.Seats.SequenceEqual(saved) && game.Records.Count == before + 1 && game.HasUnreadRecords,
                        "Continue restores seats/A2 records/unread");
                }
                game.StartWork();
                Check(game.CurrentPhase == Phase.Layout && game.TasksOpen, "Start Work opens B1 directly with solved A1 and unassigned tasks");
                if(loop==1 && day==1)
                {
                    Check(game.HasUnreadRecords, "Unread persists across manual/autosave reload");
                    game.ToggleTasks(); game.ToggleBoard(first.category=="物品"?0:1);
                    yield return Capture("02d_RecordBook_1280x720");
                    Check(!game.HasUnreadRecords && !game.Records.First(x=>x.id==first.id).unread,"Viewed record clears its unread state");
                    game.ToggleBoard(); game.ReturnToTitle(); game.ContinueGame();
                    Check(!game.HasUnreadRecords,"Read state persists through save/load");
                    game.ToggleBoard(first.category=="物品"?0:1);
                    SetResolution(1600,900); yield return Capture("02e_RecordBook_1600x900");
                    game.ToggleBoard();
                    Check(!badge.gameObject.activeSelf,"All read hides record badge");
                    SetResolution(1920,1080); yield return Capture("01b_Office_1920x1080");
                    game.ToggleTasks();
                }
                Check(game.TasksOpen, "Task panel directly after solved A1");
                foreach (TaskCardData task in game.Tasks.Cards)
                {
                    int owner = loop == 1 && day == 1 ? 0 : 1;
                    game.AssignTask(task.id, owner);
                    Check(game.CurrentPhase == Phase.Narrative && game.Tasks.Owner(task.id) == owner,
                        "B1 assignee feedback opens shared portrait " + loop + "/" + day + " " + task.id);
                    Check(game.CurrentDialogue.speakerId == SeatRules.Ids[owner], "B1 correct speaking character");
                    if (loop == 1 && day == 1) yield return Capture("03_B1_Portrait");
                    while (game.CurrentPhase == Phase.Narrative) { game.AdvanceNarrative(); yield return null; }
                    Check(game.TasksOpen && game.CurrentPhase == Phase.Layout, "B1 returns task panel");
                }
                Check(game.Tasks.Complete, "All tasks assigned " + loop + "/" + day);
                if(loop==1 && day==3) yield return Capture("11_Day3_TaskPanel");
                if (loop == 1 && day == 1)
                {
                    var task = game.Tasks.Cards[0]; game.AssignTask(task.id, 1);
                    Check(game.CurrentDialogue.speakerId == "wolf" && game.CurrentDialogue.expressionId == "nervous",
                        "Reassignment changes portrait/assignee reaction");
                    while (game.CurrentPhase == Phase.Narrative) { game.AdvanceNarrative(); yield return null; }
                    yield return Capture("04_Tasks_Reassigned");
                }
                if (loop == 2 && day == 1)
                {
                    int[] seats = (int[])game.Seats.Clone(); int[] owners = game.Tasks.Cards.Select(x => game.Tasks.Owner(x.id)).ToArray();
                    Check(DDayGame.SisySurvives(seats, owners), "L2 Day1 all three survival conditions true");
                    int[] variant = (int[])seats.Clone(); variant[1] = Enumerable.Range(0,8).First(x => SeatRules.Distance(x, variant[0]) > 1);
                    Check(!DDayGame.SisySurvives(variant, owners), "Survival fails without ORIN adjacency");
                    variant = (int[])seats.Clone(); variant[4] = 1;
                    Check(!DDayGame.SisySurvives(variant, owners), "Survival fails if METIS near coffee");
                    Check(!DDayGame.SisySurvives(seats, new[] {0}), "Survival fails with task assigned to SISY");
                }
                game.ToggleTasks(); game.StartWork();
                Check(game.CurrentPhase == Phase.Work, "Start Work allowed after A1+B1 " + loop + "/" + day);
                while (game.CurrentPhase == Phase.Work || game.CurrentPhase == Phase.Narrative
                    && game.CurrentDialogue.eventId.StartsWith("B1_"))
                {
                    if (game.CurrentPhase == Phase.Narrative)
                    {
                        if(loop==1 && day==2) Check(game.CurrentPagePanel==null,"Day2 B2 not shown during daytime B1 inquiry");
                        game.AdvanceNarrative();
                    }
                    yield return null;
                }
                Check(game.CurrentPhase == Phase.AfterHours, "Automatic after-hours fade; no WorkSummary " + loop + "/" + day);
                Check(game.Tasks.Complete,"B1 assignments retained before B2 "+loop+"/"+day);
                while(game.CurrentPhase==Phase.AfterHours) yield return null;
                var fixedEvent=ABConfig.Current.FixedFor(loop,day);
                if(fixedEvent!=null)
                {
                    Check(game.CurrentComicShot!=null && game.CurrentDialogue.eventId==fixedEvent.id,
                        "B2 comic automatically follows completed B1 "+loop+"/"+day);
                    var seenShots=new HashSet<string>();
                    bool resumed=false;
                    var pagePlayer=game.GetComponent<DDayComicPage>();
                    var pagePresentation=ComicPagePresentation.ForEvent(fixedEvent.id);
                    string[] b1Ids=game.Tasks.Cards.Select(t=>t.id).ToArray();
                    if(loop==1 && (day==4 || day==6))
                    {
                        Check(pagePresentation!=null && pagePresentation.panels.Length==(day==4?7:9),
                            "New actual page panel count Day"+day);
                        var original=DialoguePresentation.Current.Frames(fixedEvent.id,fixedEvent.lines,"",0);
                        foreach(var oldFrame in original.Select((frame,i)=>new {frame,i}))
                        {
                            int mapped=pagePresentation.LegacyPanel(oldFrame.i);
                            Check(mapped>=0 && mapped<pagePresentation.Frames().Length,"Legacy prose checkpoint maps to page Day"+day);
                        }
                        if(day==6)
                        {
                            var kept=original.Where(f=>f.sourceLineIndex!=0 && f.sourceLineIndex!=10 && f.sourceLineIndex!=31);
                            Check(string.Concat(kept.Select(f=>f.text))==string.Concat(pagePresentation.Frames().Skip(1).Select(f=>f.text)),
                                "Every original Day6 dialogue/action preserved in exact order; only section headers omitted");
                        }
                        var pageUi=game.GetComponent<DDayUI>();
                        var captionStyle=new GUIStyle((GUIStyle)typeof(DDayUI).GetField("body",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(pageUi))
                            {fontSize=29,wordWrap=true};
                        foreach(var captionFrame in pagePresentation.Frames())
                        {
                            string text=pagePresentation.panels[captionFrame.sourceLineIndex].sourceLines?.Length>0
                                ? SeatRules.PublicText(captionFrame.text) : captionFrame.text;
                            Check(captionStyle.CalcHeight(new GUIContent(text),1200)<=88,
                                "Every Day"+day+" caption fits full-sized text container "+captionFrame.dialogueId);
                        }
                    }
                    while(game.CurrentPhase==Phase.Narrative && game.CurrentComicShot!=null)
                    {
                        DialogueFrame current=game.CurrentDialogue;
                        ComicShot shot=game.CurrentComicShot;
                        Check(shot.firstLine<=current.sourceLineIndex && shot.lastLine>=current.sourceLineIndex,
                            "Presentation frame mapped to one shot "+current.dialogueId);
                        if(seenShots.Add(shot.shotId))
                        {
                            if(pagePresentation!=null)
                            {
                                if(day==1 && (current.sourceLineIndex==0 || current.sourceLineIndex==2 || current.sourceLineIndex==6))
                                {
                                    while(pagePlayer.PanelIndex!=current.sourceLineIndex) yield return null;
                                    float moment=current.sourceLineIndex==0?.46f:current.sourceLineIndex==2?.10f:.32f;
                                    while(pagePlayer.Elapsed<moment) yield return null;
                                    if(pagePlayer.Elapsed<moment+.12f)
                                    {
                                        string effectFile="Effect_"+shot.effect+".png";
                                        ScreenCapture.CaptureScreenshot(Path.Combine(Output,effectFile));
                                        evidence.Add("SCREENSHOT | "+effectFile+" elapsed="+pagePlayer.Elapsed);
                                        yield return null;
                                    }
                                    else evidence.Add("UNVERIFIED | transient effect screenshot timing missed "+shot.effect);
                                }
                                yield return Capture("B2_Day"+day+"_"+shot.shotId);
                                var page=game.CurrentPagePresentation;
                                Check(game.NarrativeCount==pagePresentation.Frames().Length && pagePlayer.PanelIndex==current.sourceLineIndex,
                                    "One-page sequence index="+current.sourceLineIndex);
                                Texture2D source=Resources.Load<Texture2D>(page.resource);
                                Check(source.width==page.width && source.height==page.height,"Original page native dimensions preserved");
                                Check(Mathf.Abs(pagePlayer.DisplayRect.width/pagePlayer.DisplayRect.height-(float)source.width/source.height)<.0001f,
                                    "Page aspect ratio preserved Day"+day);
                                var panel=game.CurrentPagePanel;
                                Check(panel.outline.Length==4 && panel.focus.width>0,
                                    "Real panel outline/focus loaded "+panel.id);
                                Check(pagePlayer.IsHighlighted(current.sourceLineIndex,panel.focus.Rect.center),
                                    "Current focus is bright "+panel.id);
                                if(day==1 && current.sourceLineIndex==4)
                                    Check(!pagePlayer.IsHighlighted(4,new Vector2(220,740)),"Photo focus keeps drawn wolf dark; no discovery/collection");
                                if(!string.IsNullOrEmpty(panel.sound))
                                    Check(pagePlayer.SoundHistory.Contains(panel.id+":"+panel.sound),"Sound cue triggered "+panel.sound);
                                if(!string.IsNullOrEmpty(panel.ambience))
                                    Check(pagePlayer.AudioActive,"Quiet ambience AudioSource plays "+panel.ambience);
                                Check(!game.Records.Any(r=>r.id=="orin_photo"),"Narrative-only photo does not grant a clue");
                                if(day==2)
                                {
                                    Check(game.Seats[0]<0 && !game.SisyAlive,"SISY absent in real Day2 office throughout comic");
                                    var actors=(GameObject[])typeof(DDayGame).GetField("animalObjects",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(game);
                                    Check(!actors[0].activeSelf,"No SISY world sprite appears during Day2 B2");
                                    Check(b1Ids.SequenceEqual(game.Tasks.Cards.Select(t=>t.id)) && game.Tasks.Complete,
                                        "Day2 announcement leaves assigned Day2 tasks unchanged; no Day3 assignment");
                                    if(current.sourceLineIndex==0)
                                        Check(!pagePlayer.IsHighlighted(0,new Vector2(800,120)),"Day2 mask differs from Day1 full-width top panel");
                                    if(current.sourceLineIndex==2)
                                        Check(panel.effect=="memory" && panel.accent!=null,"SISY image is confined to authored memory highlight");
                                }
                                if(day==4 || day==6)
                                {
                                    Check(!game.SisyAlive && game.Seats[0]<0,"SISY remains absent in L1 Day"+day);
                                    Check(b1Ids.SequenceEqual(game.Tasks.Cards.Select(t=>t.id)) && game.Tasks.Complete,
                                        "Comic does not change daytime B1 tasks Day"+day);
                                }
                            }
                            else if(seenShots.Count==1 || day==6 && (shot.kind=="private" || shot.kind=="suspense"))
                                yield return Capture("B2_L"+loop+"_D"+day+"_"+shot.kind);
                        }
                        if(pagePresentation!=null && current.sourceLineIndex>=2 && !resumed)
                        {
                            string dialogueId=current.dialogueId;int owners=game.Tasks.Count;
                            Check(!game.Records.Any(r=>r.id=="orin_photo"),"Old photo not granted before B2 completion");
                            game.SaveGame();game.ReturnToTitle();game.ContinueGame();
                            Check(game.CurrentDialogue.dialogueId==dialogueId && game.Tasks.Count==owners,
                                "Mid-comic save/Continue restores exact page and B1 owner state Day"+day);
                            resumed=true;yield return null;
                        }
                        if(loop==2 && day==6 && current.sourceLineIndex<8)
                            Check(!game.Records.Any(r=>r.id=="evidence_chain"),"Alliance not recorded at dinner before private meeting");
                        if(pagePresentation!=null)
                        {
                            int panelBefore=game.NarrativeIndex;
                            bool lastFrame=panelBefore==game.NarrativeCount-1;
                            while(Time.frameCount<=game.DialogueInputBlockedUntilFrame) yield return null;
                            if(panelBefore<2)
                            {
                                var viewType=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
                                var view=EditorWindow.GetWindow(viewType);view.Focus();
                                bool mouse=panelBefore==0;
                                view.SendEvent(mouse ? new Event {type=EventType.MouseDown,button=0,mousePosition=new Vector2(400,400)}
                                    : new Event {type=EventType.KeyDown,keyCode=KeyCode.Space});
                                float eventWait=Time.realtimeSinceStartup+.25f;
                                while(game.NarrativeIndex==panelBefore && Time.realtimeSinceStartup<eventWait) yield return null;
                                if(game.NarrativeIndex==panelBefore)
                                {
                                    evidence.Add("UNVERIFIED | GameView.SendEvent "+(mouse?"mouse":"Space")+" not delivered to runtime; shared handler fallback tested");
                                    game.GetComponent<DDayUI>().AdvanceDialogueInput();
                                }
                                else evidence.Add("PASS | Real Game View IMGUI "+(mouse?"mouse click":"Space key")+" event advances one panel");
                            }
                            else game.GetComponent<DDayUI>().AdvanceDialogueInput();
                            yield return null;
                            Check(lastFrame || game.NarrativeIndex==panelBefore+1,"Shared click/space advance handler moves one caption");
                            if(lastFrame)
                            {
                                Check(game.CurrentPhase==Phase.Narrative,"Last panel fades before next day");
                                while(game.Day==day && game.CurrentPhase==Phase.Narrative) yield return null;
                            }
                        }
                        else {game.AdvanceNarrative();yield return null;}
                    }
                    Check(seenShots.Count==(pagePresentation!=null ? pagePresentation.panels.Length
                        : ComicPresentation.Current.shots.Count(s=>s.eventId==fixedEvent.id)),
                        "All shots played in source order "+fixedEvent.id+" count="+seenShots.Count);
                }
                while (game.CurrentPhase == Phase.Narrative) { game.AdvanceNarrative(); yield return null; }
                if (ABConfig.Current.FixedFor(loop, day) != null)
                    Check(game.CompletedFixedEvents.Contains(ABConfig.Current.FixedFor(loop, day).id), "B2 completion state " + loop + "/" + day);
                if (loop == 1 && day == 1)
                {
                    Check(game.Day == 2 && game.Seats[0] == -1 && !game.Seats.Contains(6), "SISY absent and B3 empty after death");
                    Check(!game.Records.Any(r=>r.id=="orin_photo"),"Photo not auto-collected by characters after page completion");
                }
                if(loop==1 && day==2)
                {
                    Check(game.Day==3 && game.CompletedFixedEvents.Contains("B2_L1_D2"),"Day2 comic automatically reaches Day3 once");
                    Check(game.Tasks.Count==0,"Day3 B1 tasks are not preassigned by Day2 comic");
                    game.SaveGame();game.ReturnToTitle();game.ContinueGame();
                    Check(game.Day==3 && game.CurrentPhase==Phase.Layout && game.CurrentComicShot==null
                        && game.CompletedFixedEvents.Contains("B2_L1_D2"),"Completed Day2 does not replay after Continue");
                }
                if(loop==1 && (day==4 || day==6))
                {
                    Check(game.Day==day+1,"Page automatically reaches next day after Day"+day);
                    game.SaveGame();game.ReturnToTitle();game.ContinueGame();
                    Check(game.Day==day+1 && game.CurrentComicShot==null && game.CompletedFixedEvents.Contains("B2_L1_D"+day),
                        "Completed Day"+day+" does not replay after Continue");
                }
            }
            Check(game.Day == 7, "Day7 reached " + loop);
            if (loop == 1)
            {
                Check(game.CurrentPhase == Phase.Suspects, "L1 Day7 choice replaces office");
                game.ChooseSuspect("wolf");
                while (game.CurrentPhase == Phase.Narrative) { game.AdvanceNarrative(); yield return null; }
                while (game.CurrentPhase == Phase.Replay || game.CurrentPhase == Phase.Narrative)
                { if (game.CurrentPhase == Phase.Replay) game.AdvanceReplay(); else game.AdvanceNarrative(); yield return null; }
            }
        }
        Check(game.CurrentPhase == Phase.FinalDecision, "L2 Day7 exact two choices");
        game.SaveGame();
        string savePath = Environment.GetEnvironmentVariable("DDAY_SAVE_OVERRIDE");
        string beforeChoices = File.ReadAllText(savePath);
        game.ChooseFinal(true); game.ChooseFinal(false);
        Check(game.CurrentPhase == Phase.BadEnding, "YES/NO -> BE");
        File.WriteAllText(savePath,beforeChoices); game.ContinueGame(); game.ChooseFinal(false); game.ChooseFinal(true);
        Check(game.CurrentPhase == Phase.BadEnding, "NO/YES -> BE");
        File.WriteAllText(savePath,beforeChoices); game.ContinueGame(); game.ChooseFinal(false); game.ChooseFinal(false);
        Check(game.CurrentPhase == Phase.BadEnding, "NO/NO -> BE");
        File.WriteAllText(savePath,beforeChoices); game.ContinueGame(); game.ChooseFinal(true); game.ChooseFinal(true);
        Check(game.CurrentPhase == Phase.Epilogue, "YES/YES -> HE epilogue");
        while (game.CurrentPhase == Phase.Epilogue) { game.AdvanceEpilogue(); yield return null; }
        Check(game.CurrentPhase == Phase.TrueEnding, "HE completed");
        yield return Capture("06_HE");
        game.Restart(); game.StartGame();
        while(game.CurrentPhase==Phase.Narrative) {game.AdvanceNarrative();yield return null;}
        var longest=ABConfig.Current.clues.OrderByDescending(x=>x.description.Length).First();
        typeof(DDayGame).GetMethod("Record",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,
            new object[]{longest.id,longest.category,longest.title,longest.description,false});
        var other=ABConfig.Current.clues.First(x=>x.category==longest.category && x.id!=longest.id);
        typeof(DDayGame).GetMethod("Record",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,
            new object[]{other.id,other.category,other.title,other.description,false});
        game.ToggleBoard(longest.category=="物品"?0:1);
        yield return Capture("07_RecordBook_LongSourceText");
        Check(game.Records.Single(x=>x.id==longest.id).description==longest.description,"Long record body preserves source words");
        Check(game.HasUnreadRecords && game.Records.Single(x=>x.id==other.id).unread,"Viewing one record leaves other unseen records unread");
        game.ToggleBoard();yield return null;
        game.ToggleBoard(longest.category=="物品"?0:1);yield return Capture("07b_RecordBook_SecondUnread");
        Check(!game.HasUnreadRecords,"All viewed entries clear unread badge");
        game.ToggleBoard();
        game.ToggleSaveSlots(); yield return Capture("08_SaveSlots"); game.ToggleSaveSlots();
        game.GetComponent<DDayUI>().OpenOfficeSettings(); yield return Capture("09_Settings");
        Check(game.GetComponentsInChildren<Text>(true).All(x=>!x.resizeTextForBestFit &&
            Mathf.Abs(x.transform.lossyScale.x-x.transform.lossyScale.y)<.001f),"All HUD text uses fixed font size and uniform scale");
        Check(!evidence.Any(x=>x.StartsWith("TEXT_LAYOUT_FAIL")),"All captured UI text fits its measured containers");
    }

    static int CountSolutions(DayData day, int loop, int number, ref int[] example)
    {
        int[] seats = Enumerable.Repeat(-1,7).ToArray(); bool[] used = new bool[8];
        if (loop == 1) { used[6] = true; if (number == 1) seats[0] = 6; }
        int count = 0; int[] found = null;
        Action<int> visit = null;
        visit = animal => {
            if (animal == 7)
            {
                if (SeatRules.CountSatisfied(day,seats,loop == 1 ? 6 : seats[0]) != day.rules.Length) return;
                count++; if (found == null) found = (int[])seats.Clone(); return;
            }
            for (int seat = 0; seat < 8; seat++)
            {
                if (used[seat]) continue;
                seats[animal]=seat; used[seat]=true; visit(animal+1); used[seat]=false; seats[animal]=-1;
            }
        };
        visit(loop == 1 ? 1 : 0); example = found; return count;
    }

    static void SetResolution(int width,int height)
    {
        requestedWidth=width;requestedHeight=height;
        const BindingFlags flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        var assembly=typeof(EditorWindow).Assembly;
        var viewType=assembly.GetType("UnityEditor.GameView");
        var view=EditorWindow.GetWindow(viewType);
        var selected=viewType.GetProperty("selectedSizeIndex",flags);
        if(!SessionState.GetBool(Key+".SizeStored",false))
        { SessionState.SetInt(Key+".OriginalSize",(int)selected.GetValue(view));SessionState.SetBool(Key+".SizeStored",true); }
        var sizesType=assembly.GetType("UnityEditor.GameViewSizes");
        var singleton=sizesType.BaseType.GetProperty("instance",flags).GetValue(null);
        var group=sizesType.GetProperty("currentGroup",flags).GetValue(singleton);
        var get=group.GetType().GetMethod("GetGameViewSize",flags);
        int count=(int)group.GetType().GetMethod("GetTotalCount",flags).Invoke(group,null);
        int index=-1;
        for(int i=0;i<count;i++)
        {
            var size=get.Invoke(group,new object[]{i});var t=size.GetType();
            if((int)t.GetProperty("width",flags).GetValue(size)==width && (int)t.GetProperty("height",flags).GetValue(size)==height)
            {index=i;break;}
        }
        if(index<0)
        {
            var sizeType=assembly.GetType("UnityEditor.GameViewSize");
            var enumType=assembly.GetType("UnityEditor.GameViewSizeType");
            var constructor=sizeType.GetConstructor(flags,null,new[]{enumType,typeof(int),typeof(int),typeof(string)},null);
            var size=constructor.Invoke(new object[]{Enum.Parse(enumType,"FixedResolution"),width,height,"D-DAY test "+width+"x"+height});
            group.GetType().GetMethod("AddCustomSize",flags).Invoke(group,new[]{size});index=count;
        }
        selected.SetValue(view,index);view.Repaint();
        evidence.Add("GAME_VIEW | "+width+"x"+height);
    }

    static IEnumerator Capture(string name)
    {
        // Keep one live state while rendering the required three 16:9 sizes.
        string[] matrixViews={"01_Day1_Office","02_A2_Portrait","02b_A2_SecondSpeaker",
            "03_B1_Portrait","04_Tasks_Reassigned","05_B2_Portrait",
            "07_RecordBook_LongSourceText","08_SaveSlots","09_Settings",
            "10_Day3_Office","11_Day3_TaskPanel"};
        int originalWidth=requestedWidth,originalHeight=requestedHeight;
        if(matrixViews.Contains(name) || name.StartsWith("B2_Day") || name.StartsWith("Day7_"))
        {
            foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1600,900),new Vector2Int(1280,720)})
            {
                SetResolution(size.x,size.y);
                yield return CaptureFrame(name+"_"+size.x+"x"+size.y);
            }
            SetResolution(originalWidth,originalHeight);
        }
        else yield return CaptureFrame(name);
    }

    static IEnumerator CaptureFrame(string name)
    {
        var ui=UnityEngine.Object.FindFirstObjectByType<DDayUI>();
        if(ui!=null && UnityEngine.Object.FindFirstObjectByType<DDayGame>().CurrentPhase==Phase.Narrative)
            typeof(DDayUI).GetField("revealAll",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(ui,true);
        float until = Time.realtimeSinceStartup + 1.7f;
        float resizeDeadline = until + 4f;
        while (Time.realtimeSinceStartup < until || Screen.width != requestedWidth || Screen.height != requestedHeight)
        {
            if (Time.realtimeSinceStartup >= resizeDeadline) break;
            yield return null;
        }
        Check(Screen.width==requestedWidth && Screen.height==requestedHeight,
            "16:9 live viewport "+name+" actual="+Screen.width+"x"+Screen.height);
        var game=UnityEngine.Object.FindFirstObjectByType<DDayGame>();
        var toolbar=game.transform.Find("D-DAY Canvas/Bottom controls");
        if(toolbar.gameObject.activeInHierarchy)
        {
            var rect=toolbar.GetComponent<RectTransform>();
            Check(Mathf.Abs(rect.anchoredPosition.x)<.01f,"Centered bottom layout group "+name);
            float previousRight=0,previousGap=-1;int index=0;
            foreach(RectTransform child in toolbar)
            {
                Check(Mathf.Abs(child.rect.height-60)<.1f,"Uniform bottom button height "+name);
                if(index++>0)
                {
                    float gap=child.anchoredPosition.x+child.rect.xMin-previousRight;
                    Check(previousGap<0 || Mathf.Abs(gap-previousGap)<.1f,"Uniform bottom button gaps "+name);
                    previousGap=gap;
                }
                previousRight=child.anchoredPosition.x+child.rect.xMax;
            }
        }
        if(ui==null || ui.TextLayoutIssues.Count>0)
            evidence.Add("TEXT_LAYOUT_FAIL | "+name+" | "+(ui==null?"UI missing":string.Join(" | ",ui.TextLayoutIssues)));
        else evidence.Add("PASS | Rendered text fits containers: "+name);
        ScreenCapture.CaptureScreenshot(Path.Combine(Output, name + ".png"));
        evidence.Add("SCREENSHOT | " + name);
        yield return null;
    }

    static void Finish(bool pass, string reason)
    {
        EditorApplication.update -= Tick; SessionState.SetBool(Key,false);
        File.WriteAllText(Path.Combine(Output,"Results.md"), (SessionState.GetBool(Key + ".Day7", false)
            ? "# Day7 suspect selection Play verification\n\n" : SessionState.GetBool(Key + ".Latest", false)
            ? "# Latest workbook content Play verification\n\n" : "# Day4/Day6 pages / Day1, Day2 and AB Play regression\n\n")
            + DateTime.Now.ToString("s") + "\n\n" + (pass ? "PASS" : "FAIL") + "\n\n" + reason
            + "\n\n" + string.Join("\n",evidence) + (SessionState.GetBool(Key + ".Day7", false)
            ? "\n\nDay7 native mouse input was observed in the live Game View after automated state/save regression.\n"
            : "\n\nReal hardware mouse/keyboard input was not injected; callbacks and runtime methods were tested.\n"));
        var testGame = UnityEngine.Object.FindFirstObjectByType<DDayGame>();
        if (testGame != null) testGame.ReturnToTitle();
        Environment.SetEnvironmentVariable("DDAY_SAVE_OVERRIDE",SessionState.GetString(Key+".SaveOverride", ""));
        if(SessionState.GetBool(Key+".SizeStored",false))
        {
            var viewType=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            viewType.GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public)
                .SetValue(EditorWindow.GetWindow(viewType),SessionState.GetInt(Key+".OriginalSize",0));
            SessionState.SetBool(Key+".SizeStored",false);
        }
        run=null; iterators.Clear();
        if (pass) Debug.Log("DDAY_AB_PORTRAIT_VERIFICATION_PASS");
        else if (reason.StartsWith("INTERRUPTED")) Debug.Log("DDAY_VERIFICATION_INTERRUPTED: " + reason);
        else Debug.LogError("DDAY_AB_PORTRAIT_VERIFICATION_FAIL: " + reason);
        EditorApplication.isPlaying=false;
    }
}
