# 한국어로 대화 할 것.
# 계획을 먼저 세우고 실행할 지 물어볼 것.

# Repository Guidelines

## Project Structure & Module Organization
- `Packages/com.arandra.gsheet-to-data/` – Unity package delivered via Git UPM. Editor tooling sits in `Editor/`, runtime serializable helpers in `Runtime/`, samples under `Samples~/`.
- `Core/` – Upstream pipeline sources kept as a submodule (`GSheetToDataCore`, `SerializableTypes`, console harness, docs). Edit here during day-to-day development.
- `Document/` – Markdown guides (OAuth, sheet authoring, project links).
- `Scripts/` – Helper automation such as `sync-core-to-package.sh|ps1` for mirroring Core sources into the Unity package before release.

## Build, Test, and Development Commands
- `git clone --recurse-submodules <repo>` – Required to pull the Core pipeline.
- `./Scripts/sync-core-to-package.sh` (or `pwsh Scripts/sync-core-to-package.ps1`) – Copies `Core/SerializableTypes` → `Packages/.../Runtime` and `Core/GSheetToDataCore` → `Packages/.../Editor`, and regenerates the Unity asmdefs.
- Unity Editor: install the package via `Window ▸ Package Manager ▸ + ▸ Add package from git URL…` using `https://github.com/arandra/GoogleSheetToDataForUnity.git?path=Packages/com.arandra.gsheet-to-data#main`.

## Coding Style & Naming Conventions
- C# scripts follow Unity defaults: 4-space indentation, PascalCase for types/methods, camelCase for fields (prefix `_` only for private serialized fields if needed).
- Runtime code should avoid editor-only APIs; editor scripts live beneath `Editor/` and can use `[InitializeOnLoad]`, `MenuItem`, etc.
- Keep asmdef names stable: `GSheetToData.Core` (Editor only) and `GSheetToData.SerializableTypes` (Runtime).

## Testing Guidelines
- Unity integration tests currently manual: open sample scenes, use the Asset Manager (`Tools ▸ GSheetToData ▸ Asset Manager`) to generate/re-sync, and confirm ScriptableObjects emit correctly.
- For Core logic, rely on upstream repository tests (run through the `.sln` in `Core/GoogleSheetToData.sln` if needed). Mirror any fixes back via the submodule update.
- When adding features, note verification steps in the changelog and README to maintain reproducibility.

## Commit & Pull Request Guidelines
- Commits should describe both the subsystem and the change, e.g., `Editor: throttle job processor logging` or `Runtime: add Pair JSON converter docs`.
- Keep PRs scoped: include summary, testing notes (Unity version, manual steps), and reference relevant issues or sheets.
- Before opening a PR, run the sync script, ensure Unity imports cleanly (no console errors), update docs (`README`, `Document/`), and verify the package manifest still installs via git URL.
