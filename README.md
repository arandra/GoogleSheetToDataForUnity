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
2. Install the Unity package by following the steps in [`Packages/com.arandra.gsheet-to-data/README.md`](Packages/com.arandra.gsheet-to-data/README.md).
3. Explore the package folder directly if you need to inspect asmdefs, samples, or bundled DLLs.
4. Core-only contributors can continue to work inside `Core/` and push via the upstream repository; update the submodule pointer here when new tags are published.

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

## Asset Management & Overrides
- Use the **Settings** window (`Tools ▸ GSheetToData ▸ Settings`) to edit project-wide defaults (script paths, namespaces, OAuth secrets).
- Manage every sheet inside the **Asset Manager** (`Tools ▸ GSheetToData ▸ Asset Manager`). Register sheets, trigger generation/re-sync, and inspect field diffs before regenerating.
- Toggle overrides per sheet to redirect scripts or ScriptableObjects to custom folders/namespaces—perfect for Addressables or feature-specific assemblies. Overrides affect only the selected sheet; global defaults remain untouched.
- Built-in actions let you open the Google Sheet in a browser, delete generated scripts/assets, or remove stale registry entries.
- Registry metadata lives in `Assets/Settings/Editor/GSheetToDataAssetRegistry.asset`; commit it (and `.meta` files) so teammates share the same map of sheets and overrides.

## Versioning
- Core: `MAJOR.MINOR`
- Unity package: `MAJOR.MINOR.PATCH`
- Tag Unity releases as `vMAJOR.MINOR.PATCH` and record the corresponding core tag in the submodule update.
- Document notable changes in [`Packages/com.arandra.gsheet-to-data/CHANGELOG.md`](Packages/com.arandra.gsheet-to-data/CHANGELOG.md).

## Documentation
- [Sheet Authoring Guide](https://github.com/arandra/GoogleSheetToData/blob/master/Document/SheetAuthoringGuide.md)
- [Google OAuth Setup Guide](https://github.com/arandra/GoogleSheetToData/blob/master/Document/GoogleOAuthSetup.md)
- [Project Links](Document/ProjectLinks.md)

## Testing
- Target validation editor: **Unity 2022.2 LTS**.
- Current status: package creation and docs prepared; Unity install test still pending (run locally before shipping a release and update the changelog notes).

## Samples
The package exposes two `Samples~` folders (FieldTransform table workflow and InitConst const workflow). Import them via the Unity Package Manager to reproduce the documented sheet scenarios (`Sheet ID: 1_2Y3BtltwsyXTovWuWV6J32x_Ebe2Sy8vybGNhzkIsM`).

## Sensitive Data Policy
- `client_secret.json` paths are stored via `EditorPrefs` only.
- OAuth tokens default to `<Project>/Temp/GSheetToData/` to avoid committing secrets.
