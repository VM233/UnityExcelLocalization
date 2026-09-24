# Verification

Unity 2022.3.62f3 on Windows, 2026-09-24.

22 EditMode tests passed, with no failures or skipped tests.

The tests cover workbook values and sparse cells, invariant numeric parsing, unsupported cell types, size limits, package platform settings, the included sample and worksheet selection. Collection tests use Unity's real Localization and Addressables editor APIs.

Sync checks cover additions, edits, deletion, retained key IDs and asset GUIDs, repeated imports without file changes, locale-column changes, conflicting bindings, project-relative paths, missing workbooks, invalid translations and format arguments, and manual table edits replaced from Excel. An EditMode coroutine changes a workbook outside Assets and waits for automatic synchronization without calling the importer.

Addressables settings, collections and workbooks created for each test are isolated and removed. Previous project Addressables settings are restored.
