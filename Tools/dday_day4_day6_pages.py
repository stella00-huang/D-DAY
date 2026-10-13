"""Directions only: unmodified comic pages and original B2 text. Never writes AB/Excel."""
import json
import struct
from pathlib import Path

root = Path(__file__).resolve().parents[1]
config = json.loads((root / 'Assets/Resources/DDay/ABConfig.json').read_text(encoding='utf-8-sig'))
events = {e['day']: e for e in config['fixedEvents'] if e['loop'] == 1 and e['day'] in (4, 6)}

def panel(day, number, name, outline, focus, narration='', source_lines=None,
          effect='push', ambience='', sound='', accent=None):
    p = dict(id=f'Day{day}_P{number:02}_{name}', outline=[dict(x=x, y=y) for x, y in outline],
             focus=dict(zip(('x', 'y', 'width', 'height'), focus)), focusOnly=False,
             narration=narration, effect=effect, ambience=ambience, sound=sound)
    if source_lines is not None:
        p['sourceLines'] = list(source_lines)
    if accent:
        p['accent'] = dict(zip(('x', 'y', 'width', 'height'), accent))
    return p

promotion, recruitment = events[4]['lines'][0].split('：', 1)[1].split('，', 1)
party = events[4]['lines'][1].split('：', 1)[1]
day4 = [
    panel(4, 1, 'evening', [(15,11),(495,11),(458,306),(15,306)], [140,105,210,190], '9月10日，下班后。'),
    panel(4, 2, 'announcement', [(517,11),(1203,11),(1248,306),(480,306)], [680,20,336,270], '晋升名单公布。', ambience='crowd'),
    panel(4, 3, 'promotion', [(1226,11),(1665,11),(1665,306),(1270,306)], [1304,13,344,286], promotion, sound='paper'),
    # Silent reaction panels use the supplied drawing, with no invented dialogue.
    panel(4, 4, 'narcis', [(15,327),(480,327),(428,624),(15,624)], [173,330,245,291]),
    panel(4, 5, 'recruitment', [(504,327),(1105,327),(1058,624),(452,624)], [614,335,472,223], recruitment, sound='paper'),
    panel(4, 6, 'colleagues', [(1131,327),(1665,327),(1665,624),(1080,624)], [1126,343,525,272], effect='somber'),
    panel(4, 7, 'party_date', [(15,646),(1665,646),(1665,925),(15,925)], [937,684,252,214], party, sound='paper'),
]
day6 = [
    panel(6, 1, 'restaurant', [(12,10),(359,10),(325,273),(12,273)], [25,27,292,232], '9月12日，聚餐。', ambience='crowd'),
    panel(6, 2, 'celebration', [(382,10),(1522,10),(1522,273),(346,273)], [474,35,1020,230], source_lines=range(1,4), ambience='crowd'),
    panel(6, 3, 'echo', [(12,295),(427,295),(399,540),(12,540)], [22,310,383,215], source_lines=range(4,10), ambience='crowd'),
    panel(6, 4, 'sisy_memory', [(448,295),(1522,295),(1522,540),(420,540)], [455,342,1051,186], source_lines=range(11,30),
          effect='somber', ambience='crowd', accent=[908,298,316,146]),
    panel(6, 5, 'dinner_end', [(15,559),(1522,559),(1522,735),(15,735)], [186,571,800,151], source_lines=[30], effect='somber'),
    panel(6, 6, 'resignation', [(15,757),(498,757),(470,1008),(15,1008)], [180,791,290,193], source_lines=range(32,43), sound='paper'),
    panel(6, 7, 'discarded_letter', [(521,757),(806,757),(774,1008),(493,1008)], [514,790,245,202], source_lines=[43], sound='paper'),
    panel(6, 8, 'call', [(834,757),(1129,757),(1101,1008),(799,1008)], [849,797,248,187], source_lines=range(44,49), effect='phone', sound='dial'),
    panel(6, 9, 'boss', [(1156,757),(1522,757),(1522,1008),(1126,1008)], [1205,790,249,214], source_lines=[49], effect='somber'),
]
assert sorted(i for p in day6 for i in p.get('sourceLines', [])) == [i for i in range(50) if i not in (0,10,31)]
for day, panels, legacy in (
    (4, day4, [2, 6]),
    (6, day6, [0] + [1]*3 + [2]*6 + [3]*20 + [4]*2 + [5]*11 + [6] + [7]*5 + [8]),
):
    folder = root / f'Assets/Resources/DDay/Story/Day{day}'
    width, height = struct.unpack('>II', (folder / f'Day{day}ComicPage.png').read_bytes()[16:24])
    data = dict(eventId=f'B2_L1_D{day}', presentationId=f'Day{day}PageV1',
                resource=f'DDay/Story/Day{day}/Day{day}ComicPage', width=width, height=height,
                legacyLinePanels=legacy, panels=panels)
    (folder / f'Day{day}ComicPresentation.json').write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')
    print(f'Day{day}: {width}x{height}, {len(panels)} actual panels. Original B2/Excel/images unchanged.')
