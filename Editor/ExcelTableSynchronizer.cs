using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace VM233.ExcelLocalization
{
    public static class ExcelTableSynchronizer
    {
        public static ExcelTableBinding[] FindBindings() => AssetDatabase.FindAssets("t:ExcelTableBinding")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.StartsWith("Assets/", StringComparison.Ordinal))
            .Select(AssetDatabase.LoadAssetAtPath<ExcelTableBinding>).Where(binding => binding != null).ToArray();

        public static int SyncAll()
        {
            var bindings = FindBindings();
            // Validate every workbook before changing any collection.
            var data = bindings.Select(Read).ToArray();
            var changed = 0;
            for (var i = 0; i < bindings.Length; i++)
            {
                if (Apply(bindings[i], data[i]))
                {
                    changed++;
                }
            }

            return changed;
        }

        public static bool Sync(ExcelTableBinding binding) => Apply(binding, Read(binding));

        public static void Verify(ExcelTableBinding binding)
        {
            if (!Matches(binding.Collection, Read(binding)))
            {
                throw new InvalidDataException(binding.name + ": Localization tables do not match Excel.");
            }
        }

        private static ExcelTableData Read(ExcelTableBinding binding)
        {
            if (binding == null || binding.Collection == null || binding.Collection.SharedData == null ||
                !AssetDatabase.GetAssetPath(binding.Collection).StartsWith("Assets/", StringComparison.Ordinal))
            {
                throw new InvalidDataException("An Excel binding needs a saved String Table Collection under Assets.");
            }

            if (FindBindings().Any(other => other != binding && other.Collection == binding.Collection))
            {
                throw new InvalidDataException(binding.name + ": another Excel binding owns this collection.");
            }

            return ExcelTableData.Read(binding.GetFullPath(), binding.Worksheet);
        }

        private static bool Matches(StringTableCollection collection, ExcelTableData data)
        {
            if (collection.SharedData.Entries.Count != data.Entries.Count ||
                collection.StringTables.Count != data.Locales.Length)
            {
                return false;
            }

            for (var i = 0; i < data.Locales.Length; i++)
            {
                var table = collection.GetTable(new LocaleIdentifier(data.Locales[i])) as StringTable;
                if (table == null || table.Count != data.Entries.Count)
                {
                    return false;
                }

                foreach (var row in data.Entries)
                {
                    var entry = table.GetEntry(row.Key);
                    if (entry == null || entry.Value != row.Value[i] || entry.IsSmart)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Apply(ExcelTableBinding binding, ExcelTableData data)
        {
            var collection = binding.Collection;
            if (Matches(collection, data))
            {
                return false;
            }

            var directory = Path.GetDirectoryName(AssetDatabase.GetAssetPath(collection)).Replace('\\', '/');
            foreach (var code in data.Locales)
            {
                EnsureLocale(code, directory);
                var identifier = new LocaleIdentifier(code);
                if (collection.GetTable(identifier) == null)
                {
                    var path = AssetDatabase.GenerateUniqueAssetPath(
                        directory + "/" + collection.name + "_" + code + ".asset");
                    collection.AddNewTable(identifier, path);
                }
            }

            foreach (var table in collection.StringTables.ToArray())
            {
                if (!data.Locales.Contains(table.LocaleIdentifier.Code))
                {
                    var path = AssetDatabase.GetAssetPath(table);
                    collection.RemoveTable(table);
                    AssetDatabase.DeleteAsset(path);
                }
            }

            foreach (var entry in collection.SharedData.Entries.ToArray())
            {
                if (!data.Entries.ContainsKey(entry.Key))
                {
                    collection.RemoveEntry(entry.Id);
                }
            }

            for (var i = 0; i < data.Locales.Length; i++)
            {
                var table = (StringTable)collection.GetTable(new LocaleIdentifier(data.Locales[i]));
                foreach (var row in data.Entries)
                {
                    var entry = table.GetEntry(row.Key);
                    if (entry == null)
                    {
                        entry = table.AddEntry(row.Key, row.Value[i]);
                    }
                    else if (entry.Value != row.Value[i])
                    {
                        entry.Value = row.Value[i];
                    }

                    entry.IsSmart = false;
                }

                EditorUtility.SetDirty(table);
            }

            EditorUtility.SetDirty(collection.SharedData);
            EditorUtility.SetDirty(collection);
            AssetDatabase.SaveAssets();
            return true;
        }

        private static void EnsureLocale(string code, string directory)
        {
            if (LocalizationEditorSettings.GetLocales().Any(locale => locale.Identifier.Code == code))
            {
                return;
            }

            var localeDirectory = directory + "/Locales";
            if (!AssetDatabase.IsValidFolder(localeDirectory))
            {
                AssetDatabase.CreateFolder(directory, "Locales");
            }

            var locale = Locale.CreateLocale(code);
            AssetDatabase.CreateAsset(locale,
                AssetDatabase.GenerateUniqueAssetPath(localeDirectory + "/" + code + ".asset"));
            LocalizationEditorSettings.AddLocale(locale);
        }
    }
}
