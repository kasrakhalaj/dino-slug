"""Match the identified HMG announcement against the downloaded raw sound archive."""
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
from zipfile import ZipFile
import subprocess
import numpy as np
import wave

root = Path(__file__).resolve().parent / 'SourceArt' / 'MetalSlug'
with wave.open(str(root / 'voice-analysis.wav')) as w:
    audio = np.frombuffer(w.readframes(w.getnframes()), dtype='<i2').astype(float)
    audio = np.interp(np.arange(0,len(audio),w.getframerate()/8000),np.arange(len(audio)),audio)
reference = audio[int(1.27*8000):int(2.12*8000)]
reference -= reference.mean()
reference /= np.linalg.norm(reference)
archive = ZipFile(root / 'MS12X3-vrom-extraction.zip')
names = [n for n in archive.namelist() if n.startswith('MS1/SE/ADPCM_A/') and n.endswith('.mp3')]
def score(name):
    data = archive.read(name)
    proc = subprocess.run(['ffmpeg','-v','error','-f','mp3','-i','pipe:0','-ac','1','-ar','8000','-f','f32le','pipe:1'], input=data, capture_output=True)
    signal = np.frombuffer(proc.stdout, dtype='<f4').astype(float)
    if len(signal) < len(reference): return (0, name, 0)
    signal -= signal.mean()
    fft_size = 1 << (len(signal)+len(reference)-2).bit_length()
    convolution = np.fft.irfft(np.fft.rfft(signal,fft_size)*np.fft.rfft(reference[::-1],fft_size),fft_size)
    correlation = convolution[len(reference)-1:len(signal)]
    energies = signal**2
    cumulative = np.concatenate(([0], np.cumsum(energies)))
    norms = np.sqrt(np.maximum(1e-15, cumulative[len(reference):] - cumulative[:-len(reference)]))
    values = correlation / norms
    index = int(np.argmax(values))
    return float(values[index]), name, index/8000
with ThreadPoolExecutor(max_workers=4) as executor:
    ranking = sorted(executor.map(score,names),reverse=True)
for item in ranking[:10]: print(item)
best = ranking[0]
if best[0] > .2:
    (root / 'raw-heavy-machine-gun.mp3').write_bytes(archive.read(best[1]))
    (root / 'raw-voice-match.txt').write_text(str(best))
reference = audio[int(.61*8000):int(1.0*8000)].copy()
reference -= reference.mean()
reference /= np.linalg.norm(reference)
with ThreadPoolExecutor(max_workers=4) as executor:
    ranking = sorted(executor.map(score,names),reverse=True)
print('Mechanical pickup cue:')
for item in ranking[:5]: print(item)
if ranking[0][0] > .2:
    (root / 'raw-weapon-cock.mp3').write_bytes(archive.read(ranking[0][1]))
    (root / 'raw-cock-match.txt').write_text(str(ranking[0]))
