using System;
using System.Linq;
using UnityEngine;

namespace DDay
{
    [Serializable] public sealed class DDayTutorialState
    {
        public bool done, demoPlaced, bookOpened, bookClosed, firstDiscoveryShown;
        public string stepId = "T00";
        public int line, viewedEmployees, bookTabs;
        public string pendingTaskId;
        public int feedbackLine;
    }
    [Serializable] public sealed class DDayTutorialStep
    {
        public string stepId, speaker, targetUiId, completeCondition, nextStepId;
        public string[] lines, emptyLines;
    }
    [Serializable] public sealed class DDayTutorialConfig
    {
        public DDayTutorialStep[] steps;
        public string accept, next, skip, reviewTitle, reviewText, discoveryItem, discoveryInfo,
            discoveryNote, discoverySisy, demoSuccess, idleHint, fixedSeatHint;
    }
    public enum DDayTutorialSignal
    {
        DayEntered, Restored, CharacterOpened, SeatPlaced, InfoObtained,
        TaskAssigned, TaskFeedbackFinished, WorkStarted, WorkFinished,
        BookOpened, BookTabViewed, BookClosed
    }

    // Observes actual campaign actions; never solves seats, assigns tasks or advances B2.
    [RequireComponent(typeof(DDayGame))]
    public sealed class DDayTutorialController : MonoBehaviour
    {
        DDayGame game;
        DDayUI ui;
        DDayTutorialConfig configuration;
        Coroutine idleTimer;
        float discoveryUntil, inputAfter;
        string discovery;
        bool idleHintVisible;
        public DDayTutorialConfig Configuration => configuration;
        public DDayTutorialState State => game.TutorialState;
        public bool Active => configuration != null && game.Loop == 1 && game.Day == 1
            && !State.done && game.CurrentPhase != Phase.Title;
        public DDayTutorialStep Step => configuration?.steps.FirstOrDefault(s => s.stepId == State.stepId);
        public bool Visible => Active && Step != null && Step.completeCondition != "work_finish"
            && game.CurrentPhase == Phase.Layout && !game.SaveSlotsOpen && !ui.OfficeSettingsOpen;
        public bool IsDialogueStep => Visible && Step.completeCondition == "continue";
        public bool IsEmployeeStep => Visible && Step.completeCondition == "employee";
        public bool SpeechVisible => Visible && !game.TasksOpen && !game.BoardOpen
            && (IsDialogueStep || IsEmployeeStep || State.line < Step.lines.Length || ShowIdleHint);
        public bool FreeSeating => Active && Step?.completeCondition == "all_seats";
        public bool ShowIdleHint => FreeSeating && idleHintVisible;
        public string Discovery => Time.unscaledTime < discoveryUntil ? discovery : null;
        public int StageNumber => Array.FindIndex(configuration.steps.Where(s => s.lines.Length > 0).ToArray(), s => s == Step) + 1;
        public int StageCount => configuration.steps.Count(s => s.lines.Length > 0);
        public bool CanAdvance => SpeechVisible && (!IsEmployeeStep || State.viewedEmployees != 0);
        public string CurrentLine
        {
            get
            {
                var lines = Step?.lines;
                if (Step?.completeCondition == "book_cycle" && game.Records.Count == 0
                    && Step.emptyLines?.Length > 0) lines = Step.emptyLines;
                string text = lines == null || lines.Length == 0 ? ""
                    : IsDialogueStep || IsEmployeeStep ? lines[Mathf.Clamp(State.line, 0, lines.Length - 1)]
                    : string.Join("\n\n", lines);
                return text.Replace("{task}", game.Tasks.Cards.FirstOrDefault()?.title ?? "")
                    .Replace("{rule}", string.Join("；", game.CurrentDay.rules
                        .Where(r => r.animal == "wolf").Select(r => r.text)));
            }
        }
        public Rect GuideRect => DDayUI.PortraitDialogueRect;

