"""Original announcement plus a distinct blast/pump mix from saved SNK effects.

The firing mix is an adaptation, not a claim to be the isolated original S shot.
"""
from pathlib import Path
import subprocess, hashlib, json, wave
root=Path(__file__).resolve().parents[1]
src=root/'Tools/SourceArt/MetalSlug'
out=root/'Assets/Game/Audio/MetalSlug'
def run(*args): subprocess.run(['ffmpeg','-hide_banner','-loglevel','error',*map(str,args)],check=True)
run('-i',src/'raw-weapon-cock.mp3','-i',src/'raw-shotgun.mp3','-filter_complex',
    '[0:a]aformat=channel_layouts=mono,aresample=44100[a];[1:a]aformat=channel_layouts=mono,aresample=44100[b];[a][b]concat=n=2:v=0:a=1,alimiter=limit=0.9:level=false[out]',
    '-map','[out]','-c:a','pcm_s16le','-y',out/'shotgun_pickup.wav')
run('-i',out/'enemy_grenade_explosion.wav','-i',out/'enemy_rifle_shot.wav','-i',src/'raw-weapon-cock.mp3',
    '-filter_complex',
    '[0:a]atrim=duration=0.27,afade=t=out:st=0.12:d=0.15,volume=0.8[a];'
    '[1:a]atrim=duration=0.20,afade=t=out:st=0.09:d=0.11,volume=0.55[b];'
    '[2:a]atrim=duration=0.30,afade=t=out:st=0.24:d=0.06,volume=0.45,adelay=240:all=1[c];'
    '[a][b][c]amix=inputs=3:normalize=0,aformat=channel_layouts=mono,aresample=44100,alimiter=limit=0.9:level=false[out]',
    '-map','[out]','-c:a','pcm_s16le','-y',out/'shotgun_shot.wav')
report={}
for name in ['shotgun_pickup.wav','shotgun_shot.wav']:
    p=out/name
    with wave.open(str(p)) as f:
        import array
        samples=array.array('h',f.readframes(f.getnframes()))
        report[name]={'duration':len(samples)/f.getframerate(),'peak':max(map(abs,samples))/32768,
                      'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}
print(json.dumps(report,indent=2))
(src/'shotgun-audio.json').write_text(json.dumps({'firing_effect':'Adapted mix of original grenade transient, rifle transient and mechanical cock; not isolated original shotgun firing audio','files':report},indent=2))
