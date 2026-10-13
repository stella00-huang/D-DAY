using System;
using System.Linq;
using UnityEngine;

namespace DDay
{
    [Serializable] public sealed class ABSeatDay
    {
        public int loop, day, sourceRow;
        public string[] present;
        public RuleData[] rules;
        public int[] initialSeats;
        public int initialUnmet;
    }

    [Serializable] public sealed class ABTaskBranch
    {
        public string owner, text, condition;
        public bool narration;
    }

    [Serializable] public sealed class ABTask
    {
        public string id, title, description, generic;
        public int loop, day, sourceRow;
        public bool @fixed;
        public ABTaskBranch[] branches;
    }

    [Serializable] public sealed class ABClue
    {
        public string id, category, title, location, stateText, description, condition, requiredEvent;
        public string[] actors;
        public string[] dialogue;
        public bool recordSupplement;
        public int loop, firstDay, lastDay, sourceRow;
    }

    [Serializable] public sealed class ABComicText
    {
        public string[] lines;
    }
    [Serializable] public sealed class ABFixedEvent
    {
        public string id, title, trigger, original;
        public string[] lines;
        public ABComicText[] comicTexts;
        public int loop, day, sourceRow;
    }

    [Serializable] public sealed class ABConfig
    {
        public string sourceSha256;
        public ABSeatDay[] seatDays;
        public ABClue[] clues;
        public ABTask[] tasks;
        public ABFixedEvent[] fixedEvents;

        static ABConfig cached;
        public static ABConfig Current
        {
            get
            {
                if (cached != null) return cached;
                TextAsset asset = Resources.Load<TextAsset>("DDay/ABConfig");
                if (asset == null) throw new InvalidOperationException("D-DAY: ABConfig.json missing");
                cached = JsonUtility.FromJson<ABConfig>(asset.text);
                if (cached?.seatDays?.Length != 12 || cached.clues?.Length != 40
                    || cached.tasks?.Length != 24 || cached.fixedEvents?.Length != 10)
                    throw new InvalidOperationException("D-DAY: incomplete AB workbook export");
                return cached;
            }
        }

        public ABSeatDay SeatDay(int loop, int day) => seatDays.FirstOrDefault(x =>
            x.loop == loop && x.day == day);
        public ABTask[] TasksFor(int loop, int day) => tasks.Where(x =>
            x.loop == loop && x.day == day && day < 7 && !x.@fixed).ToArray();
        public ABTask TaskFor(int loop, int day, string id) => tasks.FirstOrDefault(x =>
            x.loop == loop && x.day == day && x.id == id);
        public ABFixedEvent FixedFor(int loop, int day) => fixedEvents.FirstOrDefault(x =>
            x.loop == loop && x.day == day);
    }
}
