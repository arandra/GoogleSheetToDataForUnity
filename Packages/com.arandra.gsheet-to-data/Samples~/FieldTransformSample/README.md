# FieldTransform Table Workflow

This sample mirrors the **FieldTransform** tab from the shared sheet (`ID: 1_2Y3BtltwsyXTovWuWV6J32x_Ebe2Sy8vybGNhzkIsM`).  
Use it to validate the table-mode pipeline end-to-end.

## How to use
1. Import this sample via **Package Manager ▸ Google Sheet To Data (Editor) ▸ Samples**.
2. Open `field-transform-request.json` and copy the values into the **Asset Manager** (`Tools ▸ GSheetToData ▸ Asset Manager`) when registering the sheet.
3. Ensure `client_secret.json` points to your local OAuth credentials (see https://github.com/arandra/GoogleSheetToData/blob/master/Document/GoogleOAuthSetup.md) via the **Settings** window.
4. Click **Generate / Re-sync**. After recompilation, the job processor writes:
   - `Assets/GSheetToData/Generated/Scripts/<RowType>.cs`
   - `Assets/GSheetToData/Generated/Scripts/<RowTypePlural>.cs`
   - A ScriptableObject asset at `Assets/GSheetToData/Generated/Assets/<RowTypePlural>.asset`
5. Keep the token path blank unless you want a custom directory—tokens default to `Temp/GSheetToData/`.

## What to expect
- Row definitions follow the “table” guide from https://github.com/arandra/GoogleSheetToData/blob/master/Document/SheetAuthoringGuide.md.
- Generated data contains a `List<RowType>` named `Values`.
- The ScriptableObject stores metadata (Sheet ID/Name) so downstream systems can trace the source.
