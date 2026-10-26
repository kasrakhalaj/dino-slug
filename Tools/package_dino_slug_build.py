"""Package the validated complete Windows game, excluding Unity's debug backup folder."""
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
import hashlib
import json

root = Path(__file__).resolve().parents[1]
build = root / 'Builds/Windows'
target = root.parent / 'Delivery/DinoSlug-Windows.zip'
target.parent.mkdir(parents=True, exist_ok=True)
if target.exists():
    raise FileExistsError(target)
if not (build / 'DinoSlug.exe').is_file():
    raise FileNotFoundError(build / 'DinoSlug.exe')
files = [p for p in build.rglob('*') if p.is_file() and not any('BackUpThisFolder_ButDontShipItWithYourGame' in part for part in p.parts)]
with ZipFile(target, 'x', ZIP_DEFLATED, compresslevel=6) as archive:
    for source in files:
        archive.write(source, (Path('DinoSlug') / source.relative_to(build)).as_posix())
    archive.write(root / 'Tools/RepositoryDocs/CREDITS.md', 'DinoSlug/CREDITS.md')
    archive.writestr('DinoSlug/READ-ME.txt',
        'DINO SLUG\n\nExtract this complete folder, then run DinoSlug.exe.\n'
        'Keep DinoSlug_Data, MonoBleedingEdge and the DLLs beside the executable.\n\n'
        'Space: jump | Shift: dash | Hold J / left mouse: fire | Escape: pause\n\n'
        'This is the full prototype, including original Metal Slug media. See CREDITS.md.\n'
        'Source: https://github.com/kasrakhalaj/dino-slug\n')
with ZipFile(target) as archive:
    assert archive.testzip() is None
assert target.stat().st_size < 100 * 1024 * 1024
report = {'zip': str(target), 'files': len(files), 'bytes': target.stat().st_size,
          'sha256': hashlib.sha256(target.read_bytes()).hexdigest(), 'crc_verified': True}
(root / 'Docs/DinoSlug-Rename/build-package.json').write_text(json.dumps(report, indent=2))
print(json.dumps(report, indent=2))
