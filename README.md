# GoogleSheetToDataForUnity

Unity package wrapper for the [GoogleSheetToData](https://github.com/arandra/GoogleSheetToData) core pipeline.  
This repo hosts the distributable package (`Packages/com.arandra.gsheet-to-data`), Google API DLLs, documentation, and the core submodule.

## Repository Layout
| Path | Description |
| --- | --- |
| `Packages/com.arandra.gsheet-to-data/` | Unity package with Editor tooling, asmdef files, bundled Google API DLLs, README/CHANGELOG/LICENSE, and samples. |
| `Core/` | Git submodule pointing at `arandra/GoogleSheetToData`. Unity asmdefs compile the submodule directly. |
| `Document/` | Internal docs (sheet authoring, OAuth guide, project link index). |

## Getting Started
1. Clone the repo with submodules (`git clone --recurse-submodules`).
2. For Unity consumption, reference this repository as a git package:  
   `"com.arandra.gsheet-to-data": "https://github.com/arandra/GoogleSheetToDataForUnity.git#main"`.
3. Follow the package [README](Packages/com.arandra.gsheet-to-data/README.md) for installation, OAuth setup, and sample usage.
4. Core-only contributors can continue to work inside `Core/` and push via the upstream repository; update the submodule pointer here when new tags are published.

## Versioning
- Core: `MAJOR.MINOR`
- Unity package: `MAJOR.MINOR.PATCH`
- Tag Unity releases as `vMAJOR.MINOR.PATCH` and record the corresponding core tag in the submodule update.
- Document notable changes in [`Packages/com.arandra.gsheet-to-data/CHANGELOG.md`](Packages/com.arandra.gsheet-to-data/CHANGELOG.md).

## Documentation
- [Sheet Authoring Guide](Document/SheetAuthoringGuide.md)
- [Google OAuth Setup Guide](Document/GoogleOAuthSetup.md)
- [Project Links](Document/ProjectLinks.md)

## Testing
- Target validation editor: **Unity 2022.2 LTS**.
- Current status: package creation and docs prepared; Unity install test still pending (run locally before shipping a release and update the changelog notes).

## Samples
The package exposes two `Samples~` folders (FieldTransform table workflow and InitConst const workflow). Import them via the Unity Package Manager to reproduce the documented sheet scenarios (`Sheet ID: 1_2Y3BtltwsyXTovWuWV6J32x_Ebe2Sy8vybGNhzkIsM`).

## Sensitive Data Policy
- `client_secret.json` paths are stored via `EditorPrefs` only.
- OAuth tokens default to `<Project>/Temp/GSheetToData/` to avoid committing secrets.
