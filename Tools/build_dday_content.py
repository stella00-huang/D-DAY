"""Author the small, editable D-DAY demo data file.

Run deliberately; the game loads the generated JSON and never runs this tool.
"""
import json
from pathlib import Path

OUT = Path("Assets/Resources/DDay/Content.json")


def rule(animal, kind, target, distance, text, group="工作需求"):
    return dict(animal=animal, kind=kind, target=target, distance=distance,
                text=text, group=group)


day2_rules = [
    rule("wolf", "far_seat", "rabbit_seat", 2, "不坐在兔子的空工位旁", "心理需求"),
    rule("fox", "seat_tag", "window", 0, "坐在窗边"),
    rule("fox", "not_adjacent", "wolf", 0, "避开灰狼", "心理需求"),
    rule("shiba", "adjacent", "fox", 0, "想坐在狐狸旁边", "心理需求"),
    rule("shiba", "far_seat", "rabbit_seat", 2, "避开兔子的空工位", "心理需求"),
    rule("cat", "far_env", "coffee", 3, "远离咖啡机"),
    rule("bear", "far_env", "toilet", 3, "远离厕所"),
    rule("panda", "near_env", "coffee", 1, "靠近咖啡机"),
]

day3_rules = day2_rules + [
    rule("cat", "far_person", "fox", 3, "观察狐狸，但保持至少三格", "心理需求"),
    rule("shiba", "near_person", "panda", 2, "与熊猫距离不超过两格"),
]

day4_rules = day3_rules + [
    rule("fox", "different_row", "shiba", 0, "怕引人注意，避开柴犬所在排", "心理需求"),
    rule("cat", "same_row", "wolf", 0, "想在同一排观察灰狼", "心理需求"),
]

day5_rules = day4_rules.copy()
day5_rules[8] = rule("cat", "exact_person", "fox", 2,
                     "想看清狐狸，但保持两格距离", "心理需求")

day6_rules = day5_rules.copy()
day6_rules[9] = rule("shiba", "adjacent", "wolf", 0,
                     "想坐近灰狼，问清那晚的事", "心理需求")
day6_rules[10] = rule("fox", "different_row", "wolf", 0,
                      "避开灰狼所在排", "心理需求")
day6_rules[11] = rule("cat", "same_row", "fox", 0,
                      "想在同一排观察狐狸", "心理需求")

day1 = [
    rule("rabbit", "near_env", "toilet", 1, "靠近厕所"),
    rule("wolf", "far_env", "toilet", 3, "远离厕所"),
    rule("fox", "seat_tag", "window", 0, "坐在窗边"),
    rule("shiba", "near_env", "printer", 1, "靠近打印机"),
    rule("cat", "far_env", "coffee", 3, "远离咖啡机"),
    rule("bear", "far_env", "toilet", 3, "远离厕所"),
    rule("panda", "near_env", "coffee", 1, "靠近咖啡机"),
]


def day(number, title, threshold, intro, outro, rules, guaranteed):
    return dict(day=number, title=title, threshold=threshold, intro=intro,
                outro=outro, rules=rules, guaranteedClues=guaranteed)


days = [
    day(1, "100%", 100,
        ["每个员工都有自己的办公习惯。", "拖动他们更换工位；放到有人坐的工位可以交换。",
         "满足所有需求，让办公室达到100%效率。"],
        ["18:07", "兔子倒下了。", "她没有再醒来。"], day1, []),
    day(2, "嫌疑人", 100,
        ["警方没有发现明显外伤，也没有证据证明谋杀。", "兔子的工位从今天起不再使用。",
         "心理需求出现了。但公司仍要运转。"],
        ["死亡前一天，兔子与狐狸公开争吵。", "所有人都看见了。",
        "主要嫌疑：狐狸。"], day2_rules, ["fox_argument"]),
    day(3, "咖啡", 80,
        ["相邻员工会谈话。今天新增人物距离规则；80%效率就能结束。"],
        ["兔子最后喝过一杯咖啡，来源指向柴犬。"],
        day3_rules, ["last_coffee", "coffee_source"]),
    day(4, "狐狸的秘密", 80,
        ["新增同排与不同排规则。人物档案开放。"],
        ["柴犬曾帮狐狸绕过招聘筛选；兔子发现了。",
         "检测结果：咖啡未发现异常。"],
        day4_rules, ["hire_secret", "coffee_clear"]),
    day(5, "兔子在调查谁？", 80,
        ["黑猫想看清狐狸，却坚持相隔两格。兔子留下的文件还没有看完。"],
        ["绩效记录曾被人为调整。未发送的草稿收件人是老板。",
         "兔子调查的不只是狐狸。"],
        day5_rules, ["metrics_changed", "draft_to_boss"]),
    day(6, "18:12", 80,
        ["最后一次综合布局。狐狸避开灰狼，黑猫留意狐狸。",
         "18:07兔子倒下；18:12电脑被再次登录。"],
        ["灰狼承认18:12登录并删除部分材料；他没有杀兔子。",
         "兔子与狐狸争吵，也曾是她转移注意的选择。"],
        day6_rules, ["wolf_deleted", "rabbit_strategy"]),
]


