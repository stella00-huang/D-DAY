using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace DDay
{
    [Serializable]
    public sealed class CampaignRecord
    {
        public string id;
        public string category;
        public string title;
        public string description;
        public int loop;
        public int day;
        public bool unread;
    }

    [Serializable]
    public sealed class TaskHistoryEntry
    {
        public int loop;
        public int day;
        public string taskId;
        public int owner;
    }

    [Serializable]
    sealed class CampaignSave
    {
        public int loop;
        public int day;
        public int[] seats;
        public string[] taskIds;
        public int[] taskOwners;
        public CampaignRecord[] records;
        public TaskHistoryEntry[] taskHistory;
        public bool sisyAlive;
        public bool xInfoToMetis;
        public bool sisyTrustsMetis;
        public int yesCount;
        public int finalQuestion;
        public int epilogueIndex;
        public float heElapsed;
        public float beElapsed;
        public bool beCompleted;
        public string endingPresentationId;
        public int replayIndex;
        public string phase;
        public string[] excluded;
        public string[] triggeredSeatEvents;
        public string[] completedFixedEvents;
        public string fixedEventId;
        public int fixedDialogueIndex;
        public string fixedPresentationId;
        public string suspectId;
        public string selectedSuspectId;
        public int day7DialogueIndex;
        public DDayTutorialState tutorial;
    }

    public sealed class ManualSlotInfo
    {
        public int slot;
        public bool exists;
        public int loop;
        public int day;
        public string savedAt;
    }

    // Authored schedule follows the supplied two-playthrough design. The existing seat
    // rules and visual assets remain the source for layout and presentation.
    public static class CampaignSchedule
    {
        public static DayData[] MakeDays(Database source, int loop)
        {
            ABConfig config = ABConfig.Current;
            var result = new DayData[6];
            for (int day = 1; day <= 6; day++)
            {
                ABSeatDay seating = config.SeatDay(loop, day);
                if (seating == null) throw new InvalidOperationException(
                    "D-DAY: missing A1 for loop " + loop + " day " + day);
                ABTask[] assigned = config.TasksFor(loop, day);
                result[day - 1] = new DayData {
                    day = day,
                    title = day == 6 ? "9.12 · 聚餐前收尾" : day == 1 ? "9.5 · 九月计划"
                        : day == 2 ? "9.8 · 工作交接" : day == 3 ? "9.9 · 职责重排"
                        : day == 4 ? "9.10 · 晋升名单" : "9.11 · 九月报告",
                    rules = seating.rules,
                    tasks = assigned.Select(x => new TaskCardData {
                        id = x.id, title = x.title, description = x.description }).ToArray(),
                    intro = Array.Empty<string>(),
                    outro = Array.Empty<string>(), guaranteedClues = Array.Empty<string>()
                };
            }
            return result;
        }

        public static int KeyOwner(int loop, int day, string task)
        {
            switch (task)
            {
                case "report": return loop == 1 ? 0 : 3;
                case "legacy": case "unfinished": case "backlog": return 6;
                case "coordination": return 1;
                case "hr": case "recruitment": return 3;
                case "archive": return loop == 1 ? 6 : 4;
                case "promotion": case "history": return 2;
                case "belongings": return 2;
                case "revised_report": return 6;
                default: return -1;
            }
        }

        public static ABTaskBranch ResponseBranch(int loop, int day, string task, int owner, bool sisyAssigned = false)
        {
            ABTask entry = ABConfig.Current.TaskFor(loop, day, task);
            if (entry == null || owner < 0 || owner >= SeatRules.Ids.Length) return null;
            string actor = SeatRules.Ids[owner];
            return entry.branches?.FirstOrDefault(x => x.owner == actor &&
                (string.IsNullOrEmpty(x.condition) || x.condition == (sisyAssigned ? "sisy_assigned" : "sisy_unassigned")));
        }

        public static string TaskResponse(int loop, int day, string task, int owner, bool sisyAssigned = false)
        {
            ABTask entry = ABConfig.Current.TaskFor(loop, day, task);
            // No handcrafted fallback plot: only the workbook branch or the existing brief acknowledgement.
            return ResponseBranch(loop, day, task, owner, sisyAssigned)?.text
                ?? entry?.generic ?? "已完成分配的工作。";
        }
    }

    public sealed partial class DDayGame
    {
        public DDayTutorialState TutorialState { get; private set; } = new DDayTutorialState();
        public bool TutorialLoading { get; private set; }
        public event Action<DDayTutorialSignal, int, string> TutorialSignalReceived;
        void RaiseTutorialSignal(DDayTutorialSignal signal, int value = -1, string id = null)
            => TutorialSignalReceived?.Invoke(signal, value, id);
        public void BlockDialogueInputForTutorial()
            => DialogueInputBlockedUntilFrame = Time.frameCount + 2;
        public Vector3 TutorialSeatWorldPoint(int seat) => characterAnchors[seat].position;
        public void ShowTutorialHint(string text) => SetFlash(text, 1.8f);
        DayData[] firstDays, secondDays;
        readonly List<CampaignRecord> records = new List<CampaignRecord>();
        public IReadOnlyList<CampaignRecord> Records => records;
        readonly List<TaskHistoryEntry> taskHistory = new List<TaskHistoryEntry>();
        readonly HashSet<string> completedFixedEvents = new HashSet<string>();
        public IReadOnlyCollection<string> CompletedFixedEvents => completedFixedEvents;
        public IReadOnlyList<TaskHistoryEntry> TaskHistory => taskHistory;
        public bool SisyAlive { get; private set; }
        public bool XInfoToMetis { get; private set; }
        public bool SisyTrustsMetis { get; private set; }
        public int YesCount { get; private set; }
        public int FinalQuestion { get; private set; }
        public int EpilogueIndex { get; private set; }
        public int WorkActor { get; private set; } = -1;
        public string WorkLabel { get; private set; }
        string checkedSavePath;
        DateTime checkedSaveTime;
        long checkedSaveLength = -1;
        bool checkedSaveValid;
        public bool CanContinue
        {
            get
            {
                try
                {
                    var file = new FileInfo(SavePath);
                    if (!file.Exists) { checkedSaveLength = -1; return false; }
                    if (file.FullName != checkedSavePath || file.LastWriteTimeUtc != checkedSaveTime
                        || file.Length != checkedSaveLength)
                    {
                        checkedSaveValid = TryReadContinueSave(out _);
                        checkedSavePath = file.FullName; checkedSaveTime = file.LastWriteTimeUtc;
                        checkedSaveLength = file.Length;
                    }
                    return checkedSaveValid;
                }
                catch { return false; }
            }
        }
        static bool TryReadContinueSave(out CampaignSave save)
        {
            save = null;
            try
            {
                if (!File.Exists(SavePath)) return false;
                save = JsonUtility.FromJson<CampaignSave>(File.ReadAllText(SavePath));
                return save != null && save.loop >= 1 && save.loop <= 2 && save.day >= 1 && save.day <= 7;
            }
            catch { return false; }
        }
        public const int ManualSlotCount = 15;
        public bool SaveSlotsOpen { get; private set; }
        static string SavePath => string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DDAY_SAVE_OVERRIDE"))
            ? Path.Combine(Application.persistentDataPath, "DDayCampaign.json")
            : Environment.GetEnvironmentVariable("DDAY_SAVE_OVERRIDE");
        static string ManualSlotPath(int slot) => Path.Combine(Path.GetDirectoryName(SavePath),
            "DDayManualSlot" + slot.ToString("D2") + ".json");

        public void ToggleSaveSlots()
        {
            if (CurrentPhase != Phase.Layout) return;
            BoardOpen = false; TasksOpen = false;
            SaveSlotsOpen = !SaveSlotsOpen;
        }

        public ManualSlotInfo GetManualSlotInfo(int slot)
        {
            if (slot < 1 || slot > ManualSlotCount) throw new ArgumentOutOfRangeException(nameof(slot));
            string path = ManualSlotPath(slot);
            var info = new ManualSlotInfo { slot = slot, exists = File.Exists(path) };
            if (!info.exists) return info;
            try
            {
                CampaignSave save = JsonUtility.FromJson<CampaignSave>(File.ReadAllText(path));
                if (save != null) { info.loop = save.loop; info.day = save.day; }
                info.savedAt = File.GetLastWriteTime(path).ToString("yyyy-MM-dd HH:mm");
            }
            catch (Exception e) { info.savedAt = "无法读取：" + e.Message; }
            return info;
        }

        public bool SaveManualSlot(int slot, bool overwriteConfirmed)
        {
            if (CurrentPhase != Phase.Layout || slot < 1 || slot > ManualSlotCount) return false;
            string path = ManualSlotPath(slot);
            if (File.Exists(path) && !overwriteConfirmed) return false;
            string temporary = path + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(temporary, JsonUtility.ToJson(BuildSave()));
                if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
                else File.Move(temporary, path);
                SetFlash("已保存到手动存档 " + slot.ToString("D2"), 2.5f);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("D-DAY manual save failed: " + e.Message);
                return false;
            }
        }

        void Record(string id, string category, string title, string description, bool newLoopClaim = false)
        {
            CampaignRecord entry = records.FirstOrDefault(r => r.id == id);
            if (entry == null)
            {
                records.Add(new CampaignRecord { id = id, category = category, title = title,
                    description = description, loop = Loop, day = Day, unread = true });
                SetFlash("记录册已更新", 2f);
                DDayAudioManager.Play(SfxCue.Clue);
                RaiseTutorialSignal(DDayTutorialSignal.InfoObtained, -1, id);
            }
            else if (entry.description != description || newLoopClaim && entry.loop != Loop)
            {
                entry.description = description; entry.title = title;
                entry.loop = Loop; entry.day = Day;
                entry.unread = true;
                SetFlash("记录册已更新", 2f);
            }
        }

        public bool HasUnreadRecords => records.Any(entry => entry.unread);

        public void MarkRecordRead(string id)
        {
            CampaignRecord entry = records.FirstOrDefault(item => item.id == id);
            if (entry == null || !entry.unread) return;
            entry.unread = false;
            SaveGame();
        }

        // The record book spans both loops. Count unique attainable IDs, never event repeats.
        public int RecordTotal(string category)
        {
            var ids = new HashSet<string>();
            foreach (ABClue entry in ABConfig.Current.clues)
                if (entry.category == category) ids.Add(entry.id);
            string[] scripted = category == "物品"
                ? new[] { "x_anomaly", "orin_photo" }
                : category == "情报"
                    ? new[] { "evidence_chain", "thane_order" } : Array.Empty<string>();
            foreach (string id in scripted) ids.Add(id);
            foreach (CampaignRecord entry in records)
                if (entry.category == category) ids.Add(entry.id);
            return ids.Count;
        }

        void RunCampaignWork()
        {
            if ((CurrentPhase != Phase.Layout && CurrentPhase != Phase.SecondLoop)
                || !CanStartWork) return;
            foreach (TaskCardData card in Tasks.Cards)
            {
                int owner = Tasks.Owner(card.id);
                if (!taskHistory.Any(item => item.loop == Loop && item.day == Day
                    && item.taskId == card.id && item.owner == owner))
                    taskHistory.Add(new TaskHistoryEntry { loop = Loop, day = Day,
                        taskId = card.id, owner = owner });
            }
            TasksOpen = false; BoardOpen = false; CurrentPhase = Phase.Work;
            RaiseTutorialSignal(DDayTutorialSignal.WorkStarted);
            StartCoroutine(WorkRoutine());
        }

        System.Collections.IEnumerator WorkRoutine()
        {
            foreach (TaskCardData task in Tasks.Cards)
            {
                int owner = Tasks.Owner(task.id);
                WorkActor = owner;
                WorkLabel = SeatRules.DisplayName(SeatRules.Ids[owner]) + " · " + task.title;
                yield return AnimateWorkActor(owner);
            }
            WorkActor = -1;
            foreach (TaskCardData task in Tasks.Cards)
                ApplyTaskConsequence(task, Tasks.Owner(task.id));
            if (Loop == 1 && Day == 2)
            {
                ABTask inquiry = ABConfig.Current.TaskFor(1, 2, "orin_inquiry");
                bool finished = false;
                ShowNarrative("与灰狼 ORIN 了解情况",
                    DialoguePresentation.SplitTurns(inquiry.branches.First(x => x.owner == "wolf").text), () => finished = true, "B1_L1_D2_orin_inquiry_wolf", "wolf");
                yield return new WaitUntil(() => finished);
                CurrentPhase = Phase.Work;
            }
            RaiseTutorialSignal(DDayTutorialSignal.WorkFinished);
            yield return AfterWorkTransition();
        }

        public float AfterHoursAlpha { get; private set; }
        public string AfterHoursCaption => Day == 1 ? "18:15" : Day == 6 ? "夜晚" : "下班后";

        System.Collections.IEnumerator AfterWorkTransition()
        {
            CurrentPhase = Phase.AfterHours;
            AfterHoursAlpha = 0;
            SaveGame();
            for (float time = 0; time < .72f; time += Time.unscaledDeltaTime)
            {
                AfterHoursAlpha = Mathf.SmoothStep(0, 1, time / .72f);
                yield return null;
            }
            AfterHoursAlpha = 1;
            yield return new WaitForSecondsRealtime(.18f);
            BeginAfterHoursStory();
        }

        System.Collections.IEnumerator AnimateWorkActor(int owner)
        {
            if (owner < 0 || owner >= animalObjects.Length || animalObjects[owner] == null)
                yield break;
            Transform actor = animalObjects[owner].transform;
            Vector3 home = actor.localPosition;
            for (float t = 0; t < .48f; t += Time.unscaledDeltaTime)
            {
                actor.localPosition = home + Vector3.up * (.10f * Mathf.Sin(t / .48f * Mathf.PI));
                yield return null;
            }
            actor.localPosition = home;
        }

        void ApplyTaskConsequence(TaskCardData task, int owner)
        {
            if ((task.id == "legacy" || task.id == "unfinished" || task.id == "backlog") && owner == 6)
                Record("x_anomaly", "物品", "X 项目工作记录",
                    CampaignSchedule.TaskResponse(Loop, Day, task.id, owner));
            if (Loop == 2 && task.id == "archive" && (owner == 4 || owner == 6)) XInfoToMetis = true;
            SaveGame();
        }

        string[] FixedEventLines()
        {
            ABFixedEvent authored = ABConfig.Current.FixedFor(Loop, Day);
            return authored?.lines ?? Array.Empty<string>();
        }

        public void CompleteWorkSummary()
        {
            // Compatibility for old callers/saves; this phase no longer has a UI page.
            if (CurrentPhase != Phase.WorkSummary) return;
            StartCoroutine(AfterWorkTransition());
        }

        void BeginAfterHoursStory()
        {
            if (Loop == 2 && Day == 1)
            {
                bool protectedSisy = SisySurvives(Seats,
                    Tasks.Cards.Select(card => Tasks.Owner(card.id)).ToArray());
                if (!protectedSisy)
                {
                    RescueFailureText = "须同时满足：ORIN 与 SISY 相邻；METIS 远离咖啡机；不要把 B1 工作交给 SISY。";
                    CurrentPhase = Phase.RescueFailure;
                    return;
                }
            }
            string[] fixedLines = FixedEventLines();
            if (Day == 6) { CurrentPhase = Phase.Dinner; CompleteDinner(); return; }
            if (fixedLines.Length > 0)
                ShowNarrative(Day == 1 ? "18:15 · " + ABConfig.Current.FixedFor(Loop, Day).title
                    : "DAY " + Day + " · 当天事件", fixedLines, FinishDayAfterFixedEvent,
                    ABConfig.Current.FixedFor(Loop, Day).id);
            else FinishDayAfterFixedEvent();
            SaveGame();
        }

        void FinishDayAfterFixedEvent()
        {
            ABFixedEvent finished = ABConfig.Current.FixedFor(Loop, Day);
            if (finished != null) completedFixedEvents.Add(finished.id);
            if (Loop == 1 && Day == 1)
            {
                // The page shows the photo to the viewer; nobody discovers/collects it on site.
                // Existing saved records are retained, but this shot is not an A2 discovery.
                SaveGame();
                BeginDay(2);
                return;
            }
            if (Loop == 2 && Day == 1)
            {
                SisyAlive = true;
                Record("thane_order", "情报", "尚未撤销的命令",
                    "SISY 活过 18:15，但 THANE 收到的处理命令仍在。");
                SaveGame();
                BeginDay(2);
                return;
            }
            if (Day == 6)
            {
                CurrentPhase = Phase.Dinner;
                SaveGame();
                return;
            }
            SaveGame();
            BeginDay(Day + 1);
        }

        public static bool SisySurvives(int[] seats, int[] taskOwners) =>
            SeatRules.Adjacent(seats, "wolf", "rabbit")
            && SeatRules.HasTag(seats[4], "far_coffee")
            && taskOwners.Length > 0 && taskOwners.All(owner => owner >= 0 && owner != 0);

        public void CompleteDinner()
        {
            if (CurrentPhase != Phase.Dinner) return;
            ABFixedEvent eventData = ABConfig.Current.FixedFor(Loop, 6);
            string[] lines = eventData?.lines ?? Array.Empty<string>();
            // One sequence; the shot configuration separates dinner from the later private meeting.
            ShowNarrative("9.12 · 聚餐", lines, () => {
                if (Loop == 2) Record("evidence_chain", "情报", "私下结盟",
                    "聚餐后 SISY、CASSIA、METIS 私下坦白并结成调查联盟。");
                completedFixedEvents.Add(eventData.id);
                SaveGame(); BeginDay(7);
            }, eventData.id);
            SaveGame();
        }

        public void ChooseFinal(bool yes)
        {
            if (CurrentPhase != Phase.FinalDecision || FinalQuestion >= 2) return;
            if (yes) YesCount++;
            FinalQuestion++;
            if (yes && FinalQuestion < 2) { SaveGame(); return; }
            CurrentPhase = YesCount == 2 ? Phase.Epilogue : Phase.BadEnding;
            EpilogueIndex = 0;
            HEElapsed = 0;
            BEElapsed = 0; BECompleted = false; EndingPresentationId = null;
            SaveGame();
        }

        public float HEElapsed { get; private set; }
        public float BEElapsed { get; private set; }
        public bool BECompleted { get; private set; }
        public string EndingPresentationId { get; private set; }
        public void RememberHETime(float seconds) => HEElapsed = Mathf.Max(0, seconds);
        public void RememberEndingTime(bool bad, float seconds, string presentation)
        {
            EndingPresentationId = presentation;
            if (bad) BEElapsed = Mathf.Max(0, seconds); else RememberHETime(seconds);
        }
        public void CompleteIllustratedEnding(bool bad)
        {
            if (!bad) { CompleteHEEnding(); return; }
            if (Loop != 2 || Day != 7 || CurrentPhase != Phase.BadEnding || BECompleted) return;
            BECompleted = true; SaveGame();
        }
        public void CompleteHEEnding()
        {
            if (Loop != 2 || Day != 7 || YesCount != 2 || CurrentPhase != Phase.Epilogue) return;
            EpilogueIndex = 9;
            CurrentPhase = Phase.TrueEnding;
            SaveGame();
        }

        public void AdvanceEpilogue()
        {
            if (CurrentPhase != Phase.Epilogue) return;
            EpilogueIndex++;
            if (EpilogueIndex > 8) { CurrentPhase = Phase.TrueEnding; SaveGame(); }
        }

        public void SaveGame()
        {
            if (Day < 1 || Day > 7 || CurrentPhase == Phase.Title) return;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
                File.WriteAllText(SavePath, JsonUtility.ToJson(BuildSave()));
            }
            catch (Exception e) { Debug.LogWarning("D-DAY save failed: " + e.Message); }
        }

        CampaignSave BuildSave() => new CampaignSave { loop = Loop, day = Day, seats = (int[])Seats.Clone(),
                taskIds = Tasks.Cards.Select(c => c.id).ToArray(),
                taskOwners = Tasks.Cards.Select(c => Tasks.Owner(c.id)).ToArray(),
                records = records.ToArray(), taskHistory = taskHistory.ToArray(), sisyAlive = SisyAlive,
                xInfoToMetis = XInfoToMetis, sisyTrustsMetis = SisyTrustsMetis, yesCount = YesCount,
                finalQuestion = FinalQuestion, epilogueIndex = EpilogueIndex,
                heElapsed = HEElapsed,
                beElapsed = BEElapsed, beCompleted = BECompleted, endingPresentationId = EndingPresentationId,
                replayIndex = ReplayIndex, phase = CurrentPhase.ToString(), excluded = excluded.ToArray(), tutorial = CaptureTutorialState(),
                triggeredSeatEvents = triggeredSeatEvents.ToArray(),
                completedFixedEvents = completedFixedEvents.ToArray(),
                fixedEventId = CurrentComicShot == null ? "" : CurrentDialogue.eventId,
                fixedDialogueIndex = CurrentComicShot == null ? 0 : NarrativeIndex,
                fixedPresentationId = CurrentPagePanel == null ? "" : CurrentPagePresentation.ContentPresentationId,
                suspectId = SuspectId, selectedSuspectId = Day7SelectedSuspectId,
                day7DialogueIndex = Loop == 1 && Day == 7 && !string.IsNullOrEmpty(SuspectId)
                    && CurrentPhase == Phase.Narrative ? NarrativeIndex : 0 };

        DDayTutorialState CaptureTutorialState()
        {
            if (!string.IsNullOrEmpty(TutorialState.pendingTaskId) && CurrentPhase == Phase.Narrative
                && CurrentComicShot == null) TutorialState.feedbackLine = NarrativeIndex;
            return TutorialState;
        }

        public void ContinueGame()
        {
            if (!TryReadContinueSave(out CampaignSave save)) return;
            try
            {
                StopAllCoroutines();
                TutorialLoading = true;
                TutorialState = save.tutorial ?? new DDayTutorialState();
                if (save.tutorial == null && (save.loop != 1 || save.day != 1
                    || save.phase == "Work" || save.phase == "AfterHours" || !string.IsNullOrEmpty(save.fixedEventId)))
                    TutorialState.done = true;
                Loop = save.loop; BeginDay(save.day);
                if (save.seats != null && save.seats.Length == Seats.Length) SetSeating(save.seats);
                RabbitSeat = Loop == 2 ? Seats[0] : 6;
                if (save.taskIds != null && save.taskOwners != null)
                    for (int i = 0; i < Mathf.Min(save.taskIds.Length, save.taskOwners.Length); i++)
                        if (save.taskOwners[i] >= 0) Tasks.Assign(save.taskIds[i], save.taskOwners[i], Loop == 2 || Day == 1);
                records.Clear();
                if (save.records != null) records.AddRange(save.records);
                taskHistory.Clear();
                if (save.taskHistory != null) taskHistory.AddRange(save.taskHistory);
                SisyAlive = save.sisyAlive; XInfoToMetis = save.xInfoToMetis;
                SisyTrustsMetis = save.sisyTrustsMetis; YesCount = save.yesCount;
                FinalQuestion = save.finalQuestion; EpilogueIndex = save.epilogueIndex;
                HEElapsed = Mathf.Max(0, save.heElapsed);
                BEElapsed = Mathf.Max(0, save.beElapsed); BECompleted = save.beCompleted;
                EndingPresentationId = save.endingPresentationId;
                ReplayIndex = save.replayIndex;
                excluded.Clear();
                if (save.excluded != null) foreach (string id in save.excluded) excluded.Add(id);
                triggeredSeatEvents.Clear();
                if (save.triggeredSeatEvents != null)
                    foreach (string id in save.triggeredSeatEvents)
                    {
                        ABClue common = ABConfig.Current.clues.FirstOrDefault(c => c.id == id && c.loop == 0);
                        int acquiredLoop = records.FirstOrDefault(r => r.id == id)?.loop ?? save.loop;
                        if (acquiredLoop < 1 || acquiredLoop > 2) acquiredLoop = save.loop;
                        triggeredSeatEvents.Add(common == null ? id : id + "@L" + acquiredLoop);
                    }
                // Keep acquired records, updating their source words without generating discovery notifications.
                foreach (CampaignRecord record in records)
                {
                    ABClue current = ABConfig.Current.clues.FirstOrDefault(c => c.id == record.id);
                    if (current != null)
                    {
                        record.title = current.title; record.description = current.description;
                        int acquiredLoop = record.loop >= 1 && record.loop <= 2 ? record.loop : save.loop;
                        // Older saves can have a book entry without a separate A2 trigger list.
                        triggeredSeatEvents.Add(current.loop == 0 ? current.id + "@L" + acquiredLoop : current.id);
                    }
                }
                completedFixedEvents.Clear();
                if (save.completedFixedEvents != null)
                    foreach (string id in save.completedFixedEvents) completedFixedEvents.Add(id);
                else
                    foreach (ABFixedEvent entry in ABConfig.Current.fixedEvents)
                        if (entry.loop < Loop || entry.loop == Loop && entry.day < Day)
                            completedFixedEvents.Add(entry.id);
                CurrentPhase = Phase.Layout;
                    SaveSlotsOpen = false;
                if (Loop == 1 && Day == 1 && save.phase == "Narrative"
                    && !string.IsNullOrEmpty(TutorialState.pendingTaskId)
                    && Tasks.Cards.Any(t => t.id == TutorialState.pendingTaskId)
                    && Tasks.Owner(TutorialState.pendingTaskId) >= 0)
                {
                    // Resume the saved official response, without assigning again or issuing any reward.
                    ShowAssignedTaskFeedback(TutorialState.pendingTaskId, Tasks.Owner(TutorialState.pendingTaskId));
                    narrativeIndex = Mathf.Clamp(TutorialState.feedbackLine, 0, Mathf.Max(0, NarrativeCount - 1));
                }
                else if (Loop == 1 && Day == 1 && save.phase == "Work"
                    && CanStartWork)
                    RunCampaignWork();
                if (Day == 6 && Enum.TryParse(save.phase, out Phase daySixPhase)
                    && daySixPhase == Phase.Dinner) { CurrentPhase = Phase.Dinner; CompleteDinner(); }
                if (Day < 7 && save.fixedEventId == ABConfig.Current.FixedFor(Loop, Day)?.id
                    && !string.IsNullOrEmpty(save.fixedEventId))
                {
                    BeginAfterHoursStory();
                    var page = ComicPagePresentation.ForEvent(save.fixedEventId);
                    int index = page != null ? page.ResumeIndex(save.fixedPresentationId, save.fixedDialogueIndex)
                        : save.fixedDialogueIndex;
                    narrativeIndex = Mathf.Clamp(index, 0, Mathf.Max(0, NarrativeCount - 1));
                }
                else if (Day < 7 && Enum.TryParse(save.phase, out Phase workPhase)
                    && (workPhase == Phase.AfterHours || workPhase == Phase.WorkSummary))
                    StartCoroutine(AfterWorkTransition());
                if (Day == 7)
                {
                    if (!Enum.TryParse(save.phase, out Phase savedPhase))
                        savedPhase = Loop == 1 ? Phase.Suspects : Phase.FinalDecision;
                    CurrentPhase = savedPhase == Phase.Narrative
                        ? (Loop == 1 ? Phase.Suspects : Phase.FinalDecision) : savedPhase;
                    if (Loop == 1)
                    {
                        SuspectId = Day7Suspects.Contains(save.suspectId) ? save.suspectId : null;
                        Day7SelectedSuspectId = Day7Suspects.Contains(save.selectedSuspectId)
                            ? save.selectedSuspectId : null;
                        if (savedPhase == Phase.Narrative && SuspectId != null)
                        {
                            ChooseSuspect(SuspectId);
                            narrativeIndex = Mathf.Clamp(save.day7DialogueIndex, 0, NarrativeCount - 1);
                        }
                    }
                }
                ApplyPalette(Loop == 2); SaveGame();
            }
            catch (Exception e) { Debug.LogWarning("D-DAY load failed: " + e.Message); Restart(); }
            finally { TutorialLoading = false; RaiseTutorialSignal(DDayTutorialSignal.Restored); }
        }

        public void ReturnToTitle()
        {
            SaveGame();
            StopAllCoroutines();
            BoardOpen = false; TasksOpen = false; SaveSlotsOpen = false;
            CurrentPhase = Phase.Title;
        }
    }
}
