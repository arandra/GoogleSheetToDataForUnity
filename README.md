# GoogleSheetToDataForUnity

Use Google Sheets as your content table and let the tooling generate the code and data for you.  
**GoogleSheetToDataForUnity** is a Unity package wrapper around the core [GoogleSheetToData](https://github.com/arandra/GoogleSheetToData) pipeline. It turns Google Sheets into strongly typed code and pure data files that can be consumed by Unity or any other platform that references the core library.

---

## Why this project?
In many teams the workflow looks like this: someone designs a spreadsheet, a programmer writes/updates matching C# types, another script parses the sheet, and everything must be repeated whenever the layout changes. This project eliminates most of that churn by keeping sheets as the source of truth and generating the glue for you.

### What you get
- **Less boilerplate code** – Matching C# types and loading code are generated from the Google Sheet layout, so engineers spend less time on repetitive plumbing.
- **Pure data for multiple platforms** – The pipeline emits plain data in addition to Unity ScriptableObjects, which means a single sheet can feed the Unity client, backend services, or tooling.
- **Non-programmers can own the data** – Designers edit Google Sheets directly and drive regeneration from Unity Editor windows without touching scripts or command-line tooling.

---

## How it works (high level)
1. **Design a table in Google Sheets** following the rules from the Sheet Authoring Guide.
2. **The core pipeline reads the sheet**, validates it, and emits strongly typed C# plus pure data files.
3. **The Unity package hosts editor tooling** (OAuth setup, sheet registry, generation actions, per-sheet overrides) so you can run the pipeline from inside Unity.
4. **Other platforms reuse the same data** by referencing the shared core library or the exported pure data artifacts.

---

## When should you use this?
Use it if you rely on Google Sheets as your source of truth, have non-programmers frequently editing data, need Unity and server code to stay in lockstep, or are tired of chasing deserialization bugs whenever a column changes.

---

## Unity package overview
The Unity-specific portion of this repo lives under `Packages/com.arandra.gsheet-to-data/`. It contains the editor tooling, runtime/serializable helpers, bundled Google API DLLs (editor-only), samples, and documentation.

From Unity’s perspective:
1. Add this repository as a Git dependency or local package.
2. Configure OAuth secrets and default paths in the **Settings** window.
3. Register sheets and trigger generation inside the **Asset Manager** window.
4. Consume the generated types/assets in your gameplay code.

See [`Packages/com.arandra.gsheet-to-data/README.md`](Packages/com.arandra.gsheet-to-data/README.md) plus the docs under `Document/` for step-by-step Unity workflows.

---

## Repository Layout
| Path | Description |
| --- | --- |
| `Packages/com.arandra.gsheet-to-data/` | Distributable Unity package with editor tooling, asmdefs, bundled Google API DLLs, samples, README/CHANGELOG/LICENSE. |
| `Core/` | Git submodule pointing at `arandra/GoogleSheetToData`; Unity asmdefs compile it directly so all platforms stay in sync. |
| `Document/` | Markdown guides (sheet authoring, OAuth, project link index). |
| `Scripts/` | Helper automation such as `sync-core-to-package.*` for mirroring the submodule into the Unity package pre-release. |

---

## Getting Started
1. Clone the repo with submodules (`git clone --recurse-submodules`).
2. Install the Unity package by following the steps in [`Packages/com.arandra.gsheet-to-data/README.md`](Packages/com.arandra.gsheet-to-data/README.md).
3. Inspect the package folder directly if you need to review asmdefs, samples, or bundled DLLs.
4. Core-only contributors work inside `Core/` and push via the upstream repository; update the submodule pointer here when new tags are published.

---

## Syncing Core Sources into the Package
Git-based Unity installs can only see files under `Packages/com.arandra.gsheet-to-data`, so the submodule contents must be copied into the package before release:

- `SerializableTypes` → `Packages/com.arandra.gsheet-to-data/Runtime/SerializableTypes`
- `GSheetToDataCore` → `Packages/com.arandra.gsheet-to-data/Editor/GSheetToDataCore` (editor-only keeps Google API DLLs out of player builds)

Steps:
1. Update the submodule (`git submodule update --init --recursive`).
2. Run a sync script from the repo root:
   - Bash (macOS/Linux/WSL): `./Scripts/sync-core-to-package.sh`
   - PowerShell (Windows): `pwsh Scripts/sync-core-to-package.ps1`
3. The script recopies the latest sources (excluding `bin/`, `obj/`, etc.), preserves Unity-only `.asmdef` files already in the package, and removes stale `Runtime/GSheetToDataCore` leftovers.
4. A generated manifest (`Packages/com.arandra.gsheet-to-data/.sync-manifest.json`) tracks synced files so deletions propagate—commit manifest updates with the synced sources.
5. Deterministic `.meta` files are auto-created so Unity never reports missing metadata even though Git packages are immutable.
6. Commit the refreshed `Runtime/SerializableTypes`, `Editor/GSheetToDataCore`, manifest, and any new `.meta` files before tagging a Unity release.

> Tip: iterate inside `Core/` during development, then run the sync script whenever Git installs (and Unity users) need the latest pipeline snapshot.

---

## Asset Management & Overrides
- Use the **Settings** window (`Tools ▸ GSheetToData ▸ Settings`) to edit project-wide defaults for script paths, namespaces, and OAuth secrets.
- Manage sheets through the **Asset Manager** (`Tools ▸ GSheetToData ▸ Asset Manager`). Register sheets, trigger generation/re-sync, and inspect diffs before committing.
- Per-sheet overrides let you redirect generated scripts or ScriptableObjects to custom folders/namespaces—handy for Addressables or feature-specific assemblies.
- Built-in actions let you open the Google Sheet, delete generated artifacts, or remove stale registry entries.
- Registry metadata lives in `Assets/Settings/Editor/GSheetToDataAssetRegistry.asset`; commit it (plus `.meta` files) so teammates share the same configuration.

---

## Versioning & Status
- Core library tags: `MAJOR.MINOR`.
- Unity package tags: `MAJOR.MINOR.PATCH`.
- Tag Unity releases as `vMAJOR.MINOR.PATCH` and record the corresponding core tag in the submodule update.
- Target validation editor: **Unity 2022.2 LTS**. Run a fresh Unity import test before releasing and document results in the changelog.
- Log notable changes in [`Packages/com.arandra.gsheet-to-data/CHANGELOG.md`](Packages/com.arandra.gsheet-to-data/CHANGELOG.md).

---

## Documentation
- [Sheet Authoring Guide](https://github.com/arandra/GoogleSheetToData/blob/master/Document/SheetAuthoringGuide.md)
- [Google OAuth Setup Guide](https://github.com/arandra/GoogleSheetToData/blob/master/Document/GoogleOAuthSetup.md)
- [Project Links](Document/ProjectLinks.md)

---

## Testing
- Unity integration tests are currently manual: open sample scenes, use the Asset Manager (`Tools ▸ GSheetToData ▸ Asset Manager`) to generate/re-sync, and confirm ScriptableObjects emit correctly.
- For Core logic, rely on upstream repository tests (run the `.sln` inside `Core/GoogleSheetToData.sln` as needed). Mirror fixes back via the submodule update.
- When adding features, note verification steps in the changelog and README to maintain reproducibility.

---

## Samples
The package exposes two `Samples~` folders (FieldTransform table workflow and InitConst const workflow). Import them through the Unity Package Manager to reproduce the documented sheet scenarios (`Sheet ID: 1_2Y3BtltwsyXTovWuWV6J32x_Ebe2Sy8vybGNhzkIsM`).

---

## Sensitive Data Policy
- `client_secret.json` paths are stored via `EditorPrefs` only.
- OAuth tokens default to `<Project>/Temp/GSheetToData/` so secrets are never committed.