def clue(id, title, description, category, person):
    return dict(id=id, title=title, description=description,
                category=category, person=person)


clues = [
    clue("hire_form", "招聘登记表", "兔子死亡前似乎调查过狐狸的入职资料。", "物证", "fox"),
    clue("coffee_cup", "咖啡杯", "兔子死亡前曾喝过咖啡。", "物证", "cat"),
    clue("fox_argument", "公开争吵", "死亡前一天，兔子与狐狸在所有人面前争吵。", "时间线", "fox"),
    clue("fox_returned", "狐狸曾返回办公室", "狐狸说是回来取东西；具体时间说不清。", "人物", "fox"),
    clue("rabbit_search", "兔子在调查", "兔子死亡前仍在查一件事。", "人物", "rabbit"),
    clue("last_coffee", "最后饮品：咖啡", "黑猫递给兔子一杯咖啡；杯子留在桌上。", "物证", "cat"),
    clue("coffee_source", "咖啡来源：柴犬", "黑猫使用了柴犬放在办公室的咖啡。", "物证", "shiba"),
    clue("hire_secret", "招聘筛选被绕过", "狐狸学历未达到门槛，柴犬曾帮他绕过部分招聘筛选。", "人物", "fox"),
    clue("wolf_threat", "晋升交易", "狐狸利用地下恋情威胁灰狼退出晋升竞争。", "人物", "wolf"),
    clue("coffee_clear", "咖啡未发现异常", "现有调查没有发现咖啡被做过手脚。", "物证", "cat"),
    clue("metrics_changed", "绩效记录被调整", "部分绩效评价和工作安排曾被人为调整。", "物证", "rabbit"),
    clue("draft_to_boss", "未发送草稿", "兔子的草稿准备交给老板，内容指向效率管理系统。", "物证", "rabbit"),
    clue("wolf_love", "地下恋人", "灰狼与兔子一直在秘密交往。", "人物", "wolf"),
    clue("login_1812", "18:12再次登录", "兔子倒下五分钟后，她的电脑被再次登录。", "时间线", "rabbit"),
    clue("wolf_deleted", "灰狼删除材料", "灰狼承认登录兔子电脑，删除了部分举报材料。", "时间线", "wolf"),
    clue("wolf_avoided", "灰狼的回避", "灰狼看到兔子不适，却怕恋情曝光，没有走过去。", "人物", "wolf"),
    clue("bear_pressure", "项目催促", "棕熊和熊猫只想赶上项目截止；兔子当时明显不舒服。", "时间线", "bear"),
    clue("rabbit_strategy", "主动制造争吵", "兔子利用与狐狸的公开争吵转移注意，继续调查公司。", "人物", "rabbit"),
]


def event(id, day, a, b, title, lines, reward, priority=10, requires=None):
    return dict(id=id, day=day, a=a, b=b, title=title, lines=lines,
                reward=reward, priority=priority, requires=requires or [])


