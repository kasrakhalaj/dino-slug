"""Export code and non-Metal-Slug source assets without altering the working game."""
from pathlib import Path
import argparse
import hashlib
import json
import re
import shutil

root = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument('--output', type=Path, default=root.parent / 'DinoSlug-Public')
parser.add_argument('--build', type=Path, required=True)
args = parser.parse_args()
output = args.output.resolve()
if output.exists():
    raise FileExistsError(f'Refusing to overwrite an existing publication: {output}')
if not args.build.is_file():
    raise FileNotFoundError(args.build)
output.mkdir(parents=True)

def metal_slug_media(relative):
    path = relative.removesuffix('.meta')
    if path == 'Assets/Game/Art/MetalSlug' or path.startswith('Assets/Game/Art/MetalSlug/'):
        return True
    if path == 'Assets/Game/Audio/MetalSlug' or path.startswith('Assets/Game/Audio/MetalSlug/'):
        return True
    if path.startswith('Assets/Game/UI/SoldierSelect/'):
        # These portraits are crops of the author's Dino, with no SNK frame or background.
        return not Path(path).name.startswith('mydino_')
    # The selection announcement combines the author's voice with SNK's equip cue.
    return path == 'Assets/Game/Audio/Player/Dino-Selection.wav'

included = {}
excluded = []
def copy(source, destination):
    target = output / destination
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source, target)
    included[destination] = hashlib.sha256(target.read_bytes()).hexdigest()

for folder in ['Assets', 'Packages', 'ProjectSettings']:
    for source in (root / folder).rglob('*'):
        if not source.is_file():
            continue
        relative = source.relative_to(root).as_posix()
        if metal_slug_media(relative):
            if source.suffix != '.meta':
                metadata = Path(str(source) + '.meta')
                match = re.search(r'^guid: (\w+)', metadata.read_text(), re.M) if metadata.exists() else None
                excluded.append({'path': relative, 'guid': match.group(1) if match else None,
                                 'sha256': hashlib.sha256(source.read_bytes()).hexdigest()})
            continue
        copy(source, relative)

# Keep every source script, including optional original asset-preparation tools.
code_extensions = {'.cs', '.py', '.ps1', '.lua', '.sh'}
for source in (root / 'Tools').rglob('*'):
    if source.is_file() and source.suffix.lower() in code_extensions and '__pycache__' not in source.parts:
        copy(source, source.relative_to(root).as_posix())
for relative in ['Tools/SourceArt/MyDinoAnimation/animation.json',
                 'Tools/SourceArt/RunAndSoldiers/rifle-origin.json',
                 'Tools/SourceArt/MetalSlug/Reinforcements/bazooka-origin.json',
                 'Tools/SourceArt/MetalSlug/Vehicles/tank-origin.json']:
    if (root / relative).exists():
        copy(root / relative, relative)

copy(Path('E:/Aseprite/MyDino-Running.aseprite'), 'Artwork/Dino.aseprite')
voice = root / 'Tools/SourceArt/DinoVoice/Revision-2/Dino-D-Light-Arcade.wav'
if voice.exists():
    copy(voice, 'Artwork/Dino-Voice.wav')
for name in ['README.md', 'CREDITS.md', '.gitignore']:
    copy(root / 'Tools/RepositoryDocs' / name, name)
copy(root / 'Tools/RepositoryDocs/.gitattributes', '.gitattributes')
for source in (root / 'Tools/RepositoryDocs').iterdir():
    if source.is_file():
        copy(source, source.relative_to(root).as_posix())
for name in ['ProjectOverview.md', 'MissingMedia.md', 'Validation.md']:
    copy(root / 'Tools/RepositoryDocs' / name, 'Docs/' + name)
copy(args.build, 'Builds/DinoSlug-Windows.zip')

manifest = {'project': 'Dino Slug', 'source_media_omitted': True,
            'build_includes_original_game_media': True, 'excluded_assets': excluded}
(output / 'Docs/ExcludedMedia.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')

source_code = [p.relative_to(root).as_posix() for parent in ['Assets', 'Tools']
               for p in (root / parent).rglob('*') if p.is_file() and p.suffix.lower() in code_extensions and '__pycache__' not in p.parts]
missing_code = [name for name in source_code if name not in included]
assert not missing_code, missing_code
assert not any(metal_slug_media(name) for name in included)
large = [name for name in included if (output / name).stat().st_size >= 100 * 1024 * 1024]
assert not large, f'Files exceed GitHub\'s individual-file limit: {large}'
evidence = root / 'Docs/DinoSlug-Rename/publication-audit.json'
evidence.parent.mkdir(parents=True, exist_ok=True)
evidence.write_text(json.dumps({'output': str(output), 'included_files': len(included),
    'source_code_files': len(source_code), 'missing_code_files': missing_code,
    'excluded_source_media_files': len(excluded), 'included_sha256': included}, indent=2), encoding='utf-8')
print(json.dumps({'output': str(output), 'included_files': len(included),
    'all_source_code_files': len(source_code), 'omitted_source_media_files': len(excluded)}, indent=2))
