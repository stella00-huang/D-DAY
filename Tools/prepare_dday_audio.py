"""Copy user audio unchanged; losslessly decode FLAC and measure playback gains."""
from pathlib import Path
import hashlib, json, math, shutil, zipfile
import numpy as np
import soundfile as sf

ROOT = Path(__file__).resolve().parents[1]
SOURCE = Path('C:/Users/stella/Downloads')
REPORT = ROOT / 'Docs/AudioIntegration'
REPORT.mkdir(parents=True, exist_ok=True)
BGM = ['main_menu.wav', 'day1_2.mp3', 'day3_4.mp3', 'day5_6.mp3', 'day_7.flac', 'he.ogg', 'be.mp3']
SFX = ['click_001.ogg','select_001.ogg','drop_001.ogg','error_001.ogg',
       'confirmation_001.ogg','back_001.ogg','question_001.ogg','toggle_001.ogg']

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

entries=[]
for name in BGM:
    source=SOURCE/name
    target=ROOT/'Assets/Audio/BGM'/name
    target.parent.mkdir(parents=True, exist_ok=True)
    if target.exists() and sha(target)!=sha(source):
        raise RuntimeError('Refusing to overwrite different audio: '+str(target))
    shutil.copyfile(source,target)
    entries.append(dict(name=name, source=str(source), path=target.relative_to(ROOT).as_posix(),
                        sha256=sha(target), kind='BGM'))

flac=ROOT/'Assets/Audio/BGM/day_7.flac'
wav=flac.with_suffix('.wav')
info=sf.info(str(flac))
samples,rate=sf.read(str(flac),dtype='int32',always_2d=True)
if not wav.exists(): sf.write(str(wav),samples,rate,subtype=info.subtype)
decoded,wavrate=sf.read(str(wav),dtype='int32',always_2d=True)
assert wavrate==rate and np.array_equal(samples,decoded), 'FLAC conversion changed PCM samples'
pcm1=hashlib.sha256(samples.tobytes()).hexdigest()
entries.append(dict(name=wav.name,source=str(flac),path=wav.relative_to(ROOT).as_posix(),
                    sha256=sha(wav),kind='BGM', losslessPCM=pcm1,
                    stream=dict(sampleRate=rate,channels=info.channels,subtype=info.subtype)))

with zipfile.ZipFile(SOURCE/'kenney_interface-sounds.zip') as z:
    for name in SFX:
        target=ROOT/'Assets/Audio/SFX/Kenney'/name
        target.parent.mkdir(parents=True,exist_ok=True)
        data=z.read('Audio/'+name)
        if target.exists() and target.read_bytes()!=data: raise RuntimeError('Refusing overwrite')
        target.write_bytes(data)
        entries.append(dict(name=name,source='kenney_interface-sounds.zip/Audio/'+name,
                            path=target.relative_to(ROOT).as_posix(),sha256=sha(target),kind='SFX'))
    (ROOT/'Assets/Audio/SFX/Kenney/License.txt').write_bytes(z.read('License.txt'))
    (REPORT/'Kenney_Archive_Inventory.txt').write_text('\n'.join(z.namelist()),encoding='utf-8')

for entry in entries:
    if entry['name'].endswith('.flac'): continue
    path=ROOT/entry['path']
    data,rate=sf.read(str(path),always_2d=True)
    windows=[np.sqrt(np.mean(data[i:i+rate]**2)) for i in range(0,len(data),rate)]
    audible=[v for v in windows if v>10**(-70/20)]
    rms=float(np.percentile(audible,75)) if audible else 0
    db=20*math.log10(rms) if rms else -120
    target=-25 if entry['kind']=='BGM' else -32
    gain=min(1.0,10**((target-db)/20))
    entry.update(rms75Db=round(db,2),peakDb=round(20*math.log10(max(1e-10,np.max(np.abs(data)))),2),
                 duration=round(len(data)/rate,5),gain=round(gain,5),
                 measurement='75th percentile of audible one-second RMS windows; not perceptual LUFS')
assert all(sha(ROOT/e['path'])==e['sha256'] for e in entries)
(REPORT/'Audio_Import.json').write_text(json.dumps(dict(entries=entries),ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(dict(files=len(entries),flacPCMIdentical=True,gains={e['name']:e.get('gain') for e in entries}),ensure_ascii=False))
