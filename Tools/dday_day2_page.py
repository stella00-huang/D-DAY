"""Day2-only directions from the supplied unmodified comic and captions. No AB/Day1 writes."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
panels = [
    ('police', [(12, 11), (495, 11), (495, 304), (12, 304)], [276, 90, 213, 212],
     'cool', 'rain', '', '9月8日。警方已向 ORIN 了解情况。面对有关 SISY 的询问，他终于承认了两人的恋情。'),
    ('boss_interview', [(516, 11), (1102, 11), (1102, 304), (516, 304)], [915, 111, 182, 190],
     'push', '', '', '随后，老板又单独找 ORIN 了解情况。有关 SISY 的事情，他已经没有继续隐瞒的必要了。'),
    ('memory', [(1120, 11), (1523, 11), (1523, 304), (1120, 304)], [1290, 15, 225, 281],
     'memory', '', '', '在与老板谈话后，ORIN 和 SISY 的恋情终于公开。可惜，这份迟来的坦白，SISY 再也听不到了。'),
    ('comfort', [(13, 325), (769, 325), (698, 634), (13, 634)], [50, 344, 677, 287],
     'push', 'crowd', '', 'ORIN 走出办公室后，同事们纷纷上前安慰。其实大多数人早已察觉这段恋情，此刻却仍装作第一次听说。'),
    ('tomorrow_work', [(789, 325), (1523, 325), (1523, 634), (718, 634)], [877, 340, 552, 291],
     'push', '', 'paper', '老板随后通知所有人：从明天起，SISY 原本负责的工作将重新分摊，大家提前做好准备。'),
    ('sadness', [(15, 655), (885, 655), (839, 1008), (15, 1008)], [31, 708, 803, 291],
     'somber', '', '', '失去 SISY 的悲伤尚未散去，新的工作安排又让办公室陷入沉默。大家都意识到，她曾承担了多少工作。'),
    ('echo_pressure', [(906, 655), (1523, 655), (1523, 1008), (861, 1008)], [1175, 662, 265, 297],
     'push', '', 'paper', '只有 ECHO，是真的直到今天才知道 ORIN 与 SISY 的恋情。而想到那些过去交给 SISY 处理的工作可能重新落到自己身上，它更感到压力巨大。'),
]
data = dict(eventId='B2_L1_D2', presentationId='Day2PageV1', resource='DDay/Story/Day2/Day2ComicPage',
            width=1536, height=1024, legacyLinePanels=[0, 3, 6, 4], panels=[])
for number, (name, outline, focus, effect, ambience, sound, narration) in enumerate(panels, 1):
    panel = dict(id=f'Day2_P{number:02}_{name}', outline=[dict(x=x, y=y) for x, y in outline],
        focus=dict(x=focus[0], y=focus[1], width=focus[2], height=focus[3]), focusOnly=False,
        effect=effect, ambience=ambience, sound=sound, narration=narration)
    if name == 'memory':
        panel['accent'] = dict(x=1126, y=28, width=167, height=233)
    data['panels'].append(panel)
target = root / 'Assets/Resources/DDay/Story/Day2/Day2ComicPresentation.json'
target.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')
print('Day2: 7 real panel outlines; Day1/Excel/AB/image untouched.')
