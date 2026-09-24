# Excel Localization

Keep Unity Localization string tables in sync with Excel. One workbook owns one String Table Collection; each language is a column.

Requires Unity 2022.3 or later. Uses Unity Localization 1.5.13 and NPOI 2.7.4. All plugin code and NPOI libraries run in the editor.

## Install

In Package Manager, choose **Add package from git URL**:

    https://github.com/VM233/UnityExcelLocalization.git#v0.1.0

Unity Localization is installed as a package dependency. NPOI and its dependencies are included as ordinary Git files, so installation does not require Git LFS or Microsoft Excel.

## Set up a workbook

Create Localization Settings and a String Table Collection using Unity's Localization tools. Create an **Excel Table Binding** from **Assets / Create / Localization**, then assign the collection and choose a workbook inside the Unity project.

The source can be outside Assets, such as `Config/Localization/UI.xlsx`. Select the worksheet in the binding; the default is `Strings`.

| Key | zh-CN | en |
| --- | --- | --- |
| menu.play | 开始 | Play |
| menu.close | 关闭 | Close |
| inventory.title | 背包 ({0} / {1}) | Inventory ({0} / {1}) |

The first column must be `Key`. Remaining headers are canonical locale codes. Each row needs a unique key and a nonempty translation for every language. Keep cell values as text or numbers; formulas, dates, booleans and Excel errors are rejected. The package sample contains this workbook.

Translations use standard composite formatting: `{0}`, `{1}`, up to `{31}`. All translations in a row must use the same argument indices. Escape literal braces as `{{` and `}}`. Entries are imported as regular strings, not Smart Strings.

Save Excel and leave the editor in Edit Mode. It checks the file every second and waits for the content to settle before syncing. **Sync Now** and **Tools / Excel Localization / Sync All** are also available. Play Mode and builds synchronize all bindings before continuing.

## What a sync changes

- Adds and updates translations; removes keys and language tables absent from the workbook.
- Preserves IDs for retained keys and GUIDs for retained collections and tables. Renaming a key creates a new ID, so update references to renamed keys.
- Creates missing Locale assets. Removing a language column does not remove a shared project Locale.
- Validates the whole workbook before editing the collection. Duplicate keys, missing translations, invalid formatting or unreadable files leave the last successful tables intact and block a build.
- Leaves unchanged assets alone. Running the same import twice produces no table changes.

The workbook owns all entries in its bound collection, including entries added manually in Unity. A collection can have only one binding. Sync is one-way; Unity edits are overwritten by the next sync. Removing a workbook does not delete its collection: fix the path or deliberately remove the binding.

Automatic sync is paused during Play Mode, compilation and builds. It resumes in Edit Mode. The binding Inspector displays the most recent result; errors also include the file and row or cell in the Console.

## Tests

Add `"com.vm233.excel-localization"` to the `testables` array in the project's `Packages/manifest.json`, then run **VM233.ExcelLocalization.Editor.Tests** in the EditMode Test Runner. Tests use temporary workbooks and collections; they do not rewrite user workbooks.

## License

Plugin source is MIT licensed. NPOI and the other libraries retain their own licenses under `Editor/Plugins/NPOI/Licenses`. `Editor/Plugins/NPOI/packages.json` lists official package URLs, versions and SHA-256 hashes.
