using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NPOI.XSSF.UserModel;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.TestTools;

namespace VM233.ExcelLocalization.Tests
{
    public sealed class ExcelTableSyncTests
    {
        private string directory;
        private string workbookPath;
        private ExcelTableBinding binding;
        private StringTableCollection collection;
        private AddressableAssetSettings previousAddressables;
        private AddressableAssetSettingsDefaultObject previousDefault;

        [SetUp]
        public void Setup()
        {
            var suffix = Guid.NewGuid().ToString("N");
            directory = "Assets/ExcelLocalizationTests_" + suffix;
            AssetDatabase.CreateFolder("Assets", "ExcelLocalizationTests_" + suffix);
            previousAddressables = AddressableAssetSettingsDefaultObject.Settings;
            EditorBuildSettings.TryGetConfigObject(AddressableAssetSettingsDefaultObject.kDefaultConfigObjectName,
                out previousDefault);
            var settings = AddressableAssetSettings.Create(directory + "/Addressables", "Settings", true, true);
            var defaults = ScriptableObject.CreateInstance<AddressableAssetSettingsDefaultObject>();
            AssetDatabase.CreateAsset(defaults, directory + "/AddressablesDefault.asset");
            EditorBuildSettings.AddConfigObject(AddressableAssetSettingsDefaultObject.kDefaultConfigObjectName,
                defaults, true);
            AddressableAssetSettingsDefaultObject.Settings = settings;
            Directory.CreateDirectory("Library/ExcelLocalizationTests");
            workbookPath = "Library/ExcelLocalizationTests/" + suffix + ".xlsx";
            collection = LocalizationEditorSettings.CreateStringTableCollection(
                "ExcelTest_" + suffix, directory, new List<Locale>());
            binding = ScriptableObject.CreateInstance<ExcelTableBinding>();
            binding.Configure(workbookPath, collection, automatic: false);
            AssetDatabase.CreateAsset(binding, directory + "/Binding.asset");
            Write(new[] { "Key", "zh-CN", "en" },
                new[] { "greeting", "你好 {0}", "Hello {0}" }, new[] { "close", "关闭", "Close" });
        }

        [TearDown]
        public void Cleanup()
        {
            if (binding != null)
            {
                binding.Configure(workbookPath, collection, automatic: false);
            }

            if (collection != null)
            {
                foreach (var table in collection.StringTables.ToArray())
                {
                    collection.RemoveTable(table);
                }
            }

            foreach (var locale in LocalizationEditorSettings.GetLocales().ToArray())
            {
                if (AssetDatabase.GetAssetPath(locale).StartsWith(directory + "/", StringComparison.Ordinal))
                {
                    LocalizationEditorSettings.RemoveLocale(locale);
                }
            }

            AddressableAssetSettingsDefaultObject.Settings = previousAddressables;
            if (previousDefault == null)
            {
                EditorBuildSettings.RemoveConfigObject(AddressableAssetSettingsDefaultObject.kDefaultConfigObjectName);
            }
            else
            {
                EditorBuildSettings.AddConfigObject(AddressableAssetSettingsDefaultObject.kDefaultConfigObjectName,
                    previousDefault, true);
            }

            AssetDatabase.DeleteAsset(directory);
            File.Delete(workbookPath);
            AssetDatabase.SaveAssets();
        }

        private void Write(params string[][] rows)
        {
            using (var workbook = new XSSFWorkbook())
            {
                var sheet = workbook.CreateSheet("Strings");
                for (var i = 0; i < rows.Length; i++)
                {
                    var row = sheet.CreateRow(i);
                    for (var column = 0; column < rows[i].Length; column++)
                    {
                        row.CreateCell(column).SetCellValue(rows[i][column]);
                    }
                }

                using (var stream = File.Create(workbookPath))
                {
                    workbook.Write(stream);
                }
            }
        }

        private StringTable Table(string locale) => collection.GetTable(new LocaleIdentifier(locale)) as StringTable;

