# Changelog

## 0.2.0

- Synchronize from Unity asset import callbacks instead of periodically polling files.
- Require source workbooks under Assets and store a workbook asset reference in each binding.
- Follow workbook moves and renames without rebinding; queue imports until the editor can safely write tables.
- Verify import updates, asset moves, invalid-save recovery and disabled automatic synchronization.
- Upgrade existing bindings by moving their workbooks into Assets and assigning the Workbook field.

## 0.1.0

- Bind one Excel workbook to one Unity String Table Collection.
- Synchronize key, translation and locale-column changes automatically in Edit Mode.
- Preserve existing key IDs and table GUIDs; reject invalid input before changing tables.
- Synchronize and validate before Play Mode and builds.
- Synchronize before Addressables builds its bundles; reject source changes made after build preparation.
- Include Editor-only NPOI dependencies, licenses, an example workbook and EditMode tests.
