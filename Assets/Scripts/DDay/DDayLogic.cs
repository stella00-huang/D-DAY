using System;
using UnityEngine;

namespace DDay
{
    [Serializable] public sealed class Database
    {
        public DayData[] days;
        public DialogueData[] events;
        public ClueData[] clues;
        public ProfileData[] profiles;
        public string[] replay;
        public TaskCardData[] rescueTasks;
    }

    [Serializable] public sealed class DayData
    {
        public int day;
        public string title;
        public string[] intro;
        public string[] outro;
        public RuleData[] rules;
        public TaskCardData[] tasks;
        public string[] guaranteedClues;
    }

    [Serializable] public sealed class RuleData
    {
        public string animal;
        public string kind;
        public string target;
        public int distance;
        public string text;
        public string group;
    }

    [Serializable] public sealed class DialogueData
    {
        public string id;
        public int day;
        public string a;
        public string b;
        public string title;
        public string[] lines;
        public string reward;
        public int priority;
        public string[] requires;
        public string taskId;
        public string taskOwner;
    }

    [Serializable] public sealed class ClueData
    {
        public string id;
        public string title;
        public string description;
        public string category;
        public string person;
    }

    [Serializable] public sealed class ProfileData
    {
        public string id;
        public string name;
        public string @public;
        public string hidden;
        public string clue;
    }

    public static class SeatRules
    {
        public static readonly string[] Ids = { "rabbit", "wolf", "fox", "shiba", "cat", "bear", "panda" };
        public static readonly string[] Names = { "兔子", "灰狼", "狐狸", "柴犬", "黑猫", "棕熊", "熊猫" };
        public static readonly string[] CharacterCodes = { "SISY", "ORIN", "NARCIS", "ECHO", "METIS", "THANE", "CASSIA" };
        public static string CharacterCode(int index) => index >= 0 && index < CharacterCodes.Length
            ? CharacterCodes[index] : "";
        public static string DisplayName(string id)
        {
            int index = Index(id);
            return index < 0 ? id : Names[index] + " " + CharacterCodes[index];
        }
        // Format authored dialogue at the UI boundary; keep story data and save IDs unchanged.
        public static string PublicText(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            for (int i = 0; i < CharacterCodes.Length; i++)
            {
                string pattern = "(?<!" + System.Text.RegularExpressions.Regex.Escape(Names[i] + " ")
                    + ")(?<![A-Za-z0-9_])" + CharacterCodes[i] + "(?![A-Za-z0-9_])";
                value = System.Text.RegularExpressions.Regex.Replace(value, pattern,
                    Names[i] + " " + CharacterCodes[i]);
            }
            return value;
        }
        public static readonly Vector2Int[] Grid = {
            new Vector2Int(0, 0), new Vector2Int(1, 0),
            new Vector2Int(2, 0), new Vector2Int(3, 0),
            new Vector2Int(0, 1), new Vector2Int(1, 1),
            new Vector2Int(2, 1), new Vector2Int(3, 1)
        };
        // Fixed seat graph. Visual sprites never participate in rule evaluation.
        static readonly int[][] Neighbours = {
            new[] { 1, 4 }, new[] { 0, 2, 5 },
            new[] { 1, 3, 6 }, new[] { 2, 7 },
            new[] { 0, 5 }, new[] { 1, 4, 6 },
            new[] { 2, 5, 7 }, new[] { 3, 6 }
        };
        // Rows map to the visible office: washroom upper left, coffee upper
        // middle, printer upper right; lower desks are farther from that wall.
        static readonly int[] RestroomDistance = { 0, 1, 2, 3, 2, 3, 4, 5 };
        static readonly int[] CoffeeDistance = { 1, 0, 0, 1, 3, 2, 2, 3 };
        static readonly int[] PrinterDistance = { 3, 2, 1, 0, 5, 4, 2, 3 };
        static readonly int[] LoungeDistance = { 2, 3, 4, 5, 0, 1, 2, 3 };
        static readonly int[] BossDistance = { 5, 4, 3, 2, 3, 2, 1, 0 };

        public static int FacilityDistance(int seat, string facility)
        {
            if (seat < 0 || seat >= 8) return 99;
            switch (facility)
            {
                case "toilet": return RestroomDistance[seat];
                case "coffee": return CoffeeDistance[seat];
                case "printer": return PrinterDistance[seat];
                case "lounge": return LoungeDistance[seat];
                case "boss": return BossDistance[seat];
                default: return 99;
            }
        }

        public static bool IsWindowSeat(int seat) => seat >= 0 && seat < 4;
        public static string SeatCode(int seat) => seat < 0 || seat >= 8 ? "—"
            : (seat < 4 ? "A" : "B") + (seat % 4 + 1);

