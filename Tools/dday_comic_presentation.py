"""Shot directions only. All spoken/narrated words stay in the unmodified AB configuration."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = json.loads((root/'Assets/Resources/DDay/ABConfig.json').read_text(encoding='utf-8'))
shots = []
def actor(code, x=.5, expression='neutral', left=False, scale=1, tilt=0):
    return dict(id=code,x=x,expression=expression,facingLeft=left,scale=scale,tilt=tilt)
def shot(event, number, first, last, kind, actors=(), item='', tint=.85, caption='', effect='push'):
    shots.append(dict(eventId=event,shotId=f'{event}_S{number:02}',firstLine=first,lastLine=last,
        background='DDay/OfficeTemplate/BackgroundFacilitiesV2',kind=kind,actors=list(actors),
        itemId=item,tint=tint,timeCaption=caption,effect=effect))

# Legacy Day1 prose directions are retained for old-save migration. Active Day1 presentation
# is the independent seven-panel Story/Day1/Day1ComicPresentation.json; this tool never overwrites it.
e='B2_L1_D1'
shot(e,1,0,0,'environment',tint=.56,caption='9月5日 · 18:15',effect='pan')
shot(e,2,1,2,'character',[actor('rabbit',.43)],'orin_photo',.68)
shot(e,3,3,4,'character',[actor('rabbit',.5,scale=1.05)],tint=.57)
shot(e,4,5,5,'suspense',[actor('rabbit',.36,scale=.76,tilt=12)],'orin_photo',.48)
shot(e,5,6,7,'photo',item='orin_photo',tint=.50)
e='B2_L1_D2'
shot(e,1,0,1,'character',[actor('wolf',.50,'nervous')],caption='9月8日 · 下班后')
shot(e,2,2,2,'duo',[actor('wolf',.32,'nervous'),actor('shiba',.72,left=True)])
shot(e,3,3,3,'environment',tint=.73)
e='B2_L1_D4'
shot(e,1,0,0,'character',[actor('fox',.38,'serious'),actor('shiba',.72,left=True)],caption='9月10日 · 下班后')
shot(e,2,1,1,'environment')
e='B2_L1_D6'
shot(e,1,0,9,'dinner',[actor('panda',.23,scale=.90),actor('fox',.5,'serious',scale=.90),actor('shiba',.77,left=True,scale=.90)],caption='9月12日 · 夜晚')
shot(e,2,10,16,'dinner',[actor('panda',.3),actor('cat',.7,'serious',True)],tint=.72)
shot(e,3,17,29,'duo',[actor('wolf',.3,'nervous'),actor('bear',.7,left=True)],tint=.64)
shot(e,4,30,37,'duo',[actor('panda',.3,'sad'),actor('bear',.7,left=True)],tint=.61,caption='聚餐尾声')
shot(e,5,38,43,'document',item='letter',tint=.58)
shot(e,6,44,49,'suspense',[actor('bear',.5,'neutral')],tint=.43,caption='聚餐后')
e='B2_L2_D1'
shot(e,1,0,0,'duo',[actor('wolf',.3,'nervous'),actor('rabbit',.7,'happy',True)],caption='9月5日 · 18:15')
shot(e,2,1,1,'suspense',[actor('bear',.5,'neutral')],tint=.48)
e='B2_L2_D2'
shot(e,1,0,1,'duo',[actor('wolf',.3,'nervous'),actor('rabbit',.7,'happy',True)],caption='9月8日 · 下班后')
shot(e,2,2,2,'duo',[actor('wolf',.3,'nervous'),actor('rabbit',.7,'happy',True)])
shot(e,3,3,3,'environment',tint=.72)
e='B2_L2_D4'
shot(e,1,0,0,'character',[actor('wolf',.38,'nervous'),actor('shiba',.72,left=True)],caption='9月10日 · 下班后')
shot(e,2,1,1,'environment')
e='B2_L2_D6'
shot(e,1,0,1,'dinner',[actor('wolf',.23,scale=.90),actor('rabbit',.5,scale=.90),actor('fox',.77,'serious',True,.90)],caption='9月12日 · 夜晚')
shot(e,2,2,3,'dinner',[actor('panda',.3),actor('shiba',.7,left=True)],tint=.75)
shot(e,3,4,7,'duo',[actor('rabbit',.3),actor('cat',.7,'serious',True)],tint=.68)
shot(e,4,8,12,'private',[actor('rabbit',.23,scale=.88),actor('panda',.5,scale=.88),actor('cat',.77,'serious',True,.88)],tint=.57,caption='聚餐后 · 私下会面')

# Every original B2 line before Day7 belongs to exactly one shot. Nothing is cut or reordered.
for event in source['fixedEvents']:
    if event['day']==7: continue
    for index in range(len(event['lines'])):
        matched=[s for s in shots if s['eventId']==event['id'] and s['firstLine']<=index<=s['lastLine']]
        assert len(matched)==1,(event['id'],index,matched)
target=root/'Assets/Resources/DDay/ComicPresentation.json'
target.write_text(json.dumps(dict(shots=shots),ensure_ascii=False,indent=2),encoding='utf-8')
print(f'{len(shots)} shots, 8 fixed events; Day7 choices unchanged; AB source untouched.')
