"""Package the verified player, Unity source, and report without generated caches."""
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED

ROOT=Path(__file__).resolve().parents[1]
delivery=ROOT.parent/'Delivery'
delivery.mkdir(parents=True,exist_ok=True)
target=delivery/'DinoSlug-HW1.zip'
count=0
with ZipFile(target,'w',ZIP_DEFLATED,compresslevel=6) as z:
    z.writestr('START-HERE.txt',
        'DESERT DASH - HW1 REVIEW PACKAGE\n\n'
        'PLAY: Extract the archive, then open Windows/DinoSlug.exe. Keep all supporting files together.\n'
        'CONTROLS: Space jump, Shift dash, Escape pause. Mouse or arrows/Enter for menus.\n\n'
        'EDIT: Open UnityProject with Unity 6000.5.7f1, then open Assets/Game/Scenes/DinoSlug.unity.\n'
        'REPORT: Read DinoSlug-HW1-Report.pdf. Validation evidence is under UnityProject/Docs.\n\n'
        'SUBMISSION: Publish the local Git repository and add its accessible URL. Fill in the student name/ID '
        'and use the course-required archive name. This review archive does not claim to be uploaded or submitted.\n')
    z.writestr('Repository.txt',
        'Local Git repository: E:/MyGames/HW1/DinoSlug\n'
        'Remote repository URL: not yet published; add the chosen Git host URL before submission.\n')
    for folder in ['Assets','Packages','ProjectSettings','Docs','Tools']:
        for p in sorted((ROOT/folder).rglob('*')):
            if not p.is_file() or '__pycache__' in p.parts:continue
            z.write(p,'UnityProject/'+p.relative_to(ROOT).as_posix());count+=1
    for file in ['README.md','.gitignore']:
        z.write(ROOT/file,'UnityProject/'+file);count+=1
    for p in sorted((ROOT/'Builds'/'Windows').rglob('*')):
        if not p.is_file() or any('DoNotShip' in part for part in p.parts):continue
        z.write(p,'Windows/'+p.relative_to(ROOT/'Builds'/'Windows').as_posix());count+=1
    z.write(ROOT/'Docs'/'DinoSlug-HW1-Report.pdf','DinoSlug-HW1-Report.pdf')
print(f'{target}\n{count} source/player files, {target.stat().st_size:,} bytes')
