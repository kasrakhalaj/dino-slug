# Media excluded from the Unity source

The public source project is distributed without original Metal Slug sprites, scenery, music, sounds or adapted selection-screen framing. The full Windows build includes them, as requested by the project author. Dino-only exported portraits, original/generated prototype art, fonts with their supplied licenses and the author's separate Dino recording remain in the source project.

`ExcludedMedia.json` lists each omitted source asset's original project path, GUID and SHA256 checksum. The combined selection voice is on that list because it contains the original mechanical equip cue as well as the author's recording. A hash or credit is identification, not a grant of permission to redistribute a file.

## Restoring an existing complete project

If you already have the complete development asset set and permission to use it, copy each omitted asset **and its original `.meta` file** to the path listed in the manifest before opening Unity. The matching GUIDs reconnect the scene and prefab references. A differently named sprite sheet is not a direct replacement for the exported individual sprites.

The complete local development project remains separate from this public export. No automatic downloader for the omitted media is supplied. The source catalogues are recorded in `CREDITS.md`.

Without those files, Unity can inspect and compile the code, but the full scene has missing media references and the media-dependent PlayMode suite is not expected to pass. The EditMode rule tests do not require the omitted sprites or audio. Use `Builds/DinoSlug-Windows.zip` for the complete playable prototype.
