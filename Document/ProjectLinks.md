# Project Links & References

| Item | Path | Notes |
| --- | --- | --- |
| Core Repository | `Core/` (submodule of https://github.com/arandra/GoogleSheetToData.git) | .NET parser, generator, console runner, and shared serializers. |
| Unity Package | `Packages/com.arandra.gsheet-to-data/` | Editor tooling, Google API DLLs, asmdefs, docs, and samples. |
| Sheet Authoring Guide | `Document/SheetAuthoringGuide.md` | Details for Table vs. Const layouts and sample workflows. |
| Google OAuth Guide | `Document/GoogleOAuthSetup.md` | Steps for creating/storing `client_secret.json` and tokens. |

## Notes
- Installing the git package pulls both the Unity tooling and the Core submodule source.
- Unity-specific code no longer lives in the core repo; this repository is the single entry point for Unity users.
- Add future documentation under `Document/` and link it from `README.md` as needed.
