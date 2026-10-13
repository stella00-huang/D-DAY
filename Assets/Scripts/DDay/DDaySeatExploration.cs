using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DDay
{
    // A2 is independent of mandatory A1. Worksheet row IDs prevent farming.
    public sealed partial class DDayGame
    {
        readonly HashSet<string> triggeredSeatEvents = new HashSet<string>();
        public IReadOnlyCollection<string> TriggeredSeatEvents => triggeredSeatEvents;

        void LoadSeatExplorations()
        {
            ABClue[] clues = ABConfig.Current.clues;
            if (clues.Length != 40 || clues.Select(x => x.id).Distinct().Count() != 40)
                throw new InvalidOperationException("D-DAY: A2 IDs incomplete or duplicated");
        }

        public ABClue[] FindSeatExplorations(int clickedAnimal = -1)
        {
            return ABConfig.Current.clues.Where(entry => !triggeredSeatEvents.Contains(SeatClaimKey(entry))
                && (entry.loop == 0 || entry.loop == Loop)
                && Day >= entry.firstDay && Day <= entry.lastDay
                && (string.IsNullOrEmpty(entry.requiredEvent)
                    || completedFixedEvents.Contains(entry.requiredEvent))
                && SeatExplorationMatches(entry)).ToArray();
        }

        string SeatClaimKey(ABClue entry) => entry.loop == 0 ? entry.id + "@L" + Loop : entry.id;

        bool SeatExplorationMatches(ABClue entry)
        {
            int[] actors = entry.actors.Select(SeatRules.Index).ToArray();
            if (actors.Any(index => index < 0 || Seats[index] < 0)) return false;
            int first = actors[0], second = actors.Length > 1 ? actors[1] : -1;
            bool adjacent = second >= 0 && SeatRules.Distance(Seats[first], Seats[second]) == 1;
            switch (entry.condition)
            {
                case "adjacent": return adjacent;
                case "after_promotion": return Loop == 2 && Day >= 5;
                case "not_adjacent_no_sisy_task":
                    return second >= 0 && !adjacent && Tasks.Complete
                        && Tasks.Cards.All(x => Tasks.Owner(x.id) != 0);
                case "near": return second >= 0 && SeatRules.Distance(Seats[first], Seats[second]) <= 2;
                case "edge_not_b3": return SeatRules.HasTag(Seats[first], "edge")
                    && SeatRules.Distance(Seats[first], 6) != 1;
                case "center": return SeatRules.HasTag(Seats[first], "center");
                case "bear_center": return SeatRules.HasTag(Seats[SeatRules.Index("bear")], "center");
                case "printer": return SeatRules.HasTag(Seats[first], "near_printer");
                case "boss": return SeatRules.HasTag(Seats[first], "near_boss");
                case "either_boss": return SeatRules.HasTag(Seats[first], "near_boss")
                    || second >= 0 && SeatRules.HasTag(Seats[second], "near_boss");
                case "lounge": return SeatRules.HasTag(Seats[first], "near_lounge");
                case "far_coffee": return SeatRules.HasTag(Seats[first], "far_coffee");
                case "not_coffee": return !SeatRules.HasTag(Seats[first], "near_coffee");
                case "seat_b3": return Seats[first] == 6;
                case "adjacent_or_printer": return adjacent || SeatRules.HasTag(Seats[first], "near_printer");
                case "adjacent_or_lounge": return adjacent || SeatRules.HasTag(Seats[SeatRules.Index("panda")], "near_lounge");
                // A desk inspection is offered by its owner's bubble; availability
                // never grants the record. Only the explicit click does that.
                case "desk": return true;
                case "desk_or_lounge": return true;
                case "center_or_desk": return true;
                case "desk_x_prereq": return records.Any(x => x.loop == Loop &&
                    (x.id == "A2_R09" || x.id == "A2_R25" || x.id == "A2_R27"
                    || x.id == "A2_R28" || x.id == "x_anomaly"));
                default: throw new InvalidOperationException("Unknown A2 condition " + entry.condition);
            }
        }

        void FinishSeatExploration(ABClue entry)
        {
            if (!triggeredSeatEvents.Add(SeatClaimKey(entry))) return;
            if (entry.id == "A2_R37") SisyTrustsMetis = true;
            if (entry.id == "A2_R27" || entry.id == "A2_R28") XInfoToMetis = true;
            // Each loop has its own claim, while the existing cross-loop book keeps one unique ID.
            Record(entry.id, entry.category, entry.title, entry.description, true);
            SaveGame();
        }

        public int ExplorationHost(ABClue entry)
        {
            return entry.condition == "bear_center" ? SeatRules.Index("bear")
                : SeatRules.Index(entry.actors[0]);
        }

        public bool OpenSeatExploration(int animal)
        {
            if (CurrentPhase != Phase.Layout || DragAnimal >= 0 || BoardOpen || TasksOpen
                || SaveSlotsOpen || GetComponent<DDayUI>().OfficeSettingsOpen) return false;
            ABClue entry = FindSeatExplorations().FirstOrDefault(x => ExplorationHost(x) == animal);
            if (entry == null) return false;
            string[] lines = entry.dialogue ?? Array.Empty<string>();
            ShowNarrative("探索 · " + entry.title, lines, () => {
                FinishSeatExploration(entry);
                CurrentPhase = Phase.Layout;
                SaveGame();
            }, entry.id, SeatRules.Ids[animal]);
            return true;
        }

    }
}
