"""Rebuild the pickup cue and short gunshot from the downloaded source audio."""
from pathlib import Path
from zipfile import ZipFile
import subprocess

root = Path(__file__).resolve().parents[1]
source = root / 'Tools/SourceArt/MetalSlug'
output = root / 'Assets/Game/Audio/MetalSlug'
output.mkdir(parents=True, exist_ok=True)
# Identified against the named announcement recordings; preserve the raw archive clips.
indices = {'weapon-cock':179, 'heavy-machine-gun':183, 'rocket-launcher':184, 'shotgun':185, 'flame-shot':186}
with ZipFile(source / 'MS12X3-vrom-extraction.zip') as archive:
    for name,index in indices.items():
        (source / ('raw-'+name+'.mp3')).write_bytes(archive.read(f'MS1/SE/ADPCM_A/vrom-{index:04d}.bin.mp3'))
def ffmpeg(*args):
    subprocess.run(['ffmpeg','-hide_banner','-loglevel','error',*map(str,args)],check=True)
ffmpeg('-i',source/'raw-weapon-cock.mp3','-i',source/'raw-heavy-machine-gun.mp3',
       '-filter_complex','[0:a]aresample=44100,aformat=sample_fmts=fltp:channel_layouts=mono[a];[1:a]aresample=44100,aformat=sample_fmts=fltp:channel_layouts=mono[b];[a][b]concat=n=2:v=0:a=1,alimiter=limit=0.95[out]',
       '-map','[out]','-c:a','pcm_s16le','-y',output/'hmg_pickup.wav')
ffmpeg('-i',source/'machinegun-shot.mp3','-t','0.12','-af','afade=t=out:st=0.09:d=0.03','-ac','1','-ar','44100','-y',output/'hmg_shot.wav')
print('Prepared the mechanical pickup + HMG announcement, short gunshot, and four isolated weapon voices.')
