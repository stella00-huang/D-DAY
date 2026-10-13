using System;
using System.Collections;
using UnityEngine;

namespace DDay
{
    // Attach only to the runtime D-DAY object during Play Mode. It is never
    // part of the saved scene and uses the same placement and flow methods as UI.
    public sealed class DDaySmokeRunner : MonoBehaviour
    {
        DDayGame game;
        int checks;

        IEnumerator Start()
        {
            game = GetComponent<DDayGame>();
            if (game == null) { Debug.LogError("[D-DAY smoke] Controller missing"); yield break; }
            yield return null;
            game.Restart();
            Check(game.CurrentPhase == Phase.Title, "title");
            game.StartGame();
            yield return AdvanceToLayout(1);
            yield return new WaitForEndOfFrame();
            Capture("dday-day1-layout-final.png");
            Check(game.TotalRules == 7, "DAY 1 rules");
            Check(game.TryPlaceAnimal(6, 7), "move to empty station");
            Check(game.Seats[6] == 7, "empty move applied");
            Check(game.TryPlaceAnimal(1, 2), "swap occupied station");
            Check(game.Seats[1] == 2 && game.Seats[2] == 1, "swap applied");
            Arrange(new[] { 0, 5, 2, 3, 7, 6, 1 });
            Check(game.SeatsReady, "DAY 1 seat requirements");
            AssignDay(new[] { 3, 1, 5 });
            game.StartWork();
            yield return AdvanceToPhase(Phase.Fatigue);
            Check(game.Day == 1, "fatigue sequence");
            game.SelectAnimal(0);
            game.ContinueFatigue();
            yield return new WaitForSecondsRealtime(5.0f);
            Check(game.CurrentPhase == Phase.Death && game.DeathStage == 3, "death sequence");
            game.ContinueDeath();
            yield return AdvanceToLayout(2);
            Check(game.Seats[0] == -1 && game.RabbitSeat == 0, "rabbit seat remains empty");
            Check(!game.TryPlaceAnimal(1, 0), "rabbit seat locked");
            Arrange(new[] { -1, 3, 1, 5, 4, 6, 2 });
            Check(game.SeatsReady, "DAY 2 seat requirements");
            AssignDay(new[] { 3, 6, 5 });
            game.StartWork();
            yield return AdvanceToLayout(3);

            // DAY 3: cat + fox adjacency sacrifices a rule to unlock evidence.
            Arrange(new[] { -1, 2, 5, 6, 4, 3, 1 });
            Check(game.Satisfied < game.TotalRules, "DAY 3 optional investigation layout");
            Check(game.AvailableDialogueCount > 0, "DAY 3 adjacency event available");
            yield return new WaitForEndOfFrame();
            Capture("dday-day3-investigation-final.png");
            AssignDay(new[] { 4, 1, 3 });
            game.StartWork();
            yield return AdvanceToLayout(4);
            Check(game.HasClue("fox_returned"), "DAY 3 optional clue");
            Check(game.HasClue("coffee_source"), "DAY 3 coffee core clue");

            Arrange(new[] { -1, 5, 2, 6, 4, 3, 1 });
            Check(game.Satisfied > 0, "DAY 4 seat rules evaluated");
            AssignDay(new[] { 3, 2, 5 });
            game.StartWork();
            yield return AdvanceToLayout(5);
            Check(game.HasClue("hire_secret"), "DAY 4 hiring secret");
            Check(game.HasClue("coffee_clear"), "DAY 4 coffee cleared");

            Arrange(new[] { -1, 5, 2, 6, 7, 3, 1 });
            Check(game.Satisfied > 0, "DAY 5 seat rules evaluated");
            AssignDay(new[] { 4, 1, 6 });
            game.StartWork();
            yield return AdvanceToLayout(6);
            Check(game.HasClue("metrics_changed"), "DAY 5 company system clue");
            Check(game.HasClue("draft_to_boss"), "DAY 5 draft clue");
            Check(game.HasClue("login_1812"), "DAY 6 timeline clue");

            Arrange(new[] { -1, 3, 5, 2, 7, 6, 1 });
            Check(game.Satisfied < game.TotalRules, "DAY 6 optional investigation layout");
            Check(game.AvailableDialogueCount > 0, "DAY 6 adjacency event available");
            AssignDay(new[] { 6, 3, 5 });
            game.StartWork();
            yield return AdvanceToPhase(Phase.Suspects);
            Check(game.Day == 7, "DAY 7 reached");
            yield return new WaitForEndOfFrame();
            Capture("dday-day7-final.png");
            Check(game.HasClue("wolf_deleted"), "DAY 6 wolf confession");
            Check(game.HasClue("cat_mission"), "black cat's investigation revealed");
            Check(game.HasClue("bear_boss"), "bear's hidden role revealed");
            Check(game.HasClue("panda_knows"), "panda's knowledge revealed");
            foreach (string suspect in new[] { "fox", "wolf", "shiba", "cat", "bear", "panda" })
            {
                game.ChooseSuspect(suspect);
                Check(game.CurrentPhase == Phase.SuspectResult, suspect + " selectable");
                game.ExcludeSuspect();
            }
            Check(game.CurrentPhase == Phase.Everyone, "all suspects excluded");
            game.ChooseEveryone();
            Check(game.CurrentPhase == Phase.EveryoneResult, "everyone is incomplete answer");
            game.ChooseNoCulprit();
            Check(game.CurrentPhase == Phase.Replay, "no culprit opens replay");
            for (int i = 0; i < game.Data.replay.Length; i++) game.AdvanceReplay();
            yield return AdvanceToPhase(Phase.SecondLoop);
            Check(game.Loop == 2 && game.Seats[0] >= 0, "second loop retains rabbit");
            bool knowledgeRetained = game.HasClue("bear_boss");
            AssignDay(new[] { 1, 1, 1, 1, 1, 1 });
            game.RunSecondLoop();
            Check(game.CurrentPhase == Phase.RescueFailure, "wrong workflow fails");
            game.RetrySecondLoop();
            Check(knowledgeRetained && game.HasClue("bear_boss"), "knowledge survives retry");
            Check(game.Tasks.Count == 0, "retry clears assignments");
            Arrange(new[] { 0, 1, 3, 2, 4, 5, 6 });
            AssignDay(new[] { 3, 2, 1, 6, 4, 5 });
            yield return new WaitForEndOfFrame();
            Capture("dday-second-loop-layout.png");
            game.RunSecondLoop();
            Check(game.CurrentPhase == Phase.RescueApproval, "causal chain reaches approval");
            game.ApproveRabbitLeave(false);
            Check(game.CurrentPhase == Phase.RescueFailure, "continued work choice fails");
            game.RetrySecondLoop();
            Arrange(new[] { 0, 1, 3, 2, 4, 5, 6 });
            AssignDay(new[] { 3, 2, 1, 6, 4, 5 });
            game.RunSecondLoop();
            game.ApproveRabbitLeave(true);
            Check(game.CurrentPhase == Phase.TrueEnding, "only true ending reached");
            yield return new WaitForEndOfFrame();
            Capture("dday-ending-final.png");
            game.Restart();
            Check(game.CurrentPhase == Phase.Title, "restart works");
            Debug.Log("[D-DAY smoke] PASS: " + checks +
                " assertions, DAY 1–7 and second-loop rescue complete.");
            Destroy(this);
        }

