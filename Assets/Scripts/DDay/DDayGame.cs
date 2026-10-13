using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DDay
{
    public enum Phase { Title, Narrative, Layout, Conversation, Transition, Work, Fatigue, Death, Suspects, SuspectResult, Everyone, EveryoneResult, Replay, Credits, SecondLoop, RescueFailure, RescueApproval, TrueEnding, WorkSummary, FinalDecision, BadEnding, Epilogue, Dinner, AfterHours }

    [RequireComponent(typeof(DDayUI))]
    public sealed partial class DDayGame : MonoBehaviour
    {
        public const float OfficeSafeLeft = 278f;
        public const float OfficeSafeRight = 1480f;
        public const float OfficeSafeMargin = 40f;
        const float DeskVisualScale = .92f;
        public Database Data { get; private set; }
        public Phase CurrentPhase { get; private set; } = Phase.Title;
        public int Day { get; private set; }
        public int Loop { get; private set; } = 1;
        public DayData CurrentDay => Day >= 1 && Day <= 6
            ? (Loop == 2 ? secondDays : firstDays)?[Day - 1] : null;
        public TaskAssignments Tasks { get; } = new TaskAssignments();
        public bool TasksOpen { get; private set; }
        public RescueAssessment RescueResult { get; private set; }
        public string RescueFailureText { get; private set; }
        public int[] Seats { get; } = new int[7];
        public int RabbitSeat { get; private set; } = 0;
        public int SelectedAnimal { get; private set; } = -1;
        public int SelectedSeat { get; private set; } = -1;
        public int DragAnimal { get; private set; } = -1;
        public int HoverSeat { get; private set; } = -1;
        public int HoveredAnimal { get; private set; } = -1;
        public bool BoardOpen { get; private set; }
        public int BoardTab { get; private set; }
        public string NarrativeTitle { get; private set; }
        public string NarrativeLine => narrativeLines != null && narrativeIndex < narrativeLines.Length ? narrativeLines[narrativeIndex] : "";
        public int NarrativeIndex => narrativeIndex;
        public int NarrativeCount => narrativeLines == null ? 0 : narrativeLines.Length;
        public int DeathStage { get; private set; }
        public string SuspectId { get; private set; }
        public string Day7SelectedSuspectId { get; private set; }
        public bool CanConfirmDay7Suspect => Loop == 1 && Day == 7 && CurrentPhase == Phase.Suspects
            && Day7Suspects.Contains(Day7SelectedSuspectId);
        public string FlashClue { get; private set; }
        public float FlashUntil { get; private set; }
        public int ReplayIndex { get; private set; }
        public string ReplayLine => Data != null && ReplayIndex < Data.replay.Length
            ? Data.replay[ReplayIndex] : "";
        public int Satisfied => CurrentDay == null ? 0 : SeatRules.CountSatisfied(CurrentDay, Seats, RabbitSeat);
        public int TotalRules => CurrentDay == null ? 0 : CurrentDay.rules.Length;
        public bool SeatsReady => CurrentDay != null && Satisfied == TotalRules;
        public bool CanStartWork => (CurrentPhase == Phase.Layout || CurrentPhase == Phase.SecondLoop)
            && SeatsReady && Tasks.Complete;
        public string FirstUnmetSeatRule
        {
            get
            {
                RuleData rule = CurrentDay?.rules.FirstOrDefault(item =>
                    !SeatRules.Satisfied(item, Seats, RabbitSeat));
                return rule == null ? "" : SeatRules.DisplayName(rule.animal) + "：" + rule.text;
            }
        }
        public int AvailableDialogueCount
        {
            get
            {
                if (Day < 3 || Day > 6) return 0;
                int count = 0;
                foreach (var ev in Data.events)
                    if (ev.day == Day && EventAvailable(ev)) count++;
                return Mathf.Min(2, count);
            }
        }
        public bool AllSuspectsExcluded => excluded.Count == 6;
        public int ExcludedCount => excluded.Count;
        public IReadOnlyCollection<string> Clues => clues;
        public DialogueData ActiveConversation { get; private set; }
        public int ConversationIndex { get; private set; }
        public string ConversationLine => ActiveConversation != null && ConversationIndex < ActiveConversation.lines.Length
            ? ActiveConversation.lines[ConversationIndex] : "";
        public float TransitionAlpha { get; private set; }
        public IEnumerable<DialogueData> AvailableConversations => Day == 2 && CurrentPhase == Phase.Layout
            ? Data.events.Where(ev => ev.day == 2 && !readConversations.Contains(ev.id)
                && EventAvailable(ev))
            : Enumerable.Empty<DialogueData>();
        bool EventAvailable(DialogueData ev) => SeatRules.Adjacent(Seats, ev.a, ev.b)
            && (ev.requires == null || ev.requires.All(clues.Contains))
            && (string.IsNullOrEmpty(ev.taskId)
                || Tasks.Owner(ev.taskId) == SeatRules.Index(ev.taskOwner));
        public int ItemClueCount => (HasClue("hire_form") ? 1 : 0) +
            (HasClue("coffee_cup") || HasClue("last_coffee") ? 1 : 0);

        readonly HashSet<string> clues = new HashSet<string>();
        readonly HashSet<string> readConversations = new HashSet<string>();
        readonly HashSet<string> excluded = new HashSet<string>();
        readonly List<DialogueData> pendingEvents = new List<DialogueData>();
        string[] narrativeLines;
        int narrativeIndex;
        Action afterNarrative;
        int pendingEventIndex;
        bool draggingMoved;
        Vector3 mouseDownWorld;
        Vector3 draggedStartPosition;
        Camera gameCamera;
        Transform worldRoot;
        readonly GameObject[] animalObjects = new GameObject[7];
        readonly SpriteRenderer[] animalRenderers = new SpriteRenderer[7];
        readonly SpriteRenderer[] selectionOutlines = new SpriteRenderer[7];
        readonly SpriteRenderer[] seatGlows = new SpriteRenderer[8];
        readonly SpriteRenderer[] roomRenderers = new SpriteRenderer[6];
        readonly Transform[] personalPropRoots = new Transform[7];
        readonly Transform[] characterAnchors = new Transform[8];
        readonly Transform[] deskPropAnchors = new Transform[8];
        readonly Transform[] seatRoots = new Transform[8];
        int lastScreenWidth, lastScreenHeight;
        float officeContentScale = 1f;
        GameObject memorial;
        readonly Vector3[] seatPositions = new Vector3[8];
        public bool IsConversationRead(string id) => readConversations.Contains(id);
        public Vector3 AnimalWorldPoint(int index) => index >= 0 && index < 7 && animalRenderers[index] != null
            ? new Vector3(animalRenderers[index].bounds.center.x,
                animalRenderers[index].bounds.max.y - DDayArt.HeadPaddingWorld(index)
                    * animalRenderers[index].transform.lossyScale.y, 0)
            : Vector3.zero;
        public Vector3 FacilityWorldPoint(int index) => index == 0 ? new Vector3(-5.37f, 3.12f)
            : index == 1 ? new Vector3(4.32f, 3.12f) : new Vector3(-.08f, 3.12f);

        void Awake()
        {
            var json = Resources.Load<TextAsset>("DDay/Content");
            if (json == null) { Debug.LogError("D-DAY: Content.json missing."); enabled = false; return; }
            Data = JsonUtility.FromJson<Database>(json.text);
            if (Data == null || Data.days == null || Data.days.Length != 6)
            {
                Debug.LogError("D-DAY: expected six playable day definitions."); enabled = false; return;
            }
            firstDays = CampaignSchedule.MakeDays(Data, 1);
            secondDays = CampaignSchedule.MakeDays(Data, 2);
            LoadSeatExplorations();
            gameCamera = Camera.main;
            if (gameCamera == null) gameCamera = FindFirstObjectByType<Camera>();
            if (gameCamera == null) { Debug.LogError("D-DAY: Main Camera missing."); enabled = false; return; }
            gameCamera.orthographic = true;
            gameCamera.orthographicSize = 5.1f;
            gameCamera.transform.position = new Vector3(0, 0, -10);
            gameCamera.backgroundColor = new Color(.74f, .81f, .78f);
            BuildOffice();
            if (GetComponent<DDayAudioManager>() == null) gameObject.AddComponent<DDayAudioManager>();
            if (GetComponent<DDayOfficeHud>() == null) gameObject.AddComponent<DDayOfficeHud>();
            if (GetComponent<DDayTutorialController>() == null) gameObject.AddComponent<DDayTutorialController>();
            Restart();
        }

        void Update()
        {
            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
                ReflowOfficeSafeArea();
            HoveredAnimal = (CurrentPhase == Phase.Layout || CurrentPhase == Phase.SecondLoop)
                && !BoardOpen && !TasksOpen
                && !SaveSlotsOpen && PointerInOffice()
                ? FindAnimalAt(MouseWorld()) : -1;
            UpdateSelectionOutlines();
            if (Time.frameCount <= DialogueInputBlockedUntilFrame) return;
            if (CurrentPhase != Phase.Layout && CurrentPhase != Phase.Fatigue
                && CurrentPhase != Phase.SecondLoop) return;
            if (BoardOpen || TasksOpen || SaveSlotsOpen
                || GetComponent<DDayUI>().OfficeSettingsOpen) return;
            var tutorial = GetComponent<DDayTutorialController>();
            if (tutorial != null && tutorial.BlocksOfficePointer(DDayUiSpace.FromScreen(Input.mousePosition))) return;
            bool pointerBlocked = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            DDayOfficeHud hud = GetComponent<DDayOfficeHud>();
            pointerBlocked |= hud != null && hud.IsPointerOverExplorationBubble(Input.mousePosition);
            if (Input.GetMouseButtonDown(0) && PointerInOffice() && !pointerBlocked)
            {
                Vector3 point = MouseWorld();
                int animal = FindAnimalAt(point);
                if (animal >= 0)
                {
                    SelectAnimal(animal);
                    if (animal == 0 && Loop == 1 && Day == 1 && tutorial != null && tutorial.Active)
                        SetFlash(tutorial.Configuration.fixedSeatHint, 1.8f);
                    if ((CurrentPhase == Phase.Layout || CurrentPhase == Phase.SecondLoop)
                        && !(Loop == 1 && Day == 1 && animal == 0))
                    {
                        DragAnimal = animal;
                        draggingMoved = false;
                        mouseDownWorld = point;
                        draggedStartPosition = animalObjects[animal].transform.position;
                        animalRenderers[animal].sortingOrder = 40;
                        RefreshDeskProps();
                    }
                }
                else
                {
                    SelectedSeat = NearestSeat(point);
                    GetComponent<DDayUI>().SetPublicProfile(-1);
                }
            }
            if (DragAnimal >= 0 && Input.GetMouseButton(0))
            {
                Vector3 point = MouseWorld();
                if (!draggingMoved && (point - mouseDownWorld).sqrMagnitude > .025f)
                    MarkSeatDragMoved();
                if (draggingMoved)
                {
                    animalObjects[DragAnimal].transform.position = new Vector3(point.x, point.y, 0);
                    HoverSeat = NearestSeat(point);
                }
            }
            if (DragAnimal >= 0 && Input.GetMouseButtonUp(0))
            {
                int moving = DragAnimal;
                int target = draggingMoved ? NearestSeat(MouseWorld()) : -1;
                FinishSeatDrag(moving, target, draggingMoved);
            }
            UpdateGlows();
        }

        void MarkSeatDragMoved()
        {
            if (draggingMoved) return;
            draggingMoved = true;
            DDayAudioManager.Play(SfxCue.Pickup);
        }

        void FinishSeatDrag(int moving, int target, bool moved)
        {
            DragAnimal = -1; HoverSeat = -1;
            animalRenderers[moving].sortingOrder = 20;
            bool placed = moved && target >= 0 && TryPlaceAnimal(moving, target);
            if (!placed) animalObjects[moving].transform.position = draggedStartPosition;
            if (moved) DDayAudioManager.Play(placed ? SfxCue.Drop : SfxCue.Error);
            RefreshDeskProps();
        }

        bool PointerInOffice()
        {
            Vector2 point = DDayUiSpace.FromScreen(Input.mousePosition);
            float x = point.x, y = point.y;
            return x > OfficeSafeLeft && x < OfficeSafeRight && y > 83 && y < 970;
        }

        Vector3 MouseWorld()
        {
            var m = Input.mousePosition;
            var point = gameCamera.ScreenToWorldPoint(new Vector3(m.x, m.y, 10f));
            return new Vector3(point.x, point.y, 0);
        }

        int FindAnimalAt(Vector3 point)
        {
            for (int i = animalObjects.Length - 1; i >= 0; i--)
            {
                if (Seats[i] < 0 || animalObjects[i] == null || !animalObjects[i].activeSelf) continue;
                Bounds bounds = animalRenderers[i].bounds;
                if (point.x >= bounds.min.x - .12f && point.x <= bounds.max.x + .12f
                    && point.y >= bounds.min.y - .12f && point.y <= AnimalWorldPoint(i).y + .12f) return i;
            }
            return -1;
        }

        int NearestSeat(Vector3 point)
        {
            int best = -1;
            float bestSq = 1.75f * 1.75f;
            for (int i = 0; i < 8; i++)
            {
                Vector3 center = seatPositions[i] + new Vector3(0, .86f * officeContentScale, 0);
                float sq = (point - center).sqrMagnitude;
                if (sq < bestSq) { best = i; bestSq = sq; }
            }
            return best;
        }

        public bool TryPlaceAnimal(int animal, int targetSeat)
        {
            if ((CurrentPhase != Phase.Layout && CurrentPhase != Phase.SecondLoop)
                || animal < 0 || animal >= Seats.Length
                || Seats[animal] < 0 || targetSeat < 0 || targetSeat >= 8
                || (Loop == 1 && Day == 1 && animal == 0)
                || (Loop == 1 && targetSeat == 6)) return false;
            int original = Seats[animal];
            if (original == targetSeat) { RefreshAnimals(); return true; }
            int occupant = -1;
            for (int i = 0; i < Seats.Length; i++)
                if (i != animal && Seats[i] == targetSeat) { occupant = i; break; }
            Seats[animal] = targetSeat;
            if (occupant >= 0) Seats[occupant] = original;
            if (Loop == 2 && animal == 0) RabbitSeat = targetSeat;
            if (Loop == 2 && occupant == 0) RabbitSeat = original;
            SelectedSeat = targetSeat;
            RefreshAnimals();
            SaveGame();
            RaiseTutorialSignal(DDayTutorialSignal.SeatPlaced, animal);
            return true;
        }

        void SetFlash(string message, float seconds)
        {
            FlashClue = message;
            FlashUntil = Time.unscaledTime + seconds;
        }

        public void SelectAnimal(int index)
        {
            if (index >= 0 && index < 7 && Seats[index] >= 0)
            {
                SelectedAnimal = index;
                SelectedSeat = Seats[index];
                RaiseTutorialSignal(DDayTutorialSignal.CharacterOpened, index);
            }
        }

        public void ToggleTasks()
        {
            if (CurrentPhase != Phase.Layout && CurrentPhase != Phase.SecondLoop) return;
            if (!TasksOpen && !SeatsReady)
            {
                SetFlash("座位不达标", 3f);
                return;
            }
            BoardOpen = false; SaveSlotsOpen = false;
            TasksOpen = !TasksOpen;
        }

        public void CloseTutorialTaskPanel()
        {
            if (Loop == 1 && Day == 1 && CurrentPhase == Phase.Layout && Tasks.Complete)
                TasksOpen = false;
        }

        public void AssignTask(string id, int animal)
        {
            if (CurrentPhase != Phase.Layout || !SeatsReady || !TasksOpen) return;
            int previous = Tasks.Owner(id);
            if (!Tasks.Assign(id, animal, Loop == 2 || Day == 1)) return;
            if (previous != animal) DDayAudioManager.Play(SfxCue.Confirmation);
            SetFlash("任务已交给 " + SeatRules.DisplayName(SeatRules.Ids[animal]), 1.3f);
            SaveGame();
            if (previous == animal) return;
            ShowAssignedTaskFeedback(id, animal);
            RaiseTutorialSignal(DDayTutorialSignal.TaskAssigned, animal, id);
        }

        void ShowAssignedTaskFeedback(string id, int animal)
        {
            string actor = SeatRules.Ids[animal];
            string response = CampaignSchedule.TaskResponse(Loop, Day, id, animal, Tasks.Cards.Any(card => Tasks.Owner(card.id) == 0));
            ShowNarrative("分配任务", DialoguePresentation.SplitTurns(response), () => {
                CurrentPhase = Phase.Layout; TasksOpen = true; SaveGame();
                RaiseTutorialSignal(DDayTutorialSignal.TaskFeedbackFinished, animal, id);
            }, "B1_L" + Loop + "_D" + Day + "_" + id + "_" + actor, actor);
        }

        public bool HasClue(string id) => clues.Contains(id);
        public bool IsExcluded(string id) => excluded.Contains(id);

        void AddClue(string id)
        {
            if (string.IsNullOrEmpty(id) || !clues.Add(id)) return;
            DDayAudioManager.Play(SfxCue.Clue);
            var clue = Data.clues.FirstOrDefault(c => c.id == id);
            SetFlash(clue == null ? "发现新线索" : "新线索：" + clue.title, 3f);
        }

        public bool OpenConversation(string id)
        {
            if (CurrentPhase != Phase.Layout || Day != 2) return false;
            var ev = AvailableConversations.FirstOrDefault(item => item.id == id);
            if (ev == null) return false;
            if (DragAnimal >= 0)
            {
                animalRenderers[DragAnimal].sortingOrder = 20;
                DragAnimal = -1;
                HoverSeat = -1;
                RefreshAnimals();
            }
            ActiveConversation = ev;
            ConversationIndex = 0;
            CurrentPhase = Phase.Conversation;
            return true;
        }

        public void AdvanceConversation()
        {
            if (CurrentPhase != Phase.Conversation || ActiveConversation == null) return;
            DDayAudioManager.Play(SfxCue.Dialogue);
            ConversationIndex++;
            if (ConversationIndex < ActiveConversation.lines.Length) return;
            readConversations.Add(ActiveConversation.id);
            AddClue(ActiveConversation.reward);
            ActiveConversation = null;
            CurrentPhase = Phase.Layout;
        }

        public void ToggleBoard(int tab = 0)
        {
            if (CurrentPhase != Phase.Layout) return;
            TasksOpen = false; SaveSlotsOpen = false;
            BoardOpen = !BoardOpen;
            BoardTab = Mathf.Clamp(tab, 0, 1);
            RaiseTutorialSignal(BoardOpen ? DDayTutorialSignal.BookOpened : DDayTutorialSignal.BookClosed, BoardTab);
        }

        public void SetBoardTab(int tab)
        {
            BoardTab = Mathf.Clamp(tab, 0, 1);
            RaiseTutorialSignal(DDayTutorialSignal.BookTabViewed, BoardTab);
        }

        public void Restart()
        {
            StopAllCoroutines();
            TutorialState = new DDayTutorialState();
            clues.Clear(); excluded.Clear(); pendingEvents.Clear(); readConversations.Clear();
            Day = 0; Loop = 1; RabbitSeat = 6;
            SelectedAnimal = -1; SelectedSeat = -1;
            BoardOpen = false; TasksOpen = false; SaveSlotsOpen = false;
            DragAnimal = -1; HoverSeat = -1;
            HoveredAnimal = -1;
            Tasks.Reset(null); RescueResult = null; RescueFailureText = null;
            records.Clear(); taskHistory.Clear(); SisyAlive = false; XInfoToMetis = false;
            triggeredSeatEvents.Clear();
            completedFixedEvents.Clear();
            SisyTrustsMetis = false; YesCount = 0; FinalQuestion = 0; EpilogueIndex = 0;
            HEElapsed = 0;
            BEElapsed = 0; BECompleted = false; EndingPresentationId = null;
            SuspectId = null; Day7SelectedSuspectId = null;
            CurrentPhase = Phase.Title;
            ActiveConversation = null;
            TransitionAlpha = 0;
            SetSeating(new[] { 0, 1, 2, 3, 5, 4, 6 });
            ApplyPalette();
        }

        public void StartGame()
        {
            Restart();
            try { if (CanContinue) System.IO.File.Delete(SavePath); }
            catch (Exception e) { Debug.LogWarning("D-DAY reset save failed: " + e.Message); }
            BeginDay(1);
        }

        void BeginDay(int number)
        {
            Day = number;
            RabbitSeat = 6;
            SelectedAnimal = -1;
            BoardOpen = false; TasksOpen = false; SaveSlotsOpen = false;
            if (number == 7)
            {
                SuspectId = null; Day7SelectedSuspectId = null;
                Tasks.Reset(null);
                SetSeating(new[] { -1, -1, -1, -1, -1, -1, -1 });
                ApplyPalette(Loop == 2);
                CurrentPhase = Loop == 1 ? Phase.Suspects : Phase.FinalDecision;
                if (Loop == 2) { FinalQuestion = 0; YesCount = 0; }
                SaveGame();
                return;
            }
            ABSeatDay seating = ABConfig.Current.SeatDay(Loop, number);
            if (seating?.initialSeats == null || seating.initialSeats.Length != Seats.Length)
                throw new InvalidOperationException("D-DAY: missing audited initial layout");
            SetSeating(seating.initialSeats);
            Tasks.Reset(CurrentDay.tasks);
            RabbitSeat = Loop == 2 ? Seats[0] : 6;
            if (SeatsReady) throw new InvalidOperationException("D-DAY: initial layout must be unsolved");
            ApplyPalette(Loop == 2);
            if (Loop == 1 && number == 1 && !TutorialState.done
                && GetComponent<DDayTutorialController>()?.Configuration != null)
                CurrentPhase = Phase.Layout;
            else if (CurrentDay.intro.Length == 0) CurrentPhase = Phase.Layout;
            else ShowNarrative("DAY " + number + " · " + CurrentDay.title,
                CurrentDay.intro, () => CurrentPhase = Phase.Layout);
            RaiseTutorialSignal(DDayTutorialSignal.DayEntered);
            SaveGame();
        }

        void SetSeating(int[] positions)
        {
            Array.Copy(positions, Seats, 7);
            RefreshAnimals();
        }

        void RefreshAnimals()
        {
            for (int i = 0; i < 7; i++)
            {
                bool visible = Seats[i] >= 0;
                if (animalObjects[i] == null) continue;
                animalObjects[i].SetActive(visible);
                if (visible)
                {
                    animalObjects[i].transform.SetParent(characterAnchors[Seats[i]], false);
                    animalObjects[i].transform.localPosition = Vector3.zero;
                }
            }
            if (memorial != null) memorial.SetActive(Loop == 1 && Day >= 2);
            RefreshDeskProps();
        }

        void RefreshDeskProps()
        {
            for (int i = 0; i < personalPropRoots.Length; i++)
            {
                Transform root = personalPropRoots[i];
                if (root == null) continue;
                bool rabbitBelongings = i == 0 && Loop == 1 && Day >= 2;
                int seat = Seats[i] >= 0 ? Seats[i] : RabbitSeat;
                root.SetParent(deskPropAnchors[seat], false);
                root.localPosition = Vector3.zero;
                root.gameObject.SetActive((Seats[i] >= 0 || rabbitBelongings) && DragAnimal != i);
            }
        }

        void UpdateGlows()
        {
            for (int i = 0; i < seatGlows.Length; i++)
            {
                if (seatGlows[i] == null) continue;
                bool valid = DragAnimal >= 0 && !(Loop == 1 && i == 6);
                seatGlows[i].enabled = valid;
                seatGlows[i].color = HoverSeat == i ? new Color(.45f, 1f, .75f, .92f)
                    : new Color(.88f, .91f, .70f, .34f);
            }
        }

        void UpdateSelectionOutlines()
        {
            for (int i = 0; i < selectionOutlines.Length; i++)
            {
                var outline = selectionOutlines[i];
                if (outline == null) continue;
                bool selected = (CurrentPhase == Phase.Layout || CurrentPhase == Phase.SecondLoop)
                    && (SelectedAnimal == i || HoveredAnimal == i) && Seats[i] >= 0;
                outline.enabled = selected;
                if (selected)
                {
                    float alpha = .40f + .13f * (1f + Mathf.Sin(Time.unscaledTime * 3f)) * .5f;
                    outline.color = new Color(.46f, .95f, .79f, alpha);
                }
            }
        }

        public void StartWork()
        {
            if (CurrentPhase != Phase.Layout && CurrentPhase != Phase.SecondLoop) return;
            if (!SeatsReady)
            {
                DDayAudioManager.Play(SfxCue.Error);
                SetFlash("座位不达标", 3f);
                return;
            }
            if (!Tasks.Complete)
            {
                BoardOpen = false; SaveSlotsOpen = false;
                TasksOpen = true;
                return;
            }
            DDayAudioManager.Play(SfxCue.Confirmation);
            RunCampaignWork();
        }

        IEnumerator AdvanceToDayThree()
        {
            CurrentPhase = Phase.Transition;
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / .65f)
            {
                TransitionAlpha = Mathf.SmoothStep(0, 1, t);
                yield return null;
            }
            TransitionAlpha = 1;
            yield return new WaitForSecondsRealtime(.25f);
            BeginDay(3);
            TransitionAlpha = 0;
        }

        void PlayNextEvent()
        {
            if (pendingEventIndex < pendingEvents.Count)
            {
                var ev = pendingEvents[pendingEventIndex++];
                ShowNarrative(ev.title, ev.lines, () => { AddClue(ev.reward); PlayNextEvent(); });
                return;
            }
            foreach (string id in CurrentDay.guaranteedClues) AddClue(id);
            ShowNarrative("DAY " + Day + " END", CurrentDay.outro, () => BeginDay(Day + 1));
        }

        public void ContinueFatigue()
        {
            if (CurrentPhase != Phase.Fatigue || SelectedAnimal != 0) return;
            StartCoroutine(DeathSequence());
        }

        IEnumerator DeathSequence()
        {
            CurrentPhase = Phase.Death;
            DeathStage = 0;
            yield return new WaitForSecondsRealtime(1.6f);
            DeathStage = 1;
            yield return new WaitForSecondsRealtime(1.3f);
            DeathStage = 2;
            yield return new WaitForSecondsRealtime(1.7f);
            DeathStage = 3;
        }

        public void ContinueDeath()
        {
            if (CurrentPhase != Phase.Death || DeathStage != 3) return;
            Record("orin_photo", "物品", "ORIN 的照片",
                "警方在 SISY 身边发现的旧照片。照片里的两个人关系似乎不止普通同事。");
            ShowNarrative("18:15 · 遗留物品", new[] {
                "SISY 身边发现一张与 ORIN 的旧照片。警方把它收进证物袋。",
                "所有人的座位和今天被转交的工作，都需要重新看一遍。"
            }, () => BeginDay(2));
        }

        void ShowNarrative(string title, string[] lines, Action after,
            string eventId = "", string focus = "", int lineOffset = 0)
        {
            NarrativeTitle = title;
            PrepareDialogue(eventId, lines ?? Array.Empty<string>(), focus, lineOffset);
            narrativeIndex = 0;
            afterNarrative = after;
            CurrentPhase = Phase.Narrative;
            if (narrativeLines.Length == 0) AdvanceNarrative();
        }

        public void AdvanceNarrative()
        {
            if (CurrentPhase != Phase.Narrative) return;
            if (pageComicEnding) return;
            DDayAudioManager.Play(SfxCue.Dialogue);
            if (CurrentPagePanel != null && narrativeIndex == narrativeLines.Length - 1)
            { StartCoroutine(FinishPageComic()); return; }
            narrativeIndex++;
            if (narrativeIndex < narrativeLines.Length)
            {
                if (CurrentComicShot != null) SaveGame();
                return;
            }
            CompleteNarrative();
        }

        void CompleteNarrative()
        {
            var next = afterNarrative;
            afterNarrative = null;
            DialogueInputBlockedUntilFrame = Time.frameCount + 1;
            next?.Invoke();
        }

        public void SelectDay7Suspect(string id)
        {
            if (Loop != 1 || Day != 7 || CurrentPhase != Phase.Suspects || !Day7Suspects.Contains(id)) return;
            Day7SelectedSuspectId = Day7SelectedSuspectId == id ? null : id;
            SaveGame();
        }

        public void ConfirmDay7Suspect()
        {
            if (CanConfirmDay7Suspect) ChooseSuspect(Day7SelectedSuspectId);
        }

        public void ChooseSuspect(string id)
        {
            if (Loop != 1 || Day != 7 || CurrentPhase != Phase.Suspects || !Day7Suspects.Contains(id)) return;
            SuspectId = id;
            Day7SelectedSuspectId = id;
            string name = id == "boss" ? "老板" : SeatRules.DisplayName(id);
            ShowNarrative("DAY 7 · " + name, new[] {
                name + "的选择确实促成了这个结果，但没有证据表明存在唯一的直接凶手。",
                "请重看 DAY 1：工作转嫁、沉默、回避和催促如何相互叠加。"
            }, ChooseNoCulpritDirect);
            SaveGame();
        }

        public void ExcludeSuspect()
        {
            if (CurrentPhase != Phase.SuspectResult) return;
            excluded.Add(SuspectId);
            CurrentPhase = excluded.Count == 6 ? Phase.Everyone : Phase.Suspects;
            SaveGame();
        }

        public void ChooseEveryone()
        {
            if (CurrentPhase == Phase.Everyone) CurrentPhase = Phase.EveryoneResult;
        }

        public void ChooseNoCulprit()
        {
            if (CurrentPhase != Phase.EveryoneResult) return;
            ChooseNoCulpritDirect();
        }

        public void AdvanceReplay()
        {
            if (CurrentPhase != Phase.Replay) return;
            ReplayIndex++;
            if (ReplayIndex >= Data.replay.Length)
                ShowNarrative("第一次结局 · 没有单一凶手", new[] {
                    "THANE 有命令，也有执行能力，却没有亲手杀死 SISY。",
                    "工作转嫁、沉默、回避、刺激和管理者的催促一起把 TA 推向终点。",
                    "员工不会记得上一周目。你记得。再经历一次 D-DAY，试着改变结果。"
                }, BeginSecondLoop);
        }

        public void BeginCausalReplay()
        {
            if (CurrentPhase != Phase.Suspects) return;
            ChooseNoCulpritDirect();
        }

        void ChooseNoCulpritDirect()
        {
            SetSeating(new[] { 0, 3, 2, 1, 4, 6, 7 });
            ApplyPalette(true);
            ReplayIndex = 0;
            CurrentPhase = Phase.Replay;
        }

        void BeginSecondLoop()
        {
            Loop = 2; SisyAlive = false; BoardOpen = false; TasksOpen = false;
            SelectedAnimal = -1; SelectedSeat = -1;
            BeginDay(1);
        }

        public void RunSecondLoop()
        {
            RunCampaignWork();
        }

        public void ApproveRabbitLeave(bool approve)
        {
            if (CurrentPhase != Phase.RescueApproval) return;
            if (approve)
            {
                SisyAlive = true;
                Record("thane_order", "情报", "尚未撤销的命令",
                    "SISY 活过 18:15，但 THANE 收到的处理命令仍在。危险不是只有过劳。");
                ShowNarrative("DAY CLEAR · 18:15", new[] {
                    "SISY 提前离开。ORIN 跟了上去。今天 TA 活了下来。",
                    "THANE 的处理命令仍在。接下来要查清 APIS 为什么要让 SISY 消失。"
                }, () => { SaveGame(); BeginDay(2); });
            }
            else
            {
                RescueFailureText = "所有任务如期完成，兔子却仍然倒下了。";
                CurrentPhase = Phase.RescueFailure;
            }
        }

        public void RetrySecondLoop()
        {
            if (CurrentPhase != Phase.RescueFailure) return;
            BeginDay(Day == 1 ? 1 : Day);
        }

        public string SuspectEvidence(string id)
        {
            switch (id)
            {
                case "fox": return "与兔子公开争吵；威胁灰狼；害怕招聘秘密曝光。";
                case "wolf": return "回避身体不适的兔子；18:12登录电脑并删除材料。";
                case "shiba": return "帮助狐狸绕过招聘筛选；办公室咖啡来自 TA。";
                case "cat": return "递给兔子最后喝过的咖啡；看到了许多事却沉默。";
                case "bear": return "在兔子身体不适时仍催促 TA 处理项目。";
                case "panda": return "知道兔子疲惫，仍在截止日前继续催促。";
                default: return "没有直接杀人的证据。";
            }
        }

        void BuildOffice()
        {
            var root = new GameObject("D-DAY Runtime Office");
            root.transform.SetParent(transform);
            worldRoot = root.transform;
            roomRenderers[0] = AddSprite("BACKGROUND / unified office",
                DDayArt.OfficeBackground, Vector3.zero, 1f, -40, worldRoot);
            if (roomRenderers[0].sprite != null)
            {
                Vector2 size = roomRenderers[0].sprite.bounds.size;
                float height = gameCamera.orthographicSize * 2f;
                // Keep the original pixel art ratio. Any side gap remains behind the HUD.
                roomRenderers[0].transform.localScale = Vector3.one * (height / size.y);
            }
            var safeArea = new GameObject("OfficeSafeArea / HUD-free eight seats");
            safeArea.transform.SetParent(worldRoot);
            for (int seat = 0; seat < 8; seat++)
            {
                int row = seat / 4;
                Vector3 center = new Vector3(0, row == 0 ? -1.05f : -3.51f, 0);
                seatPositions[seat] = center;
                var parent = new GameObject("SEAT " + seat + " / fixed desk and chair");
                parent.transform.SetParent(safeArea.transform);
                parent.transform.position = center;
                seatRoots[seat] = parent.transform;
                var anchor = new GameObject("CharacterAnchor " + seat);
                anchor.transform.SetParent(parent.transform);
                anchor.transform.localPosition = new Vector3(0, .69f, 0);
                characterAnchors[seat] = anchor.transform;
                var deskBack = new GameObject("DeskBack " + seat);
                deskBack.transform.SetParent(parent.transform);
                deskBack.transform.localPosition = Vector3.zero;
                var deskFront = new GameObject("DeskFront " + seat);
                deskFront.transform.SetParent(parent.transform);
                deskFront.transform.localPosition = Vector3.zero;
                var propAnchor = new GameObject("PersonalProps Anchor " + seat);
                propAnchor.transform.SetParent(parent.transform);
                propAnchor.transform.localPosition = new Vector3(0, 1.04f, 0);
                deskPropAnchors[seat] = propAnchor.transform;
                var glow = AddSprite("INTERACTION / drag target only", "ToiletBreakV02/Pixel/Rug",
                    new Vector3(0, .48f), .77f, 9, parent.transform);
                glow.enabled = false;
                seatGlows[seat] = glow;
                AddSprite("SEAT LAYER / chair", DDayArt.OfficeChair,
                    new Vector3(-.02f, .60f), 1f, 10, deskBack.transform);
                AddSprite("SEAT LAYER / desk", DDayArt.OfficeDesk,
                    Vector3.zero, DeskVisualScale, 15, deskBack.transform);
                AddSprite("SEAT LAYER / desk front", DDayArt.OfficeDeskFront,
                    Vector3.zero, DeskVisualScale, 35, deskFront.transform);
                if (seat == 6)
                {
                    memorial = new GameObject("Rabbit's untouched things");
                    memorial.transform.SetParent(parent.transform);
                    AddSprite("Cup", "ToiletBreakV02/Pixel/Mug", new Vector3(-.78f, 1.45f), .48f, 36, memorial.transform);
                    AddSprite("File", "ToiletBreakV02/Pixel/Folder", new Vector3(.12f, 1.31f), .36f, 36, memorial.transform);
                    memorial.SetActive(false);
                }
            }
            for (int i = 0; i < 7; i++)
            {
                animalRenderers[i] = AddSprite(SeatRules.Names[i] + " / movable employee",
                    DDayArt.SeatedEmployee(i), Vector3.zero, 1.10f, 20, worldRoot);
                animalObjects[i] = animalRenderers[i].gameObject;
                selectionOutlines[i] = AddSprite("Selection Outline / employee card",
                    DDayArt.SeatedEmployee(i), Vector3.zero, 1.065f, 19,
                    animalObjects[i].transform);
                selectionOutlines[i].enabled = false;
                var props = new GameObject("PersonalProps / " + SeatRules.Names[i]);
                props.transform.SetParent(worldRoot);
                personalPropRoots[i] = props.transform;
                AddSprite("CharacterDeskProps / visual only", DDayArt.PersonalProp(i),
                    new Vector3(-.79f, 0, 0), .84f, 26, props.transform);
            }
            ReflowOfficeSafeArea();
        }

        void ReflowOfficeSafeArea()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            // Camera, IMGUI and Canvas share one undistorted 16:9 design space.
            float scale = DDayUiSpace.Scale;
            Vector2 offset = DDayUiSpace.Offset;
            gameCamera.rect = new Rect(offset.x / Screen.width, offset.y / Screen.height,
                1920f * scale / Screen.width, 1080f * scale / Screen.height);
            float aspect = 16f / 9f;
            float halfWidth = gameCamera.orthographicSize * aspect;
            float virtualPixelsPerWorld = 1920f / (2f * halfWidth);
            float deskWidth = DDayArt.OfficeDesk == null ? 2.52f
                : DDayArt.OfficeDesk.bounds.size.x * DeskVisualScale;
            float available = OfficeSafeRight - OfficeSafeLeft - 2f * OfficeSafeMargin;
            officeContentScale = Mathf.Min(1f, (available - 3f * 16f) /
                (4f * deskWidth * virtualPixelsPerWorld));
            float deskHalf = deskWidth * officeContentScale * virtualPixelsPerWorld * .5f;
            float first = OfficeSafeLeft + OfficeSafeMargin + deskHalf;
            float last = OfficeSafeRight - OfficeSafeMargin - deskHalf;
            for (int seat = 0; seat < seatRoots.Length; seat++)
            {
                int column = seat % 4;
                int row = seat / 4;
                float virtualX = Mathf.Lerp(first, last, column / 3f);
                Vector3 center = new Vector3((virtualX / 960f - 1f) * halfWidth,
                    row == 0 ? -1.05f : -3.51f, 0);
                seatPositions[seat] = center;
                if (seatRoots[seat] == null) continue;
                seatRoots[seat].position = center;
                seatRoots[seat].localScale = Vector3.one * officeContentScale;
            }
        }

        SpriteRenderer AddSprite(string name, Sprite sprite, Vector3 position,
            float scale, int order, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = position;
            go.transform.localScale = Vector3.one * scale;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            if (sprite == null) Debug.LogWarning("D-DAY missing new sprite: " + name);
            renderer.sortingOrder = order;
            return renderer;
        }

        void AddZone(string name, Vector3 position, Vector2 size, Color tint, int order)
        {
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            bool tile = name.Contains("Washroom") || name.Contains("Print");
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    bool grout = tile && (x % 16 == 0 || y % 16 == 0);
                    float shade = grout ? .78f : ((x / 8 + y / 8) % 2 == 0 ? 1f : .97f);
                    texture.SetPixel(x, y, new Color(tint.r * shade,
                        tint.g * shade, tint.b * shade, tint.a));
                }
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32),
                new Vector2(.5f, .5f), 32f, 0, SpriteMeshType.FullRect);
            var zone = new GameObject(name);
            zone.transform.SetParent(worldRoot);
            zone.transform.localPosition = position;
            var renderer = zone.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = size;
            renderer.color = Color.white;
            renderer.sortingOrder = order;
        }

        SpriteRenderer AddSprite(string name, string path, Vector3 position,
            float scale, int order, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = position;
            go.transform.localScale = Vector3.one * scale;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = Resources.Load<Sprite>(path);
            if (renderer.sprite == null) Debug.LogWarning("D-DAY missing sprite: " + path);
            renderer.sortingOrder = order;
            return renderer;
        }

        void ApplyPalette(bool replay = false)
        {
            float cool = Day <= 1 || replay ? 0f : Mathf.Clamp01(.15f + .08f * (Day - 2));
            Color tint = Color.Lerp(Color.white, new Color(.76f, .85f, .97f), cool);
            foreach (var renderer in worldRoot.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer == null) continue;
                renderer.color = tint;
            }
            gameCamera.backgroundColor = Color.Lerp(new Color(.76f, .83f, .77f),
                new Color(.55f, .65f, .74f), cool);
        }
    }
}
