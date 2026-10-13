using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using DDay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Runs the entire campaign state machine in Play Mode without clicking IMGUI.
// Use a separate project copy and DDAY_SAVE_OVERRIDE for automated runs.
[InitializeOnLoad]
public static class DDayCampaignSmoke
{
    const string ActiveKey = "DDayCampaignSmoke.Active";
    static readonly HashSet<string> visited = new HashSet<string>();
    static DateTime startedAt;
    static bool loadedSave;
    static bool trueEndingSeen;
    static bool enteredGame;
    static bool checkedInvestigationSave;
    static bool checkedFinalChoiceSave;
    static bool checkedAllAnimalMoves;
    static bool checkedManualSlots;
    static bool checkedSettings;
    static readonly List<string> day1WorkOrder = new List<string>();
    static readonly Dictionary<string, int[]> seatSolutions = new Dictionary<string, int[]>();

    static DDayCampaignSmoke()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        startedAt = DateTime.UtcNow;
        EditorApplication.update += Tick;
    }

    [MenuItem("D-DAY/Run Dual Loop Campaign Smoke")]
    public static void Run()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        if (!Path.GetFileName(projectRoot).Equals("_DDayValidationProject", StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogError("D-DAY smoke test must run from the isolated validation project, not the live game project.");
            return;
        }
        Environment.SetEnvironmentVariable("DDAY_SAVE_OVERRIDE",
            Path.Combine(projectRoot, "DDaySmokeSave.json"));
        CheckAllDaysHaveSolution();
        EditorSceneManager.OpenScene("Assets/Scenes/TodayWhereToSit.unity");
        SessionState.SetBool(ActiveKey, true);
        startedAt = DateTime.UtcNow;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.isPlaying = true;
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying) return;
        if ((DateTime.UtcNow - startedAt).TotalSeconds > 120)
        {
            Finish(false, "Timed out before finishing both playthroughs.");
            return;
        }
        try
        {
            var game = UnityEngine.Object.FindFirstObjectByType<DDayGame>();
            if (game == null || game.Data == null) return;
            if (seatSolutions.Count == 0) CheckAllDaysHaveSolution();
            switch (game.CurrentPhase)
            {
                case Phase.Title:
                    if (!enteredGame) { enteredGame = true; game.StartGame(); }
                    break;
                case Phase.Narrative:
                    if (game.Loop == 1 && game.Day == 1
                        && game.NarrativeTitle.StartsWith("工作结束"))
                        day1WorkOrder.Add("task");
                    if (game.NarrativeTitle.StartsWith("工作结束"))
                    {
                        TaskCardData task = game.Tasks.Cards.FirstOrDefault(card =>
                            game.NarrativeTitle.Contains(card.title));
                        Require(task != null, "Work-end narrative did not identify its task.");
                        int owner = game.Tasks.Owner(task.id);
                        Require(game.NarrativeTitle.Contains(SeatRules.Names[owner])
                            && game.NarrativeLine == CampaignSchedule.TaskResponse(
                                game.Loop, game.Day, task.id, owner),
                            "Work-end narrative did not match the assigned owner.");
                    }
                    game.AdvanceNarrative(); break;
                case Phase.Layout: ArrangeAndWork(game); break;
                case Phase.Work: break;
                case Phase.WorkSummary:
                    if (game.Loop == 1 && game.Day == 1)
                    {
                        Require(game.Records.Any(r => r.id == "orin_reaction"),
                            "Adjacent ORIN/SISY seats did not reveal the expected reaction.");
                        Require(day1WorkOrder.Count > 0 && day1WorkOrder.All(stage => stage == "task"),
                            "Only task-owner stories should play after Start Work.");
                    }
                    if (game.Loop == 2 && game.Day == 2)
                        Require(game.XInfoToMetis, "CASSIA-to-METIS seat information did not flow.");
                    visited.Add(game.Loop + "/" + game.Day);
                    game.CompleteWorkSummary();
                    break;
                case Phase.Dinner:
                    game.SaveGame(); game.ReturnToTitle(); game.ContinueGame();
                    Require(game.CurrentPhase == Phase.Dinner,
                        "Day 6 dinner scene did not resume from save.");
                    game.CompleteDinner();
                    break;
                case Phase.Fatigue:
                    game.SelectAnimal(0);
                    game.ContinueFatigue();
                    break;
                case Phase.Death:
                    if (game.DeathStage == 3) game.ContinueDeath();
                    break;
                case Phase.Suspects:
                    foreach (string id in new[] { "fox", "wolf", "shiba", "cat", "bear", "panda" })
                        if (!game.IsExcluded(id)) { game.ChooseSuspect(id); break; }
                    break;
                case Phase.SuspectResult:
                    game.ExcludeSuspect();
                    if (!checkedInvestigationSave)
                    {
                        game.ReturnToTitle(); game.ContinueGame();
                        Require(game.IsExcluded("fox") && game.CurrentPhase == Phase.Suspects,
                            "Day 7 investigation did not resume after saving.");
                        checkedInvestigationSave = true;
                    }
                    break;
                case Phase.Everyone: game.ChooseEveryone(); break;
                case Phase.EveryoneResult: game.ChooseNoCulprit(); break;
                case Phase.Replay: game.AdvanceReplay(); break;
                case Phase.RescueApproval: game.ApproveRabbitLeave(true); break;
                case Phase.RescueFailure: throw new Exception("Unexpected failure: " + game.RescueFailureText);
                case Phase.FinalDecision:
                    if (game.FinalQuestion == 1 && !checkedFinalChoiceSave)
                    {
                        game.ReturnToTitle(); game.ContinueGame();
                        Require(game.CurrentPhase == Phase.FinalDecision && game.FinalQuestion == 1
                            && game.YesCount == 1, "Day 7 first final answer did not resume correctly.");
                        checkedFinalChoiceSave = true;
                    }
                    game.ChooseFinal(!trueEndingSeen || game.FinalQuestion == 1);
                    break;
                case Phase.Epilogue: game.AdvanceEpilogue(); break;
                case Phase.TrueEnding:
                    if (trueEndingSeen) break;
                    trueEndingSeen = true;
                    Require(visited.Count == 12, "Expected all 12 working days; found " + visited.Count);
                    typeof(DDayGame).GetMethod("BeginDay", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(game, new object[] { 7 });
                    break;
                case Phase.BadEnding:
                    Require(trueEndingSeen, "Bad ending arrived before true-ending branch test.");
                    Finish(true, "Both seven-day loops, save/load, rescue, true ending, and bad ending passed.");
                    break;
            }
        }
        catch (Exception ex) { Finish(false, ex.ToString()); }
    }

    static void ArrangeAndWork(DDayGame game)
    {
        if (!checkedAllAnimalMoves && game.Loop == 1 && game.Day == 1)
        {
            int[] original = (int[])game.Seats.Clone();
            var hitTest = typeof(DDayGame).GetMethod("FindAnimalAt", BindingFlags.Instance | BindingFlags.NonPublic);
            var resetSeats = typeof(DDayGame).GetMethod("SetSeating", BindingFlags.Instance | BindingFlags.NonPublic);
            Require(game.TryPlaceAnimal(1, 7), "Could not move ORIN away for optional discovery test.");
            Require(!game.FindSeatExplorations().Any(ev => ev.id == "l1_orin_cares")
                && !game.Records.Any(r => r.id == "orin_reaction"),
                "Moving ORIN away must not unlock the adjacent discovery.");
            Require(game.TryPlaceAnimal(1, 1), "Could not return ORIN beside SISY.");
            Require(game.Records.Any(r => r.id == "orin_reaction")
                && game.TriggeredSeatEvents.Contains("l1_orin_cares"),
                "Returning ORIN beside SISY must immediately unlock the discovery.");
            for (int animal = 0; animal < 7; animal++)
            {
                Require((int)hitTest.Invoke(game, new object[] { game.AnimalWorldPoint(animal) }) == animal,
                    "Character hitbox did not match visible animal " + animal);
                int emptySeat = Enumerable.Range(0, 8).First(s => !game.Seats.Contains(s));
                Require(game.TryPlaceAnimal(animal, emptySeat), "Could not move animal " + animal);
            }
            resetSeats.Invoke(game, new object[] { original });
            Require(game.Seats.SequenceEqual(original), "Seat reset after move test failed.");
            checkedAllAnimalMoves = true;
        }
        if (!checkedManualSlots && game.Loop == 1 && game.Day == 1)
        {
            int slot = DDayGame.ManualSlotCount;
            ManualSlotInfo existing = game.GetManualSlotInfo(slot);
            string autoBefore = File.ReadAllText(Environment.GetEnvironmentVariable("DDAY_SAVE_OVERRIDE"));
            Require(game.SaveManualSlot(slot, existing.exists), "Manual slot 15 could not be written.");
            Require(!game.SaveManualSlot(slot, false), "Occupied manual slot allowed silent overwrite.");
            Require(game.SaveManualSlot(slot, true), "Confirmed overwrite failed.");
            Require(game.GetManualSlotInfo(slot).day == 1, "Manual slot metadata has wrong day.");
            Require(File.ReadAllText(Environment.GetEnvironmentVariable("DDAY_SAVE_OVERRIDE")) == autoBefore,
                "Manual slot write modified the automatic progress file.");
            checkedManualSlots = true;
        }
        if (!checkedSettings && game.Loop == 1 && game.Day == 1)
        {
            int volume = DDaySettings.BgmPercent, speed = DDaySettings.TextSpeed;
            bool fullscreen = DDaySettings.Fullscreen;
            DDaySettings.SetBgmPercent(35);
            DDaySettings.SetFullscreen(false);
            DDaySettings.SetTextSpeed(2);
            DDaySettings.Load();
            Require(DDaySettings.BgmPercent == 35 && !DDaySettings.Fullscreen
                && DDaySettings.TextSpeed == 2 && DDaySettings.CharactersPerSecond == 72,
                "Settings were not persisted or applied.");
            DDaySettings.SetBgmPercent(volume);
            DDaySettings.SetFullscreen(fullscreen);
            DDaySettings.SetTextSpeed(speed);
            checkedSettings = true;
        }
        if (game.Loop == 1 && game.Day == 2)
            Require(game.Records.Any(r => r.id == "orin_photo" && r.day == 1),
                "The Day 1 death scene did not reveal ORIN's photograph.");
        if (!loadedSave && game.Loop == 1 && game.Day == 3)
        {
            game.SaveGame(); game.ReturnToTitle(); game.ContinueGame();
            Require(game.Loop == 1 && game.Day == 3 && game.CurrentPhase == Phase.Layout,
                "Save/load did not restore Day 3 layout.");
            loadedSave = true;
        }
        if (game.Loop == 2 && game.Day == 1)
        {
            Require(game.TaskHistory.Any(entry => entry.loop == 1 && entry.day == 1
                && entry.taskId == "report"),
                "First-loop task owner history was not retained for the second loop.");
            Require(game.TryPlaceAnimal(0, 4), "Could not move SISY to an edge seat.");
            Require(game.TryPlaceAnimal(1, 5), "Could not move ORIN next to SISY.");
        }
        if (game.Loop == 2 && game.Day == 2)
        {
            Require(game.TryPlaceAnimal(1, 7), "Could not change Day 2 layout for seat exploration.");
            Require(game.XInfoToMetis && game.Records.Any(r => r.id == "x_handoff"),
                "CASSIA/METIS information must unlock as soon as the layout changes.");
        }
        int[] solution = seatSolutions[game.Loop + "/" + game.Day];
        typeof(DDayGame).GetMethod("SetSeating", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(game, new object[] { solution });
        int movingAnimal = Enumerable.Range(0, 7).First(i => game.Seats[i] >= 0);
        int empty = Enumerable.Range(0, 8).First(seat => !game.Seats.Contains(seat)
            && !(game.Loop == 1 && game.Day >= 2 && seat == game.RabbitSeat));
        Require(game.TryPlaceAnimal(movingAnimal, empty), "Could not move into a spare desk.");
        Require(game.TryPlaceAnimal(movingAnimal, solution[movingAnimal]),
            "Could not return to the solved layout.");
        Require(game.SeatsReady, "Seat puzzle was not solved on " + game.Loop + "/" + game.Day);
        if (game.Tasks.Cards.Length > 0)
        {
            game.StartWork();
            Require(game.CurrentPhase == Phase.Layout,
                "Work began before all task owners were selected.");
        }
        game.ToggleTasks();
        foreach (TaskCardData task in game.Tasks.Cards)
        {
            if (task.id == "orin_interview") continue;
            int owner = CampaignSchedule.KeyOwner(game.Loop, game.Day, task.id);
            Require(owner >= 0, "Missing key owner for " + task.id);
            int other = owner == 1 ? 2 : 1;
            Require(CampaignSchedule.TaskResponse(game.Loop, game.Day, task.id, owner)
                != CampaignSchedule.TaskResponse(game.Loop, game.Day, task.id, other),
                "Task response did not change by owner: " + task.id);
            game.AssignTask(task.id, owner);
        }
        game.ToggleTasks();
        Require(game.Tasks.Complete, "Task assignment incomplete on " + game.Loop + "/" + game.Day);
        if (game.Loop == 1 && game.Day == 1)
        {
            string taskId = game.Tasks.Cards[0].id;
            int originalOwner = game.Tasks.Owner(taskId);
            game.ToggleTasks();
            game.AssignTask(taskId, 1);
            Require(game.Tasks.Owner(taskId) == 1 && game.CurrentPhase == Phase.Layout,
                "Changing a task owner played a story or failed to update.");
            game.AssignTask(taskId, originalOwner);
            game.ToggleTasks();
            game.SaveGame(); game.ReturnToTitle(); game.ContinueGame();
            Require(game.Tasks.Owner(taskId) == originalOwner && game.CurrentPhase == Phase.Layout,
                "Assigned task owner did not survive automatic save/load.");
            int oldSeat = game.Seats[movingAnimal];
            int spareSeat = Enumerable.Range(0, 8).First(seat => !game.Seats.Contains(seat));
            Require(game.TryPlaceAnimal(movingAnimal, spareSeat), "Could not perturb solved seats.");
            Require(!game.SeatsReady, "Seat gate test did not break a rule.");
            game.StartWork();
            Require(game.CurrentPhase == Phase.Layout,
                "Work began despite an unsatisfied seat rule.");
            Require(game.TryPlaceAnimal(movingAnimal, oldSeat) && game.SeatsReady,
                "Seat puzzle did not recover after gate test.");
        }
        if (game.Loop == 2 && game.Day == 6)
            Require(game.SisyTrustsMetis, "Day 6 SISY/METIS seat handoff did not occur.");
        Require(game.CurrentPhase == Phase.Layout,
            "Task assignment played a story before Start Work.");
        game.StartWork();
        Require(game.CurrentPhase == Phase.Work
            || (game.Tasks.Cards.Length == 0 && game.CurrentPhase == Phase.WorkSummary),
            "Start Work did not enter the work sequence.");
    }

    static void Require(bool pass, string message)
    {
        if (!pass) throw new InvalidOperationException(message);
    }

    static void CheckAllDaysHaveSolution()
    {
        seatSolutions.Clear();
        var content = Resources.Load<TextAsset>("DDay/Content");
        Require(content != null, "D-DAY content resource missing.");
        var data = JsonUtility.FromJson<Database>(content.text);
        for (int loop = 1; loop <= 2; loop++)
        {
            var days = CampaignSchedule.MakeDays(data, loop);
            for (int day = 1; day <= 6; day++)
            {
                var seats = new int[7];
                for (int i = 0; i < seats.Length; i++) seats[i] = -1;
                var used = new bool[8];
                if (loop == 1 && day >= 2) used[0] = true; // SISY's former seat remains empty.
                Require(FindLayout(days[day - 1], seats, used, loop == 1 && day >= 2 ? 1 : 0),
                    "No complete seat-rule solution for loop " + loop + " day " + day);
                seatSolutions[loop + "/" + day] = (int[])seats.Clone();
                Debug.Log("DDAY_RULE_SOLUTION " + loop + "/" + day + " "
                    + days[day - 1].rules.Length + " rules; seats=" + string.Join(",", seats));
            }
        }
    }

    static bool FindLayout(DayData day, int[] seats, bool[] used, int animal)
    {
        if (animal == seats.Length)
            return SeatRules.CountSatisfied(day, seats, 0) == day.rules.Length;
        for (int seat = 0; seat < used.Length; seat++)
        {
            if (used[seat]) continue;
            used[seat] = true;
            seats[animal] = seat;
            if (FindLayout(day, seats, used, animal + 1)) return true;
            used[seat] = false;
            seats[animal] = -1;
        }
        return false;
    }

    static void Finish(bool pass, string message)
    {
        EditorApplication.update -= Tick;
        SessionState.SetBool(ActiveKey, false);
        if (pass) Debug.Log("DDAY_CAMPAIGN_SMOKE_PASS: " + message);
        else Debug.LogError("DDAY_CAMPAIGN_SMOKE_FAIL: " + message);
        EditorApplication.Exit(pass ? 0 : 1);
    }
}
