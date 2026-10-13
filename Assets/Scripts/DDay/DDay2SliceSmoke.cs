using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace DDay
{
    // Add to the runtime D-DAY object during Play Mode only.
    public sealed class DDay2SliceSmoke : MonoBehaviour
    {
        DDayGame game;
        DDayUI ui;
        int assertions;

        IEnumerator Start()
        {
            game = GetComponent<DDayGame>();
            ui = GetComponent<DDayUI>();
            Require(game != null && ui != null, "controller and UI available");
            yield return null;
            game.Restart();
            game.StartGame();
            yield return AdvanceToLayout(1);
            Arrange(new[] { 0, 5, 2, 3, 7, 6, 1 });
            Require(game.SeatsReady, "DAY1 remains solvable");
            game.ToggleTasks();
            game.AssignTask("d1_hr", 3);
            game.AssignTask("d1_core", 1);
            game.AssignTask("d1_review", 5);
            game.ToggleTasks();
            Require(game.Tasks.Complete, "DAY1 tasks assigned");
            game.StartWork();
            yield return AdvanceToPhase(Phase.Fatigue);
            game.SelectAnimal(0);
            game.ContinueFatigue();
            yield return new WaitForSecondsRealtime(5f);
            game.ContinueDeath();
            yield return AdvanceToLayout(2);
            yield return new WaitForEndOfFrame();
            Capture("dday-v2-day2-office.png");
            Require(game.TotalRules == 8, "DAY2 has eight rules");
            game.ToggleTasks();
            game.AssignTask("d2_hr", 3);
            game.AssignTask("d2_project", 6);
            game.AssignTask("d2_staff", 5);
            game.ToggleTasks();
            Require(game.Tasks.Complete, "DAY2 redistribution assigned");
            Require(game.AvailableConversations.Any(e => e.id == "d2_fox_shiba"), "fox and shiba bubble initially visible");

            Require(game.TryPlaceAnimal(6, 6), "move panda into empty seat");
            Require(game.Seats[6] == 6, "empty-seat move applied");
            Require(game.TryPlaceAnimal(1, 2), "swap wolf and fox");
            Require(game.Seats[1] == 2 && game.Seats[2] == 1, "swap applied");
            Require(!game.TryPlaceAnimal(1, 0), "rabbit seat rejects illegal drop");
            Arrange(new[] { -1, 1, 2, 3, 4, 7, 5 });

            Require(game.TryPlaceAnimal(2, 6), "move fox away from shiba");
            Require(!game.AvailableConversations.Any(e => e.id == "d2_fox_shiba"), "unread bubble disappears when separated");
            Require(game.TryPlaceAnimal(2, 2), "return fox near shiba");
            Require(game.AvailableConversations.Any(e => e.id == "d2_fox_shiba"), "unread bubble reappears");
            yield return new WaitForEndOfFrame();
            Capture("dday-v2-day2-bubble.png");

            Require(game.OpenConversation("d2_fox_shiba"), "fox-shiba conversation opens");
            Require(game.CurrentPhase == Phase.Conversation, "dialogue overlays office");
            Require(!game.TryPlaceAnimal(1, 5), "seat cannot move during dialogue");
            game.AdvanceConversation();
            game.AdvanceConversation();
            Require(game.ConversationLine.Contains("【招聘登记表】"), "hiring keyword appears");
            yield return new WaitForEndOfFrame();
            Capture("dday-v2-day2-red-keyword.png");
            game.AdvanceConversation();
            game.AdvanceConversation();
            Require(game.CurrentPhase == Phase.Layout, "dialogue returns to layout");
            Require(game.HasClue("hire_form") && game.ItemClueCount == 1, "hiring form becomes item clue");
            Require(game.IsConversationRead("d2_fox_shiba"), "fox-shiba read state stored");
            Require(!game.AvailableConversations.Any(e => e.id == "d2_fox_shiba"), "read conversation does not repeat");

            Require(game.TryPlaceAnimal(4, 5), "move cat next to wolf");
            Require(game.AvailableConversations.Any(e => e.id == "d2_wolf_cat"), "wolf-cat bubble appears");
            Require(game.OpenConversation("d2_wolf_cat"), "wolf-cat dialogue opens");
            while (game.CurrentPhase == Phase.Conversation) game.AdvanceConversation();
            Require(game.HasClue("coffee_cup") && game.ItemClueCount == 2, "coffee cup enters item tab");

            Require(game.TryPlaceAnimal(6, 6), "move panda next to bear");
            Require(game.AvailableConversations.Any(e => e.id == "d2_bear_panda"), "bear-panda bubble appears");
            Require(game.OpenConversation("d2_bear_panda"), "bear-panda dialogue opens");
            while (game.CurrentPhase == Phase.Conversation) game.AdvanceConversation();
            Require(game.ItemClueCount == 2, "bear-panda produces no item clue");
            Require(game.IsConversationRead("d2_bear_panda"), "bear-panda read state stored");

            ui.SetRightTab(1);
            yield return new WaitForEndOfFrame();
            Capture("dday-v2-day2-item-clues.png");
            Require(game.CurrentPhase == Phase.Layout, "can keep arranging after dialogue");
            Arrange(new[] { -1, 3, 1, 5, 4, 6, 2 });
            Require(game.SeatsReady, "DAY2 seat requirements satisfied");
            Require(game.HasClue("hire_form") && game.HasClue("coffee_cup"), "investigation remains after reaching target");
            ui.SetRightTab(0);
            yield return new WaitForEndOfFrame();
            Capture("dday-v2-day2-perfect.png");
            game.StartWork();
            Require(game.CurrentPhase == Phase.Transition, "end work begins pure transition");
            yield return new WaitForSecondsRealtime(.3f);
            yield return new WaitForEndOfFrame();
            Capture("dday-v2-day2-transition.png");
            yield return new WaitForSecondsRealtime(.8f);
            Require(game.Day == 3 && game.CurrentPhase == Phase.Narrative, "transition enters DAY3 intro");
            Require(game.HasClue("hire_form") && game.IsConversationRead("d2_fox_shiba"),
                "DAY2 clues and read state persist into next day");
            Debug.Log("[D-DAY DAY2 slice] PASS: " + assertions + " runtime assertions.");
            Destroy(this);
        }

        void Arrange(int[] target)
        {
            for (int i = 0; i < target.Length; i++)
                if (target[i] >= 0 && game.Seats[i] != target[i])
                    Require(game.TryPlaceAnimal(i, target[i]), "arrange " + SeatRules.Ids[i]);
            for (int i = 0; i < target.Length; i++)
                Require(game.Seats[i] == target[i], "seat " + SeatRules.Ids[i]);
        }

        IEnumerator AdvanceToLayout(int day)
        {
            int limit = 0;
            while (game.Day != day || game.CurrentPhase != Phase.Layout)
            {
                Require(limit++ < 600, "advance to DAY " + day);
                if (game.CurrentPhase == Phase.Narrative) game.AdvanceNarrative();
                yield return null;
            }
        }

        IEnumerator AdvanceToPhase(Phase phase)
        {
            int limit = 0;
            while (game.CurrentPhase != phase)
            {
                Require(limit++ < 600, "advance to " + phase);
                if (game.CurrentPhase == Phase.Narrative) game.AdvanceNarrative();
                yield return null;
            }
        }

        void Require(bool ok, string label)
        {
            if (!ok) throw new InvalidOperationException("[D-DAY DAY2 slice] FAIL: " + label);
            assertions++;
        }

        static void Capture(string filename)
        {
            ScreenCapture.CaptureScreenshot(Application.dataPath + "/Screenshots/" + filename);
        }
    }
}
