"""Install the approved light-arcade voice after the existing weapon-cocking cue."""
from pathlib import Path
import hashlib
import json
import shutil
import wave
from refine_recording5_voice import normalize

ROOT = Path(__file__).resolve().parents[1]
FOLDER = ROOT / 'Tools/SourceArt/DinoVoice/Revision-2'
CUE = ROOT / 'Tools/SourceArt/MetalSlug/raw-weapon-cock.mp3'
VOICE = FOLDER / 'Dino-D-Light-Arcade.wav'
RATE = 48000
GAP = .045


def pcm(path):
    with wave.open(str(path), 'rb') as wav:
        assert (wav.getnchannels(), wav.getsampwidth(), wav.getframerate()) == (1, 2, RATE)
        return wav.readframes(wav.getnframes())


def main():
    cue = FOLDER / 'Selection-Weapon-Cock.wav'
    normalize(CUE, cue, 'afade=t=in:d=0.003,afade=t=out:st=0.716:d=0.003')
    cue_pcm, voice_pcm = pcm(cue), pcm(VOICE)
    gap_pcm = b'\0\0' * round(RATE * GAP)
    result = FOLDER / 'Dino-Selection-Kchkch-Then-Dino.wav'
    with wave.open(str(result), 'wb') as wav:
        wav.setnchannels(1)
        wav.setsampwidth(2)
        wav.setframerate(RATE)
        wav.writeframes(cue_pcm + gap_pcm + voice_pcm)
    # Verify exact order and preserve the approved voice without reprocessing it.
    combined = pcm(result)
    assert combined[:len(cue_pcm)] == cue_pcm
    assert combined[len(cue_pcm):len(cue_pcm) + len(gap_pcm)] == gap_pcm
    assert combined[len(cue_pcm) + len(gap_pcm):] == voice_pcm
    target = ROOT / 'Assets/Game/Audio/Player/Dino-Selection.wav'
    shutil.copy2(result, target)
    metadata = {
        'installed': str(target.relative_to(ROOT)),
        'sequence': ['existing Metal Slug weapon-cocking cue', '45 ms gap', 'approved light arcade Dino voice'],
        'cue_source': str(CUE.relative_to(ROOT)),
        'cue_seconds': len(cue_pcm) / (2 * RATE),
        'gap_seconds': GAP,
        'voice_start_seconds': (len(cue_pcm) + len(gap_pcm)) / (2 * RATE),
        'voice_seconds': len(voice_pcm) / (2 * RATE),
        'total_seconds': len(combined) / (2 * RATE),
        'voice_sha256': hashlib.sha256(VOICE.read_bytes()).hexdigest(),
        'installed_sha256': hashlib.sha256(target.read_bytes()).hexdigest(),
        'verification': 'Cue, silence gap and approved voice PCM match exactly in the installed sequence.'}
    (FOLDER / 'selection-sequence.json').write_text(json.dumps(metadata, indent=2), encoding='utf-8')
    print(json.dumps(metadata, indent=2))


if __name__ == '__main__':
    main()
