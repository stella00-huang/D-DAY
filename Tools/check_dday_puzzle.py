"""Exhaustively validate the D-DAY seat rules and investigation tradeoff."""
import itertools
import json
from collections import deque
from pathlib import Path

DATA = json.loads(Path("Assets/Resources/DDay/Content.json").read_text(encoding="utf-8"))
PEOPLE = ["rabbit", "wolf", "fox", "shiba", "cat", "bear", "panda"]
LIVING = PEOPLE[1:]
SEATS = [(x, y) for y in range(2) for x in range(4)]
TAGS = {0: {"window"}, 1: {"window"}, 2: {"window"}, 3: {"window"}}
ENV = {
    "toilet": [0, 2, 3, 4, 1, 3, 4, 5],
    "coffee": [3, 0, 0, 2, 4, 2, 2, 4],
    "printer": [5, 3, 1, 0, 6, 4, 2, 1],
}


def distance(a, b):
    return abs(a[0] - b[0]) + abs(a[1] - b[1])


def satisfied(rule, seats, rabbit_seat=0):
    seat = seats[rule["animal"]]
    point = SEATS[seat]
    kind, target, limit = rule["kind"], rule["target"], rule["distance"]
    if kind == "seat_is":
        return seat == int(target)
    if kind == "seat_tag":
        return target in TAGS.get(seat, set())
    if kind == "same_row":
        return point[1] == SEATS[seats[target]][1]
    if kind == "different_row":
        return point[1] != SEATS[seats[target]][1]
    if kind == "adjacent_any":
        return any(distance(point, SEATS[s]) == 1 for who, s in seats.items() if who != rule["animal"])
    gap = (ENV[target][seat] if target in ENV else
           distance(point, SEATS[rabbit_seat] if target == "rabbit_seat" else SEATS[seats[target]]))
    if kind == "adjacent":
        return gap == 1
    if kind == "not_adjacent":
        return gap != 1
    if kind in ("near_env", "near_seat", "near_person"):
        return gap <= limit
    if kind in ("far_env", "far_seat", "far_person"):
        return gap >= limit
    if kind == "exact_person":
        return gap == limit
    raise ValueError(kind)


def score(day, placement):
    return sum(satisfied(r, placement) for r in day["rules"]), len(day["rules"])


def layouts(day):
    people = PEOPLE if day["day"] == 1 else LIVING
    places = range(8) if day["day"] == 1 else range(1, 8)
    for positions in itertools.permutations(places, len(people)):
        assignment = dict(zip(people, positions))
        yield assignment, score(day, assignment)


def adjacent(layout, a, b):
    return distance(SEATS[layout[a]], SEATS[layout[b]]) == 1


def minimum_drags(day, initial):
    people = PEOPLE if day["day"] == 1 else LIVING
    start = tuple(initial[person] for person in people)
    queue = deque([(start, 0)])
    seen = {start}
    while queue:
        positions, steps = queue.popleft()
        placement = dict(zip(people, positions))
        good, total = score(day, placement)
        if 100 * good >= day["threshold"] * total:
            return steps
        for index in range(len(people)):
            for target in range(8):
                if target == positions[index] or (day["day"] > 1 and target == 0):
                    continue
                changed = list(positions)
                other = next((j for j, value in enumerate(changed) if value == target), None)
                if other is not None:
                    changed[other] = changed[index]
                changed[index] = target
                state = tuple(changed)
                if state not in seen:
                    seen.add(state)
                    queue.append((state, steps + 1))
    raise AssertionError("Unreachable day " + str(day["day"]))


def main():
    first = {"rabbit": 0, "wolf": 1, "fox": 2, "shiba": 3,
             "cat": 5, "bear": 4, "panda": 6}
    later = [
        {"wolf": 1, "fox": 2, "shiba": 3, "cat": 4, "bear": 7, "panda": 5},
        {"wolf": 1, "fox": 2, "shiba": 3, "cat": 7, "bear": 4, "panda": 6},
        {"wolf": 1, "fox": 4, "shiba": 2, "cat": 6, "bear": 7, "panda": 3},
        {"wolf": 7, "fox": 4, "shiba": 3, "cat": 2, "bear": 1, "panda": 6},
        {"wolf": 1, "fox": 4, "shiba": 3, "cat": 2, "bear": 6, "panda": 5},
    ]
    previous_example = None
    for day in DATA["days"]:
        n = day["day"]
        all_layouts = list(layouts(day))
        perfect = [p for p, result in all_layouts if result[0] == result[1]]
        passing = [p for p, result in all_layouts
                   if 100 * result[0] >= day["threshold"] * result[1]]
        initial = first if n == 1 else later[n - 2]
        initial_score = score(day, initial)
        drags = minimum_drags(day, initial)
        print(f"DAY {n}: initial {initial_score[0]}/{initial_score[1]}, "
              f"perfect={len(perfect)}, pass={len(passing)}, min-drags={drags}")
        assert perfect, f"DAY {n} has no 100% layout"
        if previous_example is not None:
            assert perfect[0] != previous_example, f"DAY {n} repeats the prior perfect example"
        previous_example = perfect[0]
        assert drags >= (2 if n == 3 else 3), f"DAY {n} too easy from start"
        assert 100 * initial_score[0] < day["threshold"] * initial_score[1], \
            f"DAY {n} initial layout already passes"
        if n == 1:
            assert len(perfect) >= 2
            print("  perfect example", perfect[0])
        if n == 2:
            print("  perfect example", perfect[0])
        if n >= 3:
            applicable = [e for e in DATA["events"] if e["day"] == n]
            investigation_only = False
            for event in applicable:
                event_pass = [p for p in passing if adjacent(p, event["a"], event["b"])]
                event_perfect = [p for p in perfect if adjacent(p, event["a"], event["b"])]
                assert event_pass, event["id"] + " cannot occur above the day threshold"
                investigation_only |= not event_perfect
                print(" ", event["id"], "pass", len(event_pass),
                      "perfect", len(event_perfect), "example", event_pass[0])
            assert investigation_only, f"DAY {n} lacks an efficiency/investigation tradeoff"
        if n == 3:
            assert any(not adjacent(p, "cat", "fox") for p in perfect)
            assert all(not adjacent(p, "cat", "fox") for p in perfect), \
                "cat + fox should demand an efficiency sacrifice"
    print("All day and event reachability checks passed.")


if __name__ == "__main__":
    main()