        void AssignDay(int[] owners)
        {
            Check(game.Tasks.Cards.Length == owners.Length, "task card count");
            game.ToggleTasks();
            for (int i = 0; i < owners.Length; i++)
                game.AssignTask(game.Tasks.Cards[i].id, owners[i]);
            Check(game.Tasks.Complete, "all tasks assigned");
            game.ToggleTasks();
        }

        void Arrange(int[] target)
        {
            for (int i = 0; i < target.Length; i++)
            {
                if (target[i] < 0 || game.Seats[i] == target[i]) continue;
                Check(game.TryPlaceAnimal(i, target[i]), "place " + SeatRules.Ids[i]);
            }
            for (int i = 0; i < target.Length; i++)
                Check(game.Seats[i] == target[i], "final seat " + SeatRules.Ids[i]);
        }

        IEnumerator AdvanceToLayout(int day)
        {
            int safety = 0;
            while (game.Day != day || game.CurrentPhase != Phase.Layout)
            {
                Check(safety++ < 600, "advance to DAY " + day);
                if (game.CurrentPhase == Phase.Narrative) game.AdvanceNarrative();
                yield return null;
            }
            Debug.Log("[D-DAY smoke] DAY " + day + " layout ready");
        }

        IEnumerator AdvanceToPhase(Phase phase)
        {
            int safety = 0;
            while (game.CurrentPhase != phase)
            {
                Check(safety++ < 600, "advance to " + phase);
                if (game.CurrentPhase == Phase.Narrative) game.AdvanceNarrative();
                yield return null;
            }
        }

        void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("[D-DAY smoke] FAIL: " + label);
            checks++;
        }

        static void Capture(string filename)
        {
            ScreenCapture.CaptureScreenshot(Application.dataPath + "/Screenshots/" + filename);
        }
    }
}