        [Test]
        public void SyncAddsUpdatesAndDeletesEntriesWithoutChangingRetainedIdsOrAssets()
        {
            Assert.IsTrue(ExcelTableSynchronizer.Sync(binding));
            var id = collection.SharedData.GetId("greeting");
            var tablePath = AssetDatabase.GetAssetPath(Table("en"));
            var tableGuid = AssetDatabase.AssetPathToGUID(tablePath);
            var collectionGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(collection));
            Write(new[] { "Key", "zh-CN", "en" },
                new[] { "greeting", "欢迎 {0}", "Welcome {0}" }, new[] { "new", "新条目", "New entry" });
            Assert.IsTrue(ExcelTableSynchronizer.Sync(binding));
            Assert.AreEqual(id, collection.SharedData.GetId("greeting"));
            Assert.AreEqual("Welcome {0}", Table("en").GetEntry(id).Value);
            Assert.IsNull(collection.SharedData.GetEntry("close"));
            Assert.IsNull(Table("zh-CN").GetEntry("close"));
            Assert.AreEqual("New entry", Table("en").GetEntry("new").Value);
            Assert.AreEqual(tableGuid, AssetDatabase.AssetPathToGUID(tablePath));
            Assert.AreEqual(collectionGuid, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(collection)));
            var before = File.ReadAllBytes(tablePath);
            Assert.IsFalse(ExcelTableSynchronizer.Sync(binding));
            CollectionAssert.AreEqual(before, File.ReadAllBytes(tablePath));
            ExcelTableSynchronizer.Verify(binding);
        }

        [Test]
        public void RemovingLocaleColumnRemovesOnlyItsTableAndKeepsSharedLocale()
        {
            ExcelTableSynchronizer.Sync(binding);
            var english = LocalizationEditorSettings.GetLocales().Single(locale => locale.Identifier.Code == "en");
            var englishPath = AssetDatabase.GetAssetPath(Table("en"));
            Write(new[] { "Key", "zh-CN" }, new[] { "greeting", "你好 {0}" });
            ExcelTableSynchronizer.Sync(binding);
            Assert.IsNull(Table("en"));
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<StringTable>(englishPath));
            Assert.IsTrue(LocalizationEditorSettings.GetLocales().Contains(english));
            Write(new[] { "Key", "zh-CN", "en", "ja" }, new[] { "greeting", "你好 {0}", "Hello {0}", "こんにちは {0}" });
            ExcelTableSynchronizer.Sync(binding);
            Assert.AreEqual("こんにちは {0}", Table("ja").GetEntry("greeting").Value);
        }

        [TestCase("duplicate-key")]
        [TestCase("duplicate-locale")]
        [TestCase("invalid-locale")]
        [TestCase("blank-translation")]
        [TestCase("missing-argument")]
        [TestCase("malformed-format")]
        public void InvalidWorkbookLeavesEveryTargetAssetUnchanged(string error)
        {
            ExcelTableSynchronizer.Sync(binding);
            var assets = collection.StringTables.Select(AssetDatabase.GetAssetPath)
                .Concat(new[] { AssetDatabase.GetAssetPath(collection.SharedData), AssetDatabase.GetAssetPath(collection) })
                .ToDictionary(path => path, File.ReadAllBytes);
            var header = new[] { "Key", "zh-CN", "en" };
            var row = new[] { "greeting", "你好 {0}", "Hello {0}" };
            switch (error)
            {
                case "duplicate-key":
                    Write(header, row, row);
                    break;
                case "duplicate-locale":
                    header[2] = "zh-CN";
                    Write(header, row);
                    break;
                case "invalid-locale":
                    header[2] = "not-a-locale";
                    Write(header, row);
                    break;
                case "blank-translation":
                    row[2] = "";
                    Write(header, row);
                    break;
                case "missing-argument":
                    row[2] = "Hello";
                    Write(header, row);
                    break;
                case "malformed-format":
                    row[2] = "Hello {0";
                    Write(header, row);
                    break;
            }

            Assert.Throws<InvalidDataException>(() => ExcelTableSynchronizer.Sync(binding));
            foreach (var asset in assets)
            {
                CollectionAssert.AreEqual(asset.Value, File.ReadAllBytes(asset.Key), asset.Key);
            }
        }

        [Test]
        public void MissingSourcePreservesEntriesAndFailsTheBuildGuard()
        {
            ExcelTableSynchronizer.Sync(binding);
            File.Delete(workbookPath);
            Assert.Throws<FileNotFoundException>(() => ExcelTableSynchronizer.Sync(binding));
            Assert.AreEqual("Hello {0}", Table("en").GetEntry("greeting").Value);
            Assert.Throws<UnityEditor.Build.BuildFailedException>(() => new ExcelTableBuildGuard().OnPreprocessBuild(null));
        }

        [Test]
        public void EmptyWorkbookDeletesEntriesAndManualTableChangesAreReplaced()
        {
            ExcelTableSynchronizer.Sync(binding);
            Table("en").GetEntry("close").Value = "Changed by hand";
            Assert.Throws<InvalidDataException>(() => ExcelTableSynchronizer.Verify(binding));
            ExcelTableSynchronizer.Sync(binding);
            Assert.AreEqual("Close", Table("en").GetEntry("close").Value);
            Write(new[] { "Key", "zh-CN", "en" });
            ExcelTableSynchronizer.Sync(binding);
            Assert.IsEmpty(collection.SharedData.Entries);
            Assert.AreEqual(0, Table("en").Count);
        }

        [Test]
        public void ConflictingBindingsAndPathsOutsideProjectAreRejected()
        {
            var second = ScriptableObject.CreateInstance<ExcelTableBinding>();
            second.Configure(workbookPath, collection, automatic: false);
            AssetDatabase.CreateAsset(second, directory + "/Duplicate.asset");
            Assert.Throws<InvalidDataException>(() => ExcelTableSynchronizer.Sync(binding));
            binding.Configure("../Outside.xlsx", collection, automatic: false);
            Assert.Throws<InvalidDataException>(() => binding.GetFullPath());
            binding.Configure(Path.GetFullPath(workbookPath), collection, automatic: false);
            Assert.Throws<InvalidDataException>(() => binding.GetFullPath());
        }

        [UnityTest]
        public IEnumerator FileChangesOutsideAssetsAreSynchronizedWithoutManualImport()
        {
            binding.Configure(workbookPath, collection);
            yield return WaitUntil(() => Table("en")?.GetEntry("close")?.Value == "Close");
            var id = collection.SharedData.GetId("close");
            Write(new[] { "Key", "zh-CN", "en" }, new[] { "close", "返回", "Back" });
            yield return WaitUntil(() => Table("en")?.GetEntry("close")?.Value == "Back");
            Assert.AreEqual(id, collection.SharedData.GetId("close"));
            Assert.IsNull(Table("en").GetEntry("greeting"));
        }

        private static IEnumerator WaitUntil(Func<bool> condition)
        {
            var deadline = EditorApplication.timeSinceStartup + 12;
            while (!condition() && EditorApplication.timeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.IsTrue(condition(), "The editor did not synchronize the changed workbook.");
        }
    }
}
