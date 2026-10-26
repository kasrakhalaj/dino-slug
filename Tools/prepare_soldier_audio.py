"""Prepare the named, pinned Metal Slug clips; preserve raw downloaded sources."""
from pathlib import Path
import hashlib
import json
import subprocess
import wave
from array import array

root = Path(__file__).resolve().parents[1]
source = root / 'Tools/SourceArt/MetalSlug/CombatAudio'
output = root / 'Assets/Game/Audio/MetalSlug'
tracks = [(f'soldier_death_{i:03d}.mp3', f'soldier_death_{i:02d}.wav') for i in range(1, 7)]
tracks += [('pistol_shot.wav', 'enemy_rifle_shot.wav'), ('grenade.wav', 'enemy_grenade_explosion.wav')]
facts = []
for raw, ready in tracks:
    subprocess.run(['ffmpeg', '-hide_banner', '-loglevel', 'error', '-i', str(source/raw),
                    '-af', 'silenceremove=start_periods=1:start_duration=0.002:start_threshold=-55dB,areverse,silenceremove=start_periods=1:start_duration=0.002:start_threshold=-55dB,areverse,aformat=channel_layouts=mono,aresample=44100,alimiter=limit=0.9:level=false',
                    '-ac', '1', '-ar', '44100', '-c:a', 'pcm_s16le', '-y', str(output/ready)], check=True)
    with wave.open(str(output/ready), 'rb') as wav:
        pcm = array('h', wav.readframes(wav.getnframes()))
        peak = max(abs(v) for v in pcm) / 32768
        duration = len(pcm) / wav.getframerate()
        assert wav.getnchannels() == 1 and duration > .05 and 0 < peak < .96
    facts.append({'raw':raw, 'game':ready, 'duration':round(duration,3), 'peak':round(peak,4),
                  'source_sha256':hashlib.sha256((source/raw).read_bytes()).hexdigest()})
    print(ready, f'{duration:.3f}s peak={peak:.3f}')
(source/'sources.json').write_text(json.dumps({'repository':'https://github.com/polmrtn/Metal-Slug',
    'revision':'fe20a4520e998b8a43f53d56fc4badd3d767f237',
    'folder':'Solution/Project1/OST/Non_music', 'original_owner':'SNK/Nazca', 'clips':facts}, indent=2))
# One local audition file: rifle, explosion, then the six distinct death voices.
with wave.open(str(source/'Combat-Sounds-Preview.wav'), 'wb') as preview:
    preview.setnchannels(1); preview.setsampwidth(2); preview.setframerate(44100)
    for name in ['enemy_rifle_shot.wav','enemy_grenade_explosion.wav'] + [f'soldier_death_{i:02d}.wav' for i in range(1,7)]:
        with wave.open(str(output/name), 'rb') as wav: preview.writeframes(wav.readframes(wav.getnframes()))
        preview.writeframes(b'\0\0' * 11025)