        void Awake()
        {
            game = GetComponent<DDayGame>(); ui = GetComponent<DDayUI>();
            var source = Resources.Load<TextAsset>("DDay/Tutorial/Day1Tutorial");
            if (source != null) configuration = JsonUtility.FromJson<DDayTutorialConfig>(source.text);
            if (configuration?.steps == null || configuration.steps.Length == 0)
            { Debug.LogWarning("D-DAY: tutorial config missing; ordinary campaign remains available."); configuration = null; }
        }
        void OnEnable() { if (game != null) game.TutorialSignalReceived += OnSignal; }
        void OnDisable()
        {
            if (game != null) game.TutorialSignalReceived -= OnSignal;
            StopIdleTimer();
        }
        void OnSignal(DDayTutorialSignal signal, int value, string id)
        {
            if (signal == DDayTutorialSignal.TaskFeedbackFinished && State.pendingTaskId == id)
            { State.pendingTaskId = null; State.feedbackLine = 0; game.SaveGame(); }
            if (game.TutorialLoading || !Active) { StopIdleTimer(); return; }
            switch (signal)
            {
                case DDayTutorialSignal.DayEntered:
                case DDayTutorialSignal.Restored:
                    if (Step == null) State.stepId = configuration.steps[0].stepId;
                    if (game.CurrentPhase == Phase.Work) State.stepId = "T06";
                    else if (game.CurrentPhase == Phase.AfterHours || game.CurrentPagePanel != null)
                    { Complete(); return; }
                    else if (Step.completeCondition == "work_finish") State.stepId = "T05";
                    else if (Step.completeCondition == "continue" && State.line >= Step.lines.Length) MoveNext();
                    break;
                case DDayTutorialSignal.CharacterOpened:
                    if (value >= 0 && value < 7) State.viewedEmployees |= 1 << value;
                    break;
                case DDayTutorialSignal.SeatPlaced:
                    if (value == DemoAnimal && DemoRuleSatisfied()) State.demoPlaced = true;
                    else if (value == DemoAnimal && Step.completeCondition == "demo_seat")
                        game.ShowTutorialHint(string.Join("；", game.CurrentDay.rules
                            .Where(r => r.animal == "wolf").Select(r => r.text)));
                    idleHintVisible = false;
                    break;
                case DDayTutorialSignal.InfoObtained:
                    if (!State.firstDiscoveryShown)
                    {
                        var record = game.Records.FirstOrDefault(r => r.id == id);
                        if (record != null)
                        {
                            discovery = string.Format(record.category == "物品" ? configuration.discoveryItem
                                : configuration.discoveryInfo, record.title) + "\n" + configuration.discoveryNote;
                            discoveryUntil = Time.unscaledTime + 3; State.firstDiscoveryShown = true;
                        }
                    }
                    break;
                case DDayTutorialSignal.BookOpened:
                    if (Step?.completeCondition == "book_cycle" && game.BoardOpen)
                    { State.bookOpened = true; State.bookTabs |= 1 << game.BoardTab; State.bookClosed = false; }
                    break;
                case DDayTutorialSignal.BookTabViewed:
                    if (Step?.completeCondition == "book_cycle" && game.BoardOpen) State.bookTabs |= 1 << value;
                    break;
                case DDayTutorialSignal.BookClosed:
                    if (Step?.completeCondition == "book_cycle" && !game.BoardOpen) State.bookClosed = State.bookOpened;
                    break;
                case DDayTutorialSignal.TaskAssigned: State.pendingTaskId = id; State.feedbackLine = 0; break;
                case DDayTutorialSignal.WorkStarted: State.stepId = "T06"; State.line = 0; break;
                case DDayTutorialSignal.WorkFinished: Complete(); return;
            }
            if ((signal == DDayTutorialSignal.Restored || signal == DDayTutorialSignal.TaskFeedbackFinished)
                && game.Tasks.Complete && Step?.targetUiId == "book") game.CloseTutorialTaskPanel();
            Reconcile(); game.SaveGame(); RestartIdleTimer();
        }
        public int DemoAnimal => SeatRules.Index("wolf");
        public bool DemoRuleSatisfied() => game.CurrentDay.rules.Where(r => r.animal == "wolf")
            .All(r => SeatRules.Satisfied(r, game.Seats, game.RabbitSeat));
        public bool DemoSeatAllowed(int seat)
        {
            if (seat == 6) return false;
            var candidate = (int[])game.Seats.Clone();
            int oldSeat = candidate[DemoAnimal];
            int other = Array.FindIndex(candidate, s => s == seat);
            if (other >= 0) candidate[other] = oldSeat;
            candidate[DemoAnimal] = seat;
            return game.CurrentDay.rules.Where(r => r.animal == "wolf")
                .All(r => SeatRules.Satisfied(r, candidate, game.RabbitSeat));
        }
        public bool AllowsExploration => !Visible || !SpeechVisible && (FreeSeating || Step.completeCondition == "demo_seat");
        void Reconcile()
        {
            for (int guard = 0; guard < configuration.steps.Length; guard++)
            {
                bool met = Step?.completeCondition == "demo_seat" ? State.demoPlaced && DemoRuleSatisfied()
                    : Step?.completeCondition == "all_seats" ? game.SeatsReady
                    : Step?.completeCondition == "task" ? game.Tasks.Complete && game.CurrentPhase == Phase.Layout
                    : Step?.completeCondition == "book_cycle" ? State.bookOpened && State.bookClosed && State.bookTabs == 3
                    : false;
                if (!met) break;
                MoveNext();
            }
        }
        void MoveNext()
        {
            string next = Step.nextStepId;
            if (string.IsNullOrEmpty(next)) { Complete(); return; }
            State.stepId = next; State.line = 0; inputAfter = Time.unscaledTime + .2f;
            if (Step?.completeCondition == "book_cycle")
            {
                State.bookOpened = State.bookClosed = false; State.bookTabs = 0;
                game.CloseTutorialTaskPanel();
            }
            game.BlockDialogueInputForTutorial();
        }
        public void Advance()
        {
            if (!CanAdvance || Time.unscaledTime < inputAfter) return;
            DDayAudioManager.Play(SfxCue.Dialogue);
            if (!IsDialogueStep && !IsEmployeeStep)
            {
                // Acknowledging instructions only closes speech; real actions complete the step.
                if (ShowIdleHint) idleHintVisible = false;
                else State.line = Step.lines.Length;
            }
            else if (State.line + 1 < Step.lines.Length) State.line++;
            else { MoveNext(); Reconcile(); }
            inputAfter = Time.unscaledTime + .2f; game.BlockDialogueInputForTutorial();
            game.SaveGame(); RestartIdleTimer();
        }
        public void Skip()
        {
            if (!Active) return;
            DDayAudioManager.Play(SfxCue.Back); Complete(); game.BlockDialogueInputForTutorial();
        }
        void Complete() { State.done = true; StopIdleTimer(); game.SaveGame(); }
        void RestartIdleTimer()
        {
            StopIdleTimer(); if (FreeSeating) idleTimer = StartCoroutine(IdleHint());
        }
        void StopIdleTimer() { if (idleTimer != null) StopCoroutine(idleTimer); idleTimer = null; }
        System.Collections.IEnumerator IdleHint()
        {
            yield return new WaitForSecondsRealtime(45);
            if (FreeSeating && !game.SeatsReady && game.DragAnimal < 0
                && !game.BoardOpen && !game.TasksOpen && !game.SaveSlotsOpen && !ui.OfficeSettingsOpen)
                idleHintVisible = true;
            idleTimer = null;
        }
        public bool BlocksOfficePointer(Vector2 point)
        {
            if (!Visible) return false;
            return SpeechVisible || IsDialogueStep || IsEmployeeStep
                || Step.completeCondition == "task" || Step.completeCondition == "book_cycle"
                || Step.completeCondition == "work_start";
        }
        public bool AllowsToolbar(string action)
        {
            if (!Visible) return true;
            if (SpeechVisible || IsDialogueStep || IsEmployeeStep) return false;
            if (action == "开始工作") return Step.completeCondition == "work_start";
            if (action == "记录册") return Step.completeCondition == "book_cycle" || FreeSeating;
            return true;
        }
        public bool AllowsUiRect(Rect rect)
        {
            if (!Visible) return true;
            if (SpeechVisible || IsDialogueStep || IsEmployeeStep) return false;
            if (Step.completeCondition == "task") return game.TasksOpen || ui.TutorialTaskRect.Overlaps(rect);
            if (Step.completeCondition == "book_cycle") return game.BoardOpen;
            if (Step.completeCondition == "demo_seat" || FreeSeating) return false;
            return true;
        }
    }
}
