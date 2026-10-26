"""Preserve the supplied take, restore its quiet attack, and prepare arcade variants."""
from pathlib import Path
import argparse, hashlib, json, shutil, subprocess, wave
import numpy as np

root=Path(__file__).resolve().parents[1]
folder=root/'Tools/SourceArt/DinoVoice'
folder.mkdir(parents=True,exist_ok=True)
rate=48000

def ffmpeg(*args):
    subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-y',*map(str,args)],check=True)

def read(path):
    data=subprocess.run(['ffmpeg','-v','error','-i',str(path),'-f','f32le','-ar',str(rate),'-ac','1','-'],check=True,stdout=subprocess.PIPE).stdout
    return np.frombuffer(data,dtype='<f4').copy()

def write(path,a):
    assert np.all(np.isfinite(a))
    with wave.open(str(path),'wb') as w:
        w.setnchannels(1);w.setsampwidth(2);w.setframerate(rate)
        w.writeframes((np.clip(a,-.98,.98)*32767).astype('<i2').tobytes())

def prepare():
    original=root/'Recording (5).m4a'
    shutil.copy2(original,folder/'Recording-5-Original.m4a')
    source=read(original)
    # Keep 130 ms before the dominant vowel; the original's early articulation
    # is about 20-30 dB quieter and would be lost to the former silence gate.
    attack=source[int(1.425*rate):int(2.46*rate)].copy()
    t=np.arange(len(attack))/rate
    gain=np.interp(t,[0,.015,.11,.155,1.04],[0,10,10,1,1])
    attack*=gain*12
    write(folder/'Dino-A-restored-dry.wav',attack)
    # Only a local synthesized /d/ release is layered in B. The actual word,
    # performance and sustained vowel remain the user's supplied recording.
    d=read(folder/'d-consonant-reference.wav')[int(.135*rate):int(.224*rate)]
    write(folder/'d-release-dry.wav',d)
    ffmpeg('-i',folder/'d-release-dry.wav','-af','rubberband=pitch=1.6','-ar',rate,folder/'d-release-matched.wav')
    d=read(folder/'d-release-matched.wav')
    d*=.28/max(.001,float(np.sqrt(np.mean(d*d))))
    n=min(350,len(d)//4);d[:n]*=np.linspace(0,1,n);d[-n:]*=np.linspace(1,0,n)
    b=attack.copy();start=int(.035*rate);length=min(len(d),len(b)-start)
    b[start:start+length]=b[start:start+length]*.2+d[:length]
    write(folder/'Dino-B-reinforced-dry.wav',b)
    for letter,pitch in [('A',.92),('B',.86)]:
        dry=folder/('Dino-A-restored-dry.wav' if letter=='A' else 'Dino-B-reinforced-dry.wav')
        result=folder/('Dino-A-Natural.wav' if letter=='A' else 'Dino-B-Reinforced-D.wav')
        graph=(
          '[0:a]asplit=2[front][tail];'
          '[front]atrim=end=0.43,asetpts=PTS-STARTPTS,atempo=0.9[a];'
          '[tail]atrim=start=0.43,asetpts=PTS-STARTPTS,atempo=0.5,atempo=0.84[b];'
          '[a][b]concat=n=2:v=0:a=1,'
          f'rubberband=pitch={pitch}:formant=shifted,'
          'highpass=f=85,lowpass=f=6500,equalizer=f=2200:t=q:w=1.1:g=3,'
          'acompressor=threshold=0.16:ratio=3:attack=2:release=90:makeup=1.3,'
          'asoftclip=type=tanh:threshold=0.8,'
          'aecho=0.8:0.4:45|90:0.12|0.06,loudnorm=I=-14:TP=-1.5:LRA=7[out]')
        ffmpeg('-i',dry,'-filter_complex',graph,'-map','[out]','-ar',rate,'-ac',1,'-c:a','pcm_s16le',result)
    metadata={
      'source':'Recording (5).m4a','source_sha256':hashlib.sha256(original.read_bytes()).hexdigest(),
      'preserved_copy':'Recording-5-Original.m4a','source_voice_span_seconds':[1.425,2.46],
      'attack':'Original quiet onset retained and raised; no onset silence gate.',
      'A':'Original attack only, pitch 0.92; sustained ending and arcade treatment.',
      'B':'Local Microsoft David synthesized d-release (0.135-0.224s) layered at 35 ms, pitch-matched; original vocal body, pitch 0.86.',
      'consonant_reference_text':'Dee no','consonant_timing':{'d':0.1371,'i':0.2128},
      'effects':'Tail time-stretch; presence EQ, compression, mild saturation, 45/90 ms slap, -14 LUFS / -1.5 dBTP target.',
      'identity':'Arcade effect on the user take, without samples or cloning of the Metal Slug announcer.'}
    (folder/'processing.json').write_text(json.dumps(metadata,indent=2))
    for p in [folder/'Dino-A-Natural.wav',folder/'Dino-B-Reinforced-D.wav']:
        with wave.open(str(p),'rb') as w: print(p.name,round(w.getnframes()/w.getframerate(),3),'seconds')

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--install',choices=['A','B']);args=parser.parse_args()
    prepare()
    if args.install:
        selected=folder/('Dino-A-Natural.wav' if args.install=='A' else 'Dino-B-Reinforced-D.wav')
        target=root/'Assets/Game/Audio/Player/Dino-Selection.wav';target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(selected,target)
        print('Installed variant',args.install,'at',target)
