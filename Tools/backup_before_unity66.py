"""Preserve all authored Unity inputs before opening the project in a newer editor."""
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
import datetime
import hashlib
import json
import subprocess

root = Path(__file__).resolve().parents[1]
folder = root.parent / 'Backups'
folder.mkdir(exist_ok=True)
stamp = datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%d-%H%M%S')
target = folder / f'DinoSlug-before-Unity66-{stamp}.zip'
files = []
for name in ['Assets', 'Packages', 'ProjectSettings', 'Tools']:
    files.extend(p for p in (root / name).rglob('*') if p.is_file() and '__pycache__' not in p.parts)
files.extend((root / 'Docs').glob('*.md'))
files.extend((root / 'Docs/AI').glob('*.md'))
for name in ['README.md', '.gitignore', '.gitattributes', 'Recording (5).m4a']:
    if (root / name).is_file():
        files.append(root / name)
state = {'source': str(root), 'utc': stamp,
         'project_version': (root / 'ProjectSettings/ProjectVersion.txt').read_text(),
         'git_head': subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=root, text=True).strip(),
         'working_tree': subprocess.check_output(['git', 'status', '--short'], cwd=root, text=True),
         'sha256': {p.relative_to(root).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest() for p in files}}
with ZipFile(target, 'x', ZIP_DEFLATED, compresslevel=4) as archive:
    for path in files:
        archive.write(path, path.relative_to(root).as_posix())
    archive.writestr('BACKUP-MANIFEST.json', json.dumps(state, indent=2))
with ZipFile(target) as archive:
    assert archive.testzip() is None
record = root / 'Docs/Unity66-Review/backup.json'
record.parent.mkdir(parents=True, exist_ok=True)
record.write_text(json.dumps({'archive': str(target), 'files': len(files), 'bytes': target.stat().st_size,
                              'source_version': '6000.5.7f1', 'crc_verified': True}, indent=2))
print(record.read_text())
