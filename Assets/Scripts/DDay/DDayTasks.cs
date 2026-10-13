using System;
using System.Collections.Generic;

namespace DDay
{
    [Serializable]
    public sealed class TaskCardData
    {
        public string id;
        public string title;
        public string description;
    }

    // Assignments are per attempt. Clues and the player's knowledge live in DDayGame.
    public sealed class TaskAssignments
    {
        readonly Dictionary<string, int> owners = new Dictionary<string, int>();
        TaskCardData[] cards = Array.Empty<TaskCardData>();

        public TaskCardData[] Cards => cards;
        public int Count => owners.Count;
        public bool Complete => owners.Count == cards.Length;

        public void Reset(TaskCardData[] next)
        {
            cards = next ?? Array.Empty<TaskCardData>();
            owners.Clear();
        }

        public int Owner(string id) => owners.TryGetValue(id, out int owner) ? owner : -1;

        public bool Assign(string id, int animal, bool rabbitAvailable)
        {
            if (animal < 0 || animal >= SeatRules.Ids.Length || (animal == 0 && !rabbitAvailable))
                return false;
            bool found = false;
            foreach (TaskCardData card in cards)
                if (card.id == id) { found = true; break; }
            if (!found) return false;
            owners[id] = animal;
            return true;
        }
    }

    public sealed class RescueAssessment
    {
        public readonly bool[] links = new bool[6];
        public int ChangedCount
        {
            get
            {
                int count = 0;
                foreach (bool link in links) if (link) count++;
                return count;
            }
        }
        public bool Ready => ChangedCount == links.Length;
        public string Feedback
        {
            get
            {
                string[] messages = {
                    "兔子仍在替 HR 扛着本该交回去的事务。",
                    "狐狸仍有机会单独找到灰狼。",
                    "灰狼没有借正常工作接触，错过了兔子的异常。",
                    "核心任务没有转交，兔子的负荷仍然过高。",
                    "黑猫没能及时看出：兔子需要停工，而不是再喝咖啡。",
                    "异常没能传到可以叫停工作的棕熊那里。"
                };
                for (int i = 0; i < links.Length; i++) if (!links[i]) return messages[i];
                return "工作链已经改变。现在只剩下老板的决定。";
            }
        }
    }

    public static class RescueLogic
    {
        public static RescueAssessment Evaluate(int[] seats, TaskAssignments tasks)
        {
            var result = new RescueAssessment();
            result.links[0] = tasks.Owner("hr") == SeatRules.Index("shiba");
            result.links[1] = tasks.Owner("coordination") == SeatRules.Index("fox")
                && SeatRules.Adjacent(seats, "fox", "shiba")
                && !SeatRules.Adjacent(seats, "fox", "wolf");
            result.links[2] = tasks.Owner("confirmation") == SeatRules.Index("wolf")
                && SeatRules.Adjacent(seats, "wolf", "rabbit");
            result.links[3] = tasks.Owner("core") == SeatRules.Index("panda");
            result.links[4] = tasks.Owner("wellbeing") == SeatRules.Index("cat")
                && SeatRules.Distance(seats[SeatRules.Index("cat")], seats[0]) <= 2
                && result.links[0] && result.links[3];
            result.links[5] = tasks.Owner("review") == SeatRules.Index("bear")
                && SeatRules.Distance(seats[SeatRules.Index("bear")], seats[SeatRules.Index("cat")]) <= 2
                && result.links[4];
            return result;
        }
    }
}
