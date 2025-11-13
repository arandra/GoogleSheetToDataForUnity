# Google Sheet To Data (Unity Package)

GoogleSheetToData brings the .NET core data pipeline into the Unity Editor so designers can pull strongly typed data straight from Google Sheets into ScriptableObjects.  
This package wraps the `arandra/GoogleSheetToData` core library, provides Unity Editor tooling, bundles the Google API DLLs (1.64.0), and includes samples that mirror the FieldTransform (table) and InitConst (const) sheet conventions.

## Requirements
- Unity **2022.2 LTS** or newer.
- .NET Standard 2.1 scripting backend (default for the Editor).
- Google Cloud OAuth `client_secret.json` that has access to the Sheets API.

## Installation
1. Open Unity and go to **Window ▸ Package Manager**.
2. Click **+ ▸ Add package from git URL…** and paste
   ```json  
  "https://github.com/arandra/GoogleSheetToDataForUnity.git?path=Packages/com.arandra.gsheet-to-data#main"  
   ```
   (`?path=…` ensures UPM pulls just the package subfolder).
3. Alternatively, edit `Packages/manifest.json` directly:
   ```json
   "com.arandra.gsheet-to-data": "https://github.com/arandra/GoogleSheetToDataForUnity.git?path=Packages/com.arandra.gsheet-to-data#main"
   ```
4. Unity downloads the package, including the `Core/` submodule source, Editor tooling, and bundled Google API DLLs.

## Quick Start
1. Follow the [Google OAuth setup guide](https://github.com/arandra/GoogleSheetToData/blob/master/Document/GoogleOAuthSetup.md) to create `client_secret.json`.
2. In Unity, import an included sample via **Package Manager ▸ Google Sheet To Data (Editor) ▸ Samples** if you want prefilled inputs.
3. Open **Tools ▸ GSheetToData ▸ Generator**.
4. Configure:
   - Script & asset output paths (must live under `Assets/`).
   - Namespace for generated classes.
   - Paths to `client_secret.json` and (optional) custom token directory.
5. Enter the target Sheet ID, Sheet tab name, and Sheet type (Table/Const).
6. Press **Generate ScriptableObject** to enqueue code generation; the job processor writes .cs files and ScriptableObjects once compilation completes.

## Settings & Sensitive Data
- Project-wide defaults (output paths, namespace) are stored in `ProjectSettings/GSheetToDataProjectSettings.asset` as a ScriptableObject.
- User-specific secrets (client secret path and OAuth token path) use `EditorPrefs` so they never touch version control.
- When the token path is left empty, the generator writes per-user tokens to `<ProjectRoot>/Temp/GSheetToData/`.

## Samples
| Sample | Sheet ID | Sheet Name | Sheet Type | Description |
| ------ | -------- | ---------- | ---------- | ----------- |
| FieldTransform Table Workflow | `1_2Y3BtltwsyXTovWuWV6J32x_Ebe2Sy8vybGNhzkIsM` | `FieldTransform` | Table | Demonstrates list-based exports and the FieldTransform workflow. |
| InitConst Single Value Workflow | `1_2Y3BtltwsyXTovWuWV6J32x_Ebe2Sy8vybGNhzkIsM` | `InitConst` | Const | Shows how single-value datasets map to ScriptableObjects. |

Each sample folder ships a JSON preset that documents which IDs/names to paste into the generator plus a README walking through OAuth/token expectations.

## Versioning
- Core repo (`core/` submodule) follows `MAJOR.MINOR`.
- Unity package follows `MAJOR.MINOR.PATCH`.
- Tag Unity releases as `vMAJOR.MINOR.PATCH` and align the bundled `core/` submodule to the matching `MAJOR.MINOR`.
- Document release notes in [`CHANGELOG.md`](./CHANGELOG.md) and include Unity Editor version used for validation.

## Testing
- Target verification editor: **Unity 2022.2 LTS**. Run through git URL installation plus generator workflow before tagging a release.
- Current status: pending manual verification (Unity is not available in this environment). Update the changelog once the 2022.2 LTS validation pass completes.

## Bundled Dependencies
- Google.Apis 1.64.0
- Google.Apis.Auth 1.64.0
- Google.Apis.Core 1.64.0
- Google.Apis.Sheets.v4 1.64.0.3148
- Google.Apis.Drive.v3 1.64.0.3155
- com.unity.nuget.newtonsoft-json 3.0.2 (automatically pulled by the manifest)

See the [Sheet Authoring Guide](https://github.com/arandra/GoogleSheetToData/blob/master/Document/SheetAuthoringGuide.md) for formatting conventions and the [Google OAuth Guide](https://github.com/arandra/GoogleSheetToData/blob/master/Document/GoogleOAuthSetup.md) for credential guidance.