events = [
    event("d2_fox_shiba", 2, "fox", "shiba", "招聘登记表", [
        "狐狸：兔子死前是不是找过你？",
        "柴犬：问了一些以前的事。",
        "狐狸：包括那份【招聘登记表】？",
        "柴犬：……你怎么知道？"], "hire_form", 30),
    event("d2_wolf_cat", 2, "wolf", "cat", "咖啡杯", [
        "黑猫：你那天一直在看兔子。",
        "灰狼：没有。",
        "黑猫：有。",
        "灰狼：……",
        "黑猫：兔子喝【咖啡】的时候，你也在看。"], "coffee_cup", 25),
    event("d2_bear_panda", 2, "bear", "panda", "项目截止", [
        "棕熊：我还是觉得那天不该催。",
        "熊猫：项目当天就截止。",
        "棕熊：兔子脸色已经很差了。",
        "熊猫：……当时谁知道会这样。"], "", 20),
    event("d3_cat_fox", 3, "cat", "fox", "回来的时间", [
        "黑猫：前天晚上，你是不是又回来过？", "狐狸：回来拿东西。",
        "黑猫：几点？", "狐狸：……记不清了。"], "fox_returned", 30),
    event("d3_shiba_panda", 3, "shiba", "panda", "在查什么", [
        "熊猫：兔子最近是不是在查什么？", "柴犬：你听谁说的？"], "rabbit_search", 20),
    event("d3_cat_panda", 3, "cat", "panda", "不是你的咖啡", [
        "熊猫：你平时不喝那个牌子的咖啡吧？", "黑猫：那不是我的。"], "last_coffee", 25),
    event("d3_cat_shiba", 3, "cat", "shiba", "咖啡来源", [
        "黑猫：我拿的是你放在茶水间的咖啡。", "柴犬：……是我买的。"], "coffee_source", 28),
    event("d4_fox_shiba", 4, "fox", "shiba", "招聘记录", [
        "狐狸：她查到多少了？", "柴犬：学历筛选那一页。",
        "狐狸：当年你说不会有人知道。"], "hire_secret", 35),
    event("d4_fox_wolf", 4, "fox", "wolf", "晋升交易", [
        "狐狸：晋升机会，你答应让给我。", "灰狼：别再提她。",
        "狐狸：那就别让我说出你们的关系。"], "wolf_threat", 30),
    event("d4_wolf_cat", 4, "wolf", "cat", "说不出口的关系", [
        "黑猫：你那天一直看着她。", "灰狼：我不能过去。",
        "黑猫：因为你们的关系？"], "wolf_love", 25),
    event("d5_cat_bear", 5, "cat", "bear", "被改过的数字", [
        "黑猫：她桌上的文件，不只是狐狸的招聘记录。",
        "棕熊：还有项目绩效表。数字被人改过。"], "metrics_changed", 35),
    event("d5_shiba_wolf", 5, "shiba", "wolf", "没有发出的邮件", [
        "柴犬：草稿收件人写着老板。", "灰狼：她说过要把记录交出去。"], "draft_to_boss", 32),
    event("d5_bear_panda", 5, "bear", "panda", "项目截止", [
        "棕熊：我只想把项目做完。", "熊猫：我知道她很累，可还是催了。"], "bear_pressure", 24),
    event("d6_wolf_panda", 6, "wolf", "panda", "18:12", [
        "熊猫：18:12，兔子的电脑为什么还亮着？",
        "灰狼：是我登录的。我删了部分材料。",
        "灰狼：她告诉过我她要举报。我害怕。"], "wolf_deleted", 40),
    event("d6_wolf_cat", 6, "wolf", "cat", "没有走过去", [
        "灰狼：那天我看出来她不舒服了。", "灰狼：我本来想过去。",
        "灰狼：但是所有人都在。我怕别人看出来。", "灰狼：……所以我没有。"], "wolf_avoided", 36),
    event("d6_fox_shiba", 6, "fox", "shiba", "那场争吵", [
        "柴犬：她那天故意把所有人的注意力引到你身上。",
        "狐狸：她还在查更大的事？"], "rabbit_strategy", 30),
]

profiles = [
    dict(id="rabbit", name="兔子", public="HR｜温和、细心", hidden="原则坚定；调查绩效系统；灰狼的秘密恋人", clue="rabbit_strategy"),
    dict(id="wolf", name="灰狼", public="沉默的项目员工", hidden="兔子的恋人；18:12删除部分材料", clue="wolf_deleted"),
    dict(id="fox", name="狐狸", public="能力强、野心大", hidden="学历不达门槛；被柴犬帮助入职", clue="hire_secret"),
    dict(id="shiba", name="柴犬", public="HR｜热心", hidden="帮助狐狸绕过招聘筛选", clue="hire_secret"),
    dict(id="cat", name="黑猫", public="安静、观察力强", hidden="给兔子递咖啡；咖啡并无异常", clue="coffee_clear"),
    dict(id="bear", name="棕熊", public="项目负责人｜直接", hidden="曾催兔子处理流程", clue="bear_pressure"),
    dict(id="panda", name="熊猫", public="项目员工｜随和", hidden="知道兔子疲惫，仍然催她", clue="bear_pressure"),
]

replay = [
    "狐狸想晋升 → 威胁灰狼。", "灰狼想保护恋情 → 退出竞争。",
    "兔子想维护原则 → 调查狐狸。", "柴犬想保护朋友 → 隐瞒招聘问题。",
    "兔子继续调查 → 看见绩效系统异常。", "灰狼害怕生活被改变 → 反对举报。",
    "棕熊和熊猫想赶项目 → 继续催促。", "黑猫见兔子疲惫 → 递了咖啡。",
    "老板想要效率 → 96% → 98% → 100%。", "兔子不愿示弱 → 继续工作。",
    "PERFECT。", "18:07。兔子倒下了。",
    "没有任何一个人设计过这个结果。", "每个人只是完成了当时认为合理的选择。",
    "这些选择相互作用以后——兔子死了。",
]

data = dict(days=days, events=events, clues=clues, profiles=profiles,
            replay=replay)
OUT.parent.mkdir(parents=True, exist_ok=True)
OUT.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(OUT, len(days), "days", len(events), "events", len(clues), "clues")
