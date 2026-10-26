"""Audition restrained revisions of the user's take without replacing the game asset."""
from pathlib import Path
import hashlib
import json
import shutil
import subprocess
import wave
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Tools/SourceArt/DinoVoice/Revision-2'
SOURCE = ROOT / 'Recording (5).m4a'
RATE = 48000


def run(*args):
    return subprocess.run(['ffmpeg', '-hide_banner', '-nostdin', '-y', *map(str, args)],
                          check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)


def normalize(source, destination, effects):
    # Level-match the previews so extra loudness does not bias the comparison.
    target = 'loudnorm=I=-19:TP=-1.5:LRA=7'
    report = run('-i', source, '-af', effects + ',' + target + ':print_format=json', '-f', 'null', '-').stderr.decode()
    measured = json.loads(report[report.rfind('{'):report.rfind('}') + 1])
    second = target + ''.join(':' + key + '=' + measured[value] for key, value in [
        ('measured_I', 'input_i'), ('measured_TP', 'input_tp'), ('measured_LRA', 'input_lra'),
        ('measured_thresh', 'input_thresh'), ('offset', 'target_offset')]) + ':linear=true'
    run('-i', source, '-af', effects + ',' + second, '-ar', RATE, '-ac', 1, '-c:a', 'pcm_s16le', destination)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    installed = ROOT / 'Assets/Game/Audio/Player/Dino-Selection.wav'
    rejected = OUT / 'Rejected-Installed-Version.wav'
    if not rejected.exists():
        shutil.copy2(installed, rejected)
    data = run('-v', 'error', '-i', SOURCE, '-f', 'f32le', '-ar', RATE, '-ac', 1, '-').stdout
    audio = np.frombuffer(data, dtype='<f4').copy()
    # Keep the original consonant lead-in, with at most +6 dB of local emphasis.
    # The previous recipe raised that area +20 dB, inserted another voice, and
    # stretched the latter half 2.38x. Neither revision does those things.
    audio = audio[round(1.40 * RATE):round(2.52 * RATE)]
    time = np.arange(audio.size) / RATE
    audio *= np.interp(time, [0, .008, .13, .19, 1.12], [0, 2, 2, 1, 1])
    audio *= np.minimum(1, np.maximum(0, (1.12 - time) / .04))
    prepared = OUT / 'Original-Attack-Float.wav'
    subprocess.run(['ffmpeg', '-v', 'error', '-nostdin', '-y', '-f', 'f32le', '-ar', str(RATE), '-ac', '1',
                    '-i', '-', '-c:a', 'pcm_f32le', str(prepared)], input=audio.astype('<f4').tobytes(), check=True)
    clean = OUT / 'Dino-C-Clean.wav'
    arcade = OUT / 'Dino-D-Light-Arcade.wav'
    # Both keep original pitch, formants and delivery speed. No synthesized phonemes.
    normalize(prepared, clean, 'highpass=f=70,lowpass=f=7200')
    normalize(prepared, arcade,
              'highpass=f=95,lowpass=f=6500,volume=12,'
              'equalizer=f=2300:t=q:w=1.2:g=2,'
              'acompressor=threshold=0.13:ratio=2:attack=5:release=65:makeup=1.15,'
              'asoftclip=type=tanh:threshold=0.95,aecho=0.95:1:42:0.10')
    outputs = {}
    for path in [clean, arcade]:
        with wave.open(str(path), 'rb') as wav:
            outputs[path.name] = {'seconds': wav.getnframes() / wav.getframerate(),
                                  'rate': wav.getframerate(), 'channels': wav.getnchannels(),
                                  'sha256': hashlib.sha256(path.read_bytes()).hexdigest()}
    metadata = {'status': 'Preview candidates; installed game voice not changed.',
                'source_sha256': hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
                'trim_seconds': [1.40, 2.52], 'attack_emphasis_db_max': 6.0206,
                'pitch_ratio': 1, 'time_stretch_ratio': 1, 'synthetic_phonemes': False,
                'preview_target_lufs': -19,
                'C': 'Original pitch and timing, gentle band limiting and level normalization.',
                'D': 'Original pitch and timing, mild presence/compression and a quiet 42 ms echo.',
                'outputs': outputs}
    (OUT / 'processing.json').write_text(json.dumps(metadata, indent=2), encoding='utf-8')
    print(json.dumps(metadata, indent=2))


if __name__ == '__main__':
    main()
