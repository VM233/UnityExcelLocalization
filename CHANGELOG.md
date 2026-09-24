# Changelog

## 0.1.0

- Bind one Excel workbook to one Unity String Table Collection.
- Synchronize key, translation and locale-column changes automatically in Edit Mode.
- Preserve existing key IDs and table GUIDs; reject invalid input before changing tables.
- Synchronize and validate before Play Mode and builds.
- Synchronize before Addressables builds its bundles; reject source changes made after build preparation.
- Include Editor-only NPOI dependencies, licenses, an example workbook and EditMode tests.