        public static string ZoneName(int seat)
        {
            switch (seat)
            {
                case 0: return "边缘工位";
                case 1: return "咖啡区侧";
                case 2: return "打印动线";
                case 3: return "打印区侧";
                case 4: case 5: return "休息区侧";
                case 6: case 7: return "老板办公室侧";
                default: return "未分区";
            }
        }

        // AB uses discrete influence zones: coffee across the front row, printer
        // in the central/right facility aisle, lounge left and boss office below.
        // The far-right restroom is scenery; current AB has no restroom rules.
        public static bool HasTag(int seat, string tag)
        {
            if (seat < 0 || seat >= Grid.Length) return false;
            switch (tag)
            {
                case "near_boss": return seat == 5 || seat == 6 || seat == 7;
                case "near_printer": return seat == 2 || seat == 3 || seat == 6;
                case "near_lounge": return seat == 4 || seat == 5 || seat == 6;
                case "near_coffee": return seat >= 0 && seat <= 3;
                case "far_coffee": return seat == 4 || seat == 7;
                case "far_boss": return seat == 0 || seat == 1 || seat == 2 || seat == 4;
                case "near_toilet": return seat == 0;
                case "edge": return seat == 0 || seat == 3 || seat == 4 || seat == 7;
                case "center": case "high_traffic": return seat == 1 || seat == 2 || seat == 5 || seat == 6;
                case "window": return IsWindowSeat(seat);
                default: return false;
            }
        }

        public static int Index(string id)
        {
            for (int i = 0; i < Ids.Length; i++) if (Ids[i] == id) return i;
            return -1;
        }

        public static int Distance(int a, int b)
        {
            if (a < 0 || b < 0 || a >= Grid.Length || b >= Grid.Length) return 99;
            if (a == b) return 0;
            var distance = new int[8];
            for (int i = 0; i < distance.Length; i++) distance[i] = 99;
            var queue = new System.Collections.Generic.Queue<int>();
            distance[a] = 0;
            queue.Enqueue(a);
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                foreach (int next in Neighbours[current])
                {
                    if (distance[next] <= distance[current] + 1) continue;
                    distance[next] = distance[current] + 1;
                    if (next == b) return distance[next];
                    queue.Enqueue(next);
                }
            }
            return 99;
        }

        public static bool Adjacent(int[] seats, string a, string b)
        {
            int ai = Index(a), bi = Index(b);
            return ai >= 0 && bi >= 0 && Distance(seats[ai], seats[bi]) == 1;
        }

        public static bool Satisfied(RuleData rule, int[] seats, int rabbitSeat)
        {
            int index = Index(rule.animal);
            if (index < 0 || seats[index] < 0) return false;
            int seat = seats[index];
            if (rule.kind == "seat_is")
                return int.TryParse(rule.target, out int wanted) && seat == wanted;
            if (rule.kind == "seat_tag")
                return HasTag(seat, rule.target);
            if (rule.kind == "same_row" || rule.kind == "different_row")
            {
                int otherIndex = Index(rule.target);
                if (otherIndex < 0 || seats[otherIndex] < 0) return false;
                bool same = Grid[seat].y == Grid[seats[otherIndex]].y;
                return rule.kind == "same_row" ? same : !same;
            }
            if (rule.kind == "adjacent_any")
            {
                for (int other = 0; other < seats.Length; other++)
                    if (other != index && Distance(seat, seats[other]) == 1) return true;
                return false;
            }

            int gap;
            if (rule.target == "toilet" || rule.target == "coffee" || rule.target == "printer")
                gap = FacilityDistance(seat, rule.target);
            else if (rule.target == "rabbit_seat")
                gap = Distance(seat, rabbitSeat);
            else
            {
                int other = Index(rule.target);
                gap = other < 0 ? 99 : Distance(seat, seats[other]);
            }

            switch (rule.kind)
            {
                case "adjacent": return gap == 1;
                case "not_adjacent": return gap != 1;
                case "near_env": case "near_seat": return gap <= rule.distance;
                case "far_env": case "far_seat": return gap >= rule.distance;
                case "near_person": return gap <= rule.distance;
                case "far_person": return gap >= rule.distance;
                case "exact_person": return gap == rule.distance;
                default: throw new ArgumentException("Unknown seat rule: " + rule.kind);
            }
        }

        public static int CountSatisfied(DayData day, int[] seats, int rabbitSeat)
        {
            int count = 0;
            foreach (var rule in day.rules)
                if (Satisfied(rule, seats, rabbitSeat)) count++;
            return count;
        }

    }
}
