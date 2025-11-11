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
2. Install the Unity package (see the section below) or explore the `Packages/com.arandra.gsheet-to-data` folder directly.
3. Follow the package [README](Packages/com.arandra.gsheet-to-data/README.md) for installation, OAuth setup, and sample usage.
4. Core-only contributors can continue to work inside `Core/` and push via the upstream repository; update the submodule pointer here when new tags are published.

## Installation (Unity Package Manager)
The package is designed to be installed straight from this Git repo, so you don't have to copy files manually.

1. Open Unity and go to **Window ▸ Package Manager**.
2. Click the **+** button → **Add package from git URL…**.
3. Paste the repo URL (optionally pin to a tag/branch). **Important:** the package lives under `Packages/com.arandra.gsheet-to-data`, so include the `?path=` hint:
   ```
   https://github.com/arandra/GoogleSheetToDataForUnity.git?path=Packages/com.arandra.gsheet-to-data#main
   ```
4. Unity pulls the `com.arandra.gsheet-to-data` package (including the Google APIs DLLs and samples) into your project.

If you keep packages under source control, you can also add the dependency directly in `Packages/manifest.json`:
```json
"com.arandra.gsheet-to-data": "https://github.com/arandra/GoogleSheetToDataForUnity.git?path=Packages/com.arandra.gsheet-to-data#main"
```

> Tip: Unity caches git packages per commit, so upgrading later is as easy as changing the hash/tag in the manifest or re-adding the package via the UI.

## Syncing Core Sources into the Package
Git-based Unity installs only have access to files that reside under `Packages/com.arandra.gsheet-to-data`.  
We keep the upstream Core submodule editable, then copy the pieces we ship into the package right before a release:

- `SerializableTypes` → `Packages/com.arandra.gsheet-to-data/Runtime/SerializableTypes`
- `GSheetToDataCore` → `Packages/com.arandra.gsheet-to-data/Editor/GSheetToDataCore` (Editor-only so the Google API DLLs stay out of player builds)

Steps:
1. Update the submodule (`git submodule update --init --recursive`).
2. Run one of the sync scripts from the repo root:
   - Bash (macOS/Linux/WSL): `./Scripts/sync-core-to-package.sh`
   - PowerShell (Windows): `pwsh Scripts/sync-core-to-package.ps1`
3. The script re-copies the latest sources (excluding `bin/`, `obj/`, etc.), preserves the Unity-only `.asmdef` files already tracked inside the package, and removes old `Runtime/GSheetToDataCore` leftovers.
4. A generated manifest (`Packages/com.arandra.gsheet-to-data/.sync-manifest.json`) keeps track of previously synced files so deletions propagate correctly—commit any changes to this manifest along with the synced sources.
5. The script also auto-creates deterministic `.meta` files for the synced directories/files so Unity never reports missing metadata (even though Git packages are immutable).
6. Commit the refreshed `Runtime/SerializableTypes`, `Editor/GSheetToDataCore`, manifest, and any new `.meta` files before tagging a Unity release so downstream installs compile without extra setup.

> Tip: keep iterating inside `Core/` during development. Run the sync script whenever you need the package copy (and therefore Git installs) to reflect the current Core state.

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
