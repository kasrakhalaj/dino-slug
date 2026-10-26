"""Rename the existing project without replacing its authored assets or GUIDs."""
from pathlib import Path
import json

root = Path(__file__).resolve().parents[1]
changed = []
for folder in ['Assets', 'ProjectSettings', 'Tools']:
    for path in (root / folder).rglob('*'):
        if not path.is_file() or path == Path(__file__).resolve():
            continue
        if path.suffix.lower() not in {'.cs', '.asmdef', '.unity', '.prefab', '.asset', '.inputactions', '.py', '.ps1', '.lua'}:
            continue
        try:
            old = path.read_bytes()
            text = old.decode('utf-8-sig')
        except UnicodeDecodeError:
            continue
        updated = text.replace('DesertDash', 'DinoSlug').replace('Desert Dash', 'Dino Slug')
        updated = updated.replace('DESERT / DASH', 'DINO / SLUG').replace('-desert-smoke', '-dino-smoke')
        if path.name == 'GameSession.cs':
            updated = updated.replace('"DinoSlug.HighScore.v1"', '"DesertDash.HighScore.v1"')
        if updated != text:
            path.write_bytes(updated.encode('utf-8-sig') if old.startswith(b'\xef\xbb\xbf') else updated.encode('utf-8'))
            changed.append(path.relative_to(root).as_posix())

# Move files together with their existing .meta files. Unity is closed for this operation.
def move(source, destination):
    if not source.exists():
        return
    destination.parent.mkdir(parents=True, exist_ok=True)
    if destination.exists():
        raise FileExistsError(destination)
    source.rename(destination)
    metadata = Path(str(source) + '.meta')
    if metadata.exists():
        metadata.rename(Path(str(destination) + '.meta'))

for folder in ['Assets/Game/Editor', 'Assets/Game/Scenes', 'Assets/Game/Input', 'Assets/Game/Scripts', 'Assets/Game/Tests/EditMode', 'Assets/Game/Tests/PlayMode']:
    for path in list((root / folder).glob('DesertDash*')):
        if path.suffix != '.meta':
            move(path, path.with_name(path.name.replace('DesertDash', 'DinoSlug')))

groups = {
    'Core': ['GameSession', 'RunModel'],
    'Player': ['PlayerController', 'PlayerWeapon', 'RunnerInput'],
    'Enemies': ['RebelSoldier', 'SoldierAttack', 'VehicleEnemy', 'MobileEnemy', 'EnemyOrdnance'],
    'World': ['ObstacleSpawner', 'ParallaxLayer', 'MapProgression', 'Interactable', 'GroundObstacle', 'AirObstacle'],
    'Pickups': ['Coin', 'HealthPickup', 'HeavyMachineGunPickup', 'ShotgunPickup'],
    'UI': ['GameHud', 'HudStyle', 'CharacterSelection'],
    'Presentation': ['FrameAnimation', 'GameAudio', 'RunPresentation'],
    'Testing': ['StandaloneSmoke'],
}
for category, names in groups.items():
    for name in names:
        source = root / 'Assets/Game/Scripts' / (name + '.cs')
        move(source, source.parent / category / source.name)

evidence = root / 'Docs/DinoSlug-Rename'
evidence.mkdir(parents=True, exist_ok=True)
(evidence / 'changed-text-files.json').write_text(json.dumps(changed, indent=2))
print(f'Renamed {len(changed)} text files; script and asset GUIDs retained.')
