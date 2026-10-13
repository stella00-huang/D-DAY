"""Independent stage directions; AB Excel and authored dialogue are read-only inputs."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ab = json.loads((ROOT / 'Assets/Resources/DDay/ABConfig.json').read_text(encoding='utf-8'))
ids = ['rabbit', 'wolf', 'fox', 'shiba', 'cat', 'bear', 'panda']
normal_centers = [140, 418, 726, 1055, 1373, 1707, 2038]
reaction_centers = [140, 403, 711, 1036, 1351, 1681, 2006]
reactions = ['happy', 'nervous', 'serious', 'sad', 'serious', 'happy', 'sad']
visible_tops = [134,143,153,171,180,114,135] # Union of both existing alpha silhouettes per animal.
states = ['neutral', 'happy', 'sad', 'angry', 'surprised', 'nervous', 'serious']
characters = []
for i, actor in enumerate(ids):
    characters.append({'speakerId': actor, 'expressions': [
        {'id': 'neutral', 'resource': 'DDay/PixelV2/Employees',
         'x': normal_centers[i] - [112,150,150,142,129,163,147][i], 'y': visible_tops[i], 'width': [224,300,300,284,258,326,294][i], 'height': 540-visible_tops[i]},
        {'id': reactions[i], 'resource': 'DDay/PixelV2/TaskReactions',
         'x': reaction_centers[i] - [112,150,150,142,129,163,147][i], 'y': visible_tops[i], 'width': [224,300,300,284,258,326,294][i], 'height': 540-visible_tops[i]}]})

directions = []
def direct(event, index, expression='neutral', focus='', speaker=''):
    directions.append({'eventId': event, 'lineIndex': index, 'speakerId': speaker,
        'focusId': focus, 'expressionId': expression, 'portraitPosition': 'left',
        'switchSpeaker': True, 'effect': 'fade'})

# Deliberate per-line direction, not a keyword classifier. Unsupported art is a documented fallback.
a2_moods = {
 4:['nervous','serious'], 5:['happy','happy'], 6:['happy'], 7:['neutral','nervous'],
 8:['neutral','nervous','surprised','serious'], 9:['serious','serious','nervous'],
 10:['serious','neutral','neutral','serious'], 11:['neutral','sad','serious'],
 12:['neutral','neutral','sad','surprised','sad'], 13:['neutral','serious','happy','angry'],
 14:['neutral','neutral'], 15:['neutral','serious','surprised','angry'],16:['neutral','happy'],
 17:['neutral','sad','serious','neutral'],18:['neutral'],19:['happy','serious','neutral','nervous'],
 20:['nervous','neutral','neutral','happy'],21:['happy','nervous','happy','serious','happy'],
 22:['neutral','serious','surprised','serious','sad','happy'],23:['neutral'],
 24:['nervous','serious','nervous','serious','neutral','nervous','happy'],25:['neutral','serious'],
 26:['neutral','sad'],27:['serious','serious','neutral','serious'],28:['neutral','serious','nervous'],
 29:['angry','serious','surprised','angry','neutral'],30:['neutral','neutral','nervous','neutral'],
 31:['neutral'],32:['neutral','surprised','serious','sad'],33:['neutral'],
 34:['happy','serious','neutral','neutral'],35:['neutral','serious','neutral','angry','surprised','angry','sad'],
 36:['neutral'],37:['serious','nervous','serious','serious'],38:['serious','nervous','serious','serious'],
 39:['neutral','serious','serious','serious','neutral'],40:['neutral'],41:['neutral','neutral','neutral','happy'],
 42:['neutral'],43:['neutral','happy']}
for clue in ab['clues']:
    moods = a2_moods[clue['sourceRow']]
    lines = [line for line in clue['stateText'].splitlines() if line.strip()]
    for index in range(len(lines)):
        direct(clue['id'], index, moods[index] if index < len(moods) else 'neutral')

# Task acceptance uses the existing, character-specific reaction art. No dialogue is invented.
for task in ab['tasks']:
    for i, actor in enumerate(ids):
        event = f"B1_L{task['loop']}_D{task['day']}_{task['id']}_{actor}"
        direct(event, 0, reactions[i], actor, actor)

# Non-quoted source paragraphs stay labelled narration. Focus portraits illustrate the authored scene.
b2 = {
 'B2_L1_D1': {1:('rabbit','neutral'),2:('rabbit','happy'),3:('rabbit','neutral'),
              4:('rabbit','surprised'),5:('rabbit','sad'),6:('wolf','sad'),7:('rabbit','happy')},
 'B2_L1_D2': {0:('wolf','nervous'),1:('wolf','sad'),2:('shiba','surprised')},
 'B2_L1_D4': {0:('fox','serious')},
 'B2_L2_D1': {0:('rabbit','happy'),1:('bear','serious')},
 'B2_L2_D2': {0:('wolf','nervous'),1:('wolf','serious'),2:('rabbit','happy')},
 'B2_L2_D4': {0:('wolf','nervous')},
 'B2_L2_D6': {0:('shiba','sad'),1:('rabbit','serious'),2:('rabbit','serious'),
              3:('panda','nervous'),4:('rabbit','serious'),5:('fox','serious'),
              6:('cat','serious'),7:('rabbit','serious'),8:('cat','serious'),
              9:('cat','serious'),10:('panda','serious'),11:('rabbit','serious')},
 'B2_L1_D6': {1:('panda','neutral'),2:('shiba','neutral'),3:('fox','serious'),
              4:('shiba','happy'),5:('shiba','happy'),6:('cat','serious'),7:('shiba','happy'),
              8:('panda','serious'),9:('bear','happy'),12:('fox','serious'),13:('shiba','sad'),
              14:('panda','sad'),15:('cat','serious'),16:('panda','serious'),17:('fox','serious'),
              18:('wolf','sad'),19:('wolf','sad'),20:('bear','neutral'),21:('wolf','surprised'),
              22:('bear','neutral'),23:('wolf','nervous'),24:('bear','neutral'),25:('wolf','serious'),
              26:('bear','nervous'),27:('wolf','serious'),28:('bear','serious'),30:('panda','serious'),
              33:('panda','serious'),34:('bear','nervous'),35:('panda','nervous'),36:('bear','serious'),
              37:('panda','serious'),40:('bear','neutral'),41:('panda','serious'),45:('bear','serious'),47:('bear','serious')}
}
for event, entries in b2.items():
    for index, (actor, mood) in entries.items():
        direct(event,index,mood,actor)

target = ROOT/'Assets/Resources/DDay/DialoguePresentation.json'
target.write_text(json.dumps({'characters':characters,'directions':directions},
                            ensure_ascii=False,indent=2),encoding='utf-8')
report = ['# D-DAY 立绘与表情素材审计','',
 '普通立绘复用 `Assets/Resources/DDay/PixelV2/Employees.png`。',
 '反馈表情复用 `Assets/Resources/DDay/PixelV2/TaskReactions.png`。',
 '裁切配置与逐句演出：`Assets/Resources/DDay/DialoguePresentation.json`。',
 '没有新增、替换或修改办公室角色图片。七种状态均可配置；缺失状态实际回退 neutral。','',
 '| 角色 | 已有真实素材 | 缺少的素材（运行时 neutral 回退） |','|---|---|---|']
codes=['兔子 SISY','灰狼 ORIN','狐狸 NARCIS','柴犬 ECHO','黑猫 METIS','棕熊 THANE','熊猫 CASSIA']
for i, code in enumerate(codes):
    report.append(f"| {code} | neutral、{reactions[i]} | {'、'.join(x for x in states if x not in ('neutral',reactions[i]))} |")
report += ['', '现有素材共 14 个有效角色/表情组合，缺少 35 个组合。',
 '情绪由独立演出配置指定，未指定为 neutral；没有按文本关键词自动猜测。',
 'B2 原文中的描述性段落显示“旁白”，不会伪装成角色亲口说出的新对白。']
(ROOT/'Docs/DDayPortraitResourceAudit.md').write_text('\n'.join(report),encoding='utf-8')
print(f'{len(characters)} portrait definitions / {len(directions)} authored direction entries')
