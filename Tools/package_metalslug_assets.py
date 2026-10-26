"""Package the curated sprite/voice sources, editable animations, and credits."""
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
import hashlib
import json

root = Path(__file__).resolve().parents[1]
source = root / 'Tools/SourceArt/MetalSlug'
target = root / 'MetalSlug-Dino-Assets.zip'
files = []
for name in ['metal-slug-items.png','metal-slug-weapon-effects.png','marco-sprites.png']:
    files.append((source/name,'Original-Sheets/'+name))
for name in ['rebel-sprites.png','rebel-rifle-source.png','rebel-full-source.png','grenadier-source.png']:
    files.append((source/'Enemies'/name,'Original-Sheets/Enemies/'+name))
for name in ['biker-source.png','flying-tara-source.png','biker_drive.aseprite','tara_fly.aseprite']:
    files.append((source/'Enemies'/name,'Mobile-Enemies/'+name))
for file in (source/'Music').iterdir():
    if file.is_file(): files.append((file,'Music-Source/'+file.name))
for file in (root/'Assets/Game/Audio/MetalSlug/Music').glob('*.ogg'):
    files.append((file,'Game-Ready-Music/'+file.name))
files.append((root/'Docs/Arcade-Expansion.md','Arcade-Expansion.md'))
files.append((root/'Docs/Classic-Select-And-Plane-Pass.md','Classic-Select-And-Plane-Pass.md'))
files.append((root/'Docs/Dino-Selection-Voice.md','Dino-Selection-Voice.md'))
files.append((root/'Docs/UX-And-Game-Feel-Pass.md','UX-And-Game-Feel-Pass.md'))
for name in ['Recording-5-Original.m4a','Dino-A-Natural.wav','Dino-B-Reinforced-D.wav','d-consonant-reference.wav','processing.json']:
    files.append((root/'Tools/SourceArt/DinoVoice'/name,'Dino-Voice/'+name))
files.append((root/'Tools/prepare_recording5_voice.py','Dino-Voice/prepare_recording5_voice.py'))
files.append((root/'Assets/Game/Audio/Player/Dino-Selection.wav','Game-Ready-Audio/Dino-Selection.wav'))
files.append((root/'Docs/Dino-Voice-Review.md','Dino-Voice-Review.md'))
files.append((root/'Docs/Squads-And-Projectile-Readability.md','Squads-And-Projectile-Readability.md'))
for name in ['Dino-C-Clean.wav','Dino-D-Light-Arcade.wav','Selection-Weapon-Cock.wav','Dino-Selection-Kchkch-Then-Dino.wav','processing.json','selection-sequence.json']:
    files.append((root/'Tools/SourceArt/DinoVoice/Revision-2'/name,'Dino-Voice/Revision-2/'+name))
for name in ['refine_recording5_voice.py','prepare_dino_selection_audio.py']:
    files.append((root/'Tools'/name,'Dino-Voice/'+name))
for file in (root/'Tools/SourceArt/SelectionReference').iterdir():
    if file.suffix=='.aseprite' or file.name=='metal-slug-soldier-select-reference.png':
        files.append((file,'Dino-Soldier-Select/Native/'+file.name))
for file in (root/'Assets/Game/UI/SoldierSelect').glob('*.png'):
    files.append((file,'Dino-Soldier-Select/Sprites/'+file.name))
for name in ['PressStart2P-Regular.ttf','OFL.txt']:
    files.append((root/'Assets/Game/UI/Fonts'/name,'Dino-Soldier-Select/Font/'+name))
for name in ['heavy-machine-gun','rocket-launcher','shotgun','flame-shot','weapon-cock']:
    files.append((source/('raw-'+name+'.mp3'),'Voices-and-Sounds/'+name+'.mp3'))
for file in (root/'Assets/Game/Audio/MetalSlug').glob('*.wav'):
    files.append((file,'Game-Ready-Audio/'+file.name))
for file in (source/'CombatAudio').iterdir():
    if file.is_file(): files.append((file,'Soldier-Combat-Audio/'+file.name))
for file in (source/'Reinforcements').iterdir():
    if file.suffix == '.aseprite' or file.name in ['shield-source.png','bazooka-source.png','sandbags-source.png','barrel-source.png','bazooka-origin.json']:
        files.append((file,'Reinforcements/'+file.name))
files.append((root/'Docs/Reinforcements-And-Medicine.md','Reinforcements-And-Medicine.md'))
for file in (source/'Vehicles').iterdir():
    if file.suffix == '.aseprite' or file.name in ['di-cokka-source.png','r-shobu-source.png','tank-origin.json']:
        files.append((file,'Vehicles/'+file.name))
