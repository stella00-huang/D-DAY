"""Day1 page directions from the user's supplied image/narration; never writes AB or image pixels."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
panels = [
    ('storm', [(8, 5), (1527, 5), (1527, 296), (8, 296)], [264, 10, 900, 194], False,
     'lightning', 'wind', 'thunder', '9月5日，18:15。窗外乌云压城，暴雨似乎随时都会落下。'),
    ('fatigue', [(9, 308), (542, 308), (497, 634), (9, 634)], [150, 318, 281, 274], False,
     'fatigue', 'wind', '', 'SISY 的桌上堆满了文件和咖啡杯。今天的工作，似乎依旧没有尽头。'),
    ('collapse', [(552, 309), (1051, 309), (1000, 634), (507, 634)], [633, 433, 361, 181], False,
     'impact', 'wind', 'impact', '下一刻，一声闷响打破了办公室的平静。SISY 从椅子上摔了下来。'),
    ('falling_photo', [(1061, 309), (1526, 309), (1526, 634), (1011, 634)], [1128, 421, 371, 206], False,
     'wind', 'wind', '', '一阵风从窗外吹进来，卷起散落的纸张。一张旧照片悄然落在地上。'),
    ('floor_photo', [(9, 644), (489, 644), (427, 1015), (9, 1015)], [25, 884, 383, 128], True,
     'photo', '', '', '照片上只有一只年轻的灰狼。那是大学时期的 ORIN。'),
    ('emergency_call', [(500, 644), (1022, 644), (1042, 1015), (437, 1015)], [568, 670, 319, 259], False,
     'phone', 'crowd', 'dial', 'ORIN 慌忙拨打急救电话。同事们围了过来，办公室顿时陷入混乱。'),
    ('ambulance', [(1032, 645), (1526, 645), (1526, 1015), (1052, 1015)], [1139, 647, 387, 357], False,
     'ambulance', 'rain', 'siren', '救护车终于赶到。雨幕中，SISY 被送往医院。没有人知道，这一天将如何改变 APIS。'),
]
data = dict(eventId='B2_L1_D1', presentationId='Day1PageV1', resource='DDay/Story/Day1/Day1ComicPage',
            width=1536, height=1024, panels=[])
for number, (name, outline, focus, focus_only, effect, ambience, sound, narration) in enumerate(panels, 1):
    data['panels'].append(dict(id=f'Day1_P{number:02}_{name}',
        outline=[dict(x=x, y=y) for x, y in outline],
        focus=dict(x=focus[0], y=focus[1], width=focus[2], height=focus[3]),
        focusOnly=focus_only, effect=effect, ambience=ambience, sound=sound, narration=narration))
target = root / 'Assets/Resources/DDay/Story/Day1/Day1ComicPresentation.json'
target.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')
print(f'7 irregular panels -> {target}; AB and source PNG unchanged.')
