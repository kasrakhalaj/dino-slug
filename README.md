# Dino Slug

A pixel-art dinosaur runner made for Game Design HW1 in Unity 6.6. Dino collects heavy-machine-gun and shotgun power-ups, fights infantry and vehicles, and runs through four maps that change every 3,000 points.

## Play

Download [DinoSlug-Windows.zip](Builds/DinoSlug-Windows.zip), extract the whole folder, and run **DinoSlug.exe**. Keep the executable, data folder, and DLLs together. The Windows build contains the complete game, including its Metal Slug media.

| Action | Control |
|---|---|
| Jump | Space |
| Dash | Shift |
| Fire an equipped weapon | Hold J or left mouse |
| Pause | Escape |
| Select Dino | Enter or mouse |

## Unity source

Use **Unity 6000.6.7f1**. The game scene is `Assets/Game/Scenes/DinoSlug.unity`.

The source project includes all game code, editor tools, tests, Dino artwork and other non-Metal-Slug assets. **Metal Slug sprites, scenery, selection-screen art, music and sounds are omitted from the source project.** The combined Dino selection clip is also omitted because it contains a Metal Slug sound effect. Dino-only portraits and the separate author-recorded voice are included.

The source checkout is not a complete playable copy of the full game. Opening its scene without the omitted media produces missing asset references, and media-dependent PlayMode tests require those files. Use the Windows build to play. See [MissingMedia](Docs/MissingMedia.md) for the asset list and restoration details.

## Layout

- `Assets/Game/Scripts`: Core, Player, Enemies, World, Pickups, UI and Presentation code.
- `Assets/Game/Scenes`, `Prefabs`, `Animation` and `Input`: Unity gameplay assets and bindings.
- `Assets/Game/Editor`: scene/asset authoring tools and the Windows build command.
- `Assets/Game/Tests`: EditMode rule tests and PlayMode integration tests.
- `Artwork`: editable Dino source and the separate Dino voice.
- `Tools`: optional source-art preparation and packaging scripts.
- `Docs`: project overview, validation and omitted-media manifest.

Choose **Dino Slug → Build Windows** when working with the complete asset set. Tests are available in Unity Test Runner as `DinoSlug.EditMode` and `DinoSlug.PlayMode`.

See [ProjectOverview](Docs/ProjectOverview.md) for the game structure and [CREDITS](CREDITS.md) for asset sources, authorship and development-tool assistance.
