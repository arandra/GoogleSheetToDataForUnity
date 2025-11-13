# InitConst Single Value Workflow

This sample references the **InitConst** tab from the same shared sheet (`ID: 1_2Y3BtltwsyXTovWuWV6J32x_Ebe2Sy8vybGNhzkIsM`).  
Use it to verify CONST exports that materialize as a single value on a ScriptableObject.

## Steps
1. Import the sample via the Unity Package Manager.
2. Copy the values from `init-const-request.json` into the **Asset Manager** (`Tools ▸ GSheetToData ▸ Asset Manager`) when registering the sheet.
3. Point `client_secret.json` at your local credential file (never commit this file) via the **Settings** window.
4. Leave the token path empty to use the default `Temp/GSheetToData/` storage.
5. Click **Generate / Re-sync**; once compilation finishes, the processor writes:
   - A POCO class for the sheet row.
   - A ScriptableObject wrapper that exposes `.Value` and metadata setters.

## Notes
- CONST rows expect one entry per line as documented in https://github.com/arandra/GoogleSheetToData/blob/master/Document/SheetAuthoringGuide.md.
- The resulting ScriptableObject exposes `Value` instead of `Values`, making it easier to inject configuration data.
