# Validation

Target: Unity **6000.6.7f1**, Windows 64-bit.

Validated after the Dino Slug rename on 2026-10-26:

| Check | Result |
|---|---|
| Compilation | No C# warnings or errors |
| EditMode | 15/15 passed |
| PlayMode | 54/54 passed, 192.549463 seconds |
| Windows build | Succeeded, 126,351,834 bytes before ZIP packaging |
| Actual executable gameplay check | Passed |
| Executable close/reopen and saved score | Passed; original preference restored |
| Menu branding | DINO SLUG title visually reviewed in the executable |
| Asset preservation | 755 existing art/audio/animation files unchanged; no duplicate asset GUIDs |

The complete working project is used for gameplay validation. The public Unity source omits Metal Slug media and therefore does not claim to pass the full media-dependent suite without restoring those assets. Publication checks compare every exported code file with the validated working source and audit the omitted source-media paths. The packaged Windows build is tested separately from the editor.
