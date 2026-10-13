"""Read the complete authoritative workbook, verify mappings, and update B1 direction metadata.

Never edits Excel, portraits, comic art, ending configuration, or player saves.
Run with the same Python runtime as dday_ab_export.py after exporting ABConfig.
"""
import argparse
import hashlib
import json
import re
from pathlib import Path

import openpyxl

from dday_ab_export import IDS, RECORD_SUPPLEMENTS, SOURCE, player_clue_text

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Docs/ContentUpdate_20261012"
TURN = r"\r?\n|\s+(?=(?:SISY|ORIN|NARCIS|ECHO|METIS|CASSIA|THANE|老板)[：:])"


def normal(text):
    return re.sub(r"\s+", "", text or "")


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def update_directions(config):
    path = ROOT / "Assets/Resources/DDay/DialoguePresentation.json"
    data = json.loads(path.read_text(encoding="utf-8"))
    directions = [d for d in data["directions"] if not d["eventId"].startswith("B1_")]
    seen = set()
    for task in config["tasks"]:
        for branch in task["branches"]:
            # The SISY-assigned branch has the same neutral direction at index zero.
            for index, line in enumerate(x for x in re.split(TURN, branch["text"]) if x.strip()):
                event = f"B1_L{task['loop']}_D{task['day']}_{task['id']}_{branch['owner']}"
                if (event, index) in seen:
                    continue
                seen.add((event, index))
                directions.append({"eventId": event, "lineIndex": index,
                    "speakerId": "", "focusId": branch["owner"], "expressionId": "neutral",
                    "portraitPosition": "left", "switchSpeaker": True, "effect": "fade",
                    "narration": branch.get("narration", False)})
    data["directions"] = directions
    text = json.dumps(data, ensure_ascii=False, indent=2)
    if text != path.read_text(encoding="utf-8"):
        path.write_text(text, encoding="utf-8")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("workbook", nargs="?", type=Path, default=SOURCE)
    parser.add_argument("--update-directions", action="store_true")
    args = parser.parse_args()
    wb = openpyxl.load_workbook(args.workbook, data_only=False)
    full = {s.title: [list(row) for row in s.iter_rows(values_only=True)] for s in wb}
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / "WorkbookFull.json").write_text(json.dumps({"source": str(args.workbook),
        "sha256": sha(args.workbook), "sheets": full}, ensure_ascii=False, indent=2), encoding="utf-8")
    config = json.loads((ROOT / "Assets/Resources/DDay/ABConfig.json").read_text(encoding="utf-8"))
    checks = []
    def check(ok, label):
        checks.append({"passed": bool(ok), "check": label})
        if not ok:
            raise AssertionError(label)
    check(config["sourceSha256"] == sha(args.workbook), "Runtime data uses current desktop workbook SHA256")
    check([len(config[k]) for k in ("seatDays", "clues", "tasks", "fixedEvents")] == [12,40,24,10],
          "12 A1 / 40 A2 / 24 B1 / 10 B2 mapped")
    mappings = []
    for clue in config["clues"]:
        source = full["A2-即时线索"][clue["sourceRow"]-1]
        check(clue["title"] == source[3].strip(), f"{clue['id']} exact record title")
        expected = (source[7] or "").strip() or RECORD_SUPPLEMENTS.get(clue["sourceRow"], "")
        check(clue["description"] == expected, f"{clue['id']} exact record or authorized supplement")
        check(normal("".join(clue["dialogue"])) == normal(player_clue_text(clue["sourceRow"],source[6].strip())),
              f"{clue['id']} all authored exploration words retained")
        mappings.append({"system":"A2", "row":clue["sourceRow"], "id":clue["id"],
            "loop":clue["loop"], "window":[clue["firstDay"],clue["lastDay"]],
            "condition":clue["condition"], "requiredEvent":clue["requiredEvent"],
            "recordSupplement":clue["recordSupplement"]})
    missing = []
    for task in config["tasks"]:
        source = full["B1-每日任务与剧情"][task["sourceRow"]-1]
        check(task["title"] == source[2].strip(), f"B1 row {task['sourceRow']} exact task title")
        check(task["description"] == "", f"B1 row {task['sourceRow']} no internal question in UI description")
        texts = [normal(b["text"]) for b in task["branches"]]
        if task["day"] < 7:
            for line in (source[4] or "").splitlines():
                line = line.strip()
                if not line or line.startswith(("【关键", "【条件】", "━━", "——不让任何人说出")) \
                    or "其他角色（带过）" in line or "本任务无关键线" in line:
                    continue
                check(any(normal(line) in text for text in texts),
                      f"B1 row {task['sourceRow']} authored line preserved: {line[:32]}")
            roster = next(d["present"] for d in config["seatDays"] if d["loop"]==task["loop"] and d["day"]==task["day"])
            if not task["fixed"]:
                missing += [{"row":task["sourceRow"],"owner":owner,"fallback":task["generic"]}
                    for owner in roster if owner not in {b["owner"] for b in task["branches"]}]
        mappings.append({"system":"B1", "row":task["sourceRow"], "id":task["id"],
            "loop":task["loop"], "day":task["day"], "fixed":task["fixed"],
            "owners":[b["owner"] for b in task["branches"]]})
    for event in config["fixedEvents"]:
        source = full["B2-固定事件"][event["sourceRow"]-1]
        check(event["original"] == source[4].strip(), f"{event['id']} complete fixed-event original retained")
        if event["comicTexts"] is not None:
            body = "".join(re.sub(r"^[①-⑨][^：]*：", "", line).strip()
                           for line in event["original"].splitlines() if line.strip())
            check(normal(body) == normal("".join(line for panel in event["comicTexts"] for line in panel["lines"])),
                  f"{event['id']} full original bound to existing comic, no deleted prose")
        mappings.append({"system":"B2", "row":event["sourceRow"], "id":event["id"],
            "loop":event["loop"], "day":event["day"]})
    player_text = "\n".join([c["title"]+c["description"]+"".join(c["dialogue"]) for c in config["clues"]]
        + [t["title"]+t["description"]+"".join(b["text"] for b in t["branches"]) for t in config["tasks"]])
    for row in full["B1-每日任务与剧情"][3:]:
        for index in (3,5,7):
            internal = (row[index] or "").strip()
            if len(internal)>15:
                check(internal not in player_text, f"B1 internal column {index+1} not bound to public text")
    check([t["sourceRow"] for t in config["tasks"] if t["fixed"]] == [6], "Only authored ORIN inquiry is fixed")
    check(sum(t["day"]==7 for t in config["tasks"])==2, "Both no-B1 Day7 records retained as data only")
    backup = ROOT / "Backups/ContentUpdate_20261012"
    old = json.loads((backup / "Assets/Resources/DDay/ABConfig.json").read_text(encoding="utf-8"))
    check(config["seatDays"] == old["seatDays"], "A1 latest table unchanged: all rules/presence/start positions identical")
    before = json.loads((backup / "Before.json").read_text(encoding="utf-8"))
    changed = [b["path"].replace("\\","/") for b in before if sha(ROOT/b["path"]) != b["sha256"]]
    for path in ["Assets/Scripts/DDay/HEEndingPlayer.cs", "Assets/Scripts/DDay/HEEndingConfig.cs",
                 "Assets/Scripts/DDay/EndingSubtitles.cs", "Assets/Scripts/DDay/DDayFinalDecisionUI.cs",
                 "Assets/Resources/DDay/ComicPresentation.json"]:
        check(sha(ROOT/path)==sha(backup/path), f"Preserved existing presentation: {path}")
    for day in (1,2,4,6):
        path=f"Assets/Resources/DDay/Story/Day{day}/Day{day}ComicPresentation.json"
        check(sha(ROOT/path)==sha(backup/path), f"Unchanged comic image/focus/effects configuration Day{day}")
    if args.update_directions:
        update_directions(config)
    artifact={"source":str(args.workbook),"sha256":sha(args.workbook),"checks":checks,
        "mappings":mappings,"missingAuthoredOwnerBranches":missing,"modifiedBackedUpFiles":changed}
    (OUT/"MappingAudit.json").write_text(json.dumps(artifact,ensure_ascii=False,indent=2),encoding="utf-8")
    print(f"PASS: {len(checks)} source/content/integrity checks; missing authored owner branches: {missing}")


if __name__ == "__main__":
    main()