files.append((root/'Docs/MetalSlug-Vehicles.md','MetalSlug-Vehicles.md'))
files.append((root/'Docs/Vehicle-Projectile-Fixes.md','Vehicle-Projectile-Fixes.md'))
for file in (source/'Terrain').iterdir():
    if file.suffix == '.aseprite' or file.name in ['mission-1-source.png','mission-2-source.png','prepared-bg3.png']:
        files.append((file,'Terrain/'+file.name))
files.append((root/'Docs/Original-MetalSlug-Terrain.md','Original-MetalSlug-Terrain.md'))
files.append((root/'Docs/Score-Maps.md','Score-Maps.md'))
art = root/'Assets/Game/Art/MetalSlug'
for file in art.rglob('*.png'):
    files.append((file,'Sprites/'+file.relative_to(art).as_posix()))
for file in (source/'Prepared').glob('*.png'):
    files.append((file,'Sprites/'+file.name))
files.extend([
    (source/'H-Pickup.aseprite','H-Pickup.aseprite'),
    (source/'Enemies/Rebel-Soldier.aseprite','Rebel-Soldier.aseprite'),
    (source/'Enemies/rifle-Combat.aseprite','Rifle-Soldier.aseprite'),
    (source/'Enemies/grenadier-Combat.aseprite','Grenadier.aseprite'),
    (source/'Enemies/Grenade-Blast.aseprite','Grenade-Blast.aseprite'),
    (source/'Dino-Held-HMG.aseprite','Dino-Held-HMG.aseprite'),
    (source/'Dino-Held-Shotgun.aseprite','Dino-Held-Shotgun.aseprite'),
    (source/'S-Pickup.aseprite','S-Pickup.aseprite'),
    (source/'Shotgun-Blast.aseprite','Shotgun-Blast.aseprite'),
    (source/'Rifle-Warning.aseprite','Rifle-Warning.aseprite'),
    (source/'shotgun-audio.json','Shotgun-Audio-Sources.json'),
    (root/'Docs/Shotgun-And-Rifle-Dodge.md','Shotgun-And-Rifle-Dodge.md'),
    (root/'Docs/CombatSoldiers.md','Combat-Soldiers.md'),
    (root/'Docs/Run-And-Soldier-Correction.md','Run-And-Soldier-Correction.md'),
    (root/'Docs/Tail-And-Combat-Audio.md','Tail-And-Combat-Audio.md'),
    (root/'Docs/MetalSlugAssets.md','Sources-and-Usage.md'),
    (source/'repository-LICENSE.txt','Source-Repository-Software-License.txt')])
with ZipFile(target,'w',ZIP_DEFLATED,compresslevel=6) as archive:
    for file,name in files: archive.write(file,name)
    archive.writestr('START-HERE.txt',
        'Metal Slug media collected for the dinosaur runner.\n\n'
        'Sprites/: H/R/S/F/L pickups, complete held HMG/arms, tracer, Rebel, rifle soldier, grenadier, shield/bazooka soldiers, Rebel Biker, Flying Tara, Di-Cokka tank, R-Shobu helicopter, medicine, original temple/coast/harbor/factory map art, terrain/village props and ordnance.\n'
        'Aseprite files: editable pickup, held gun/arms, soldiers and grenade blast.\n'
        'Voices-and-Sounds/: isolated weapon announcements and mechanical pickup cue.\n'
        'Game-Ready-Audio/: pickup cue + HMG voice sequence and short gunshot.\n'
        'Game-Ready-Music/: the original Main Theme from Metal Slug, prepared as a continuous gameplay loop.\n'
        'Arcade-Expansion.md: smooth maps, new enemies, music and Dino selection.\n'
        'Dino-Voice/: preserved user recording, two processed previews, consonant reference and preparation settings; see Dino-Selection-Voice.md.\n'
        'Original-Sheets/: source pixels and original credit banners.\n'
        'Sources-and-Usage.md: URLs, credits, preparation and gameplay details.\n\n'
        'Original game media belongs to SNK/Nazca. The mirror software license does not establish a separate license for SNK media.\n\n'
        'Updated game: collect H for 50 rounds or S for 30 shells; hold J or left mouse. S has a wide short-range blast and a pump cycle. Hazards/soldiers take three HMG hits. Jump just after the rifle warning turns red to clear its volley and body.\n')
    archive.writestr('SHA256.json',json.dumps({name:hashlib.sha256(file.read_bytes()).hexdigest() for file,name in files},indent=2))
with ZipFile(target) as archive:
    assert archive.testzip() is None
print(f'{target}\n{len(files)} assets/notes + instructions/checksums; {target.stat().st_size:,} bytes')
