# Changelog
All notable changes to this package are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.1] - 2025-11-11
### Fixed
- Bumped the package version to invalidate Unity’s cached immutable folders and ensure fresh metadata is downloaded.
- Sync scripts now auto-generate deterministic `.meta` files and keep a manifest so Git packages never miss metadata during installs.

### Changed
- `sync-core-to-package` now performs non-destructive copies and tracks deletions via `.sync-manifest.json`, simplifying release prep across contributors.

## [0.1.0] - 2025-11-10
### Added
- Unity Editor tooling migrated from the Core repo into `Packages/com.arandra.gsheet-to-data`.
- Bundled Google API DLLs (1.64.0) plus Newtonsoft dependency declaration.
- ScriptableObject + EditorPrefs-based settings persistence to keep secrets out of version control.
- `Samples~` presets for FieldTransform (table) and InitConst (const) workflows.
- Documentation for OAuth setup, sheet authoring, and cross-repo links.
- Assembly definition files for Core and SerializableTypes sources so Unity can compile the submodule directly.

### Changed
- GSheetToData settings now store OAuth tokens in `<ProjectRoot>/Temp/GSheetToData/` by default.
- Core repo README now points Unity consumers to this package.

### Testing
- Unity 2022.2 LTS git URL install: ❌ Pending (Unity editor not available in this environment).

[0.1.1]: https://github.com/arandra/GoogleSheetToDataForUnity/releases/tag/v0.1.1
[0.1.0]: https://github.com/arandra/GoogleSheetToDataForUnity/releases/tag/v0.1.0
