using System;
using System.IO;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;

namespace VM233.ExcelLocalization
{
    [CreateAssetMenu(menuName = "Localization/Excel Table Binding", fileName = "ExcelTableBinding")]
    public sealed class ExcelTableBinding : ScriptableObject
    {
        [SerializeField]
        private DefaultAsset workbook;

        [SerializeField]
        private string worksheet = "Strings";

        [SerializeField]
        private StringTableCollection collection;

        [SerializeField]
        private bool autoSync = true;

        public string SourcePath => AssetDatabase.GetAssetPath(workbook);

        public string Worksheet => worksheet;

        public StringTableCollection Collection => collection;

        public bool AutoSync => autoSync;

        public void Configure(string path, StringTableCollection target, string sheet = "Strings", bool automatic = true)
        {
            path = path.Replace('\\', '/');
            ValidatePath(path);
            workbook = AssetDatabase.LoadAssetAtPath<DefaultAsset>(path);
            if (workbook == null)
            {
                throw new InvalidDataException(path + ": import the Excel file into Assets before assigning it.");
            }
            collection = target;
            worksheet = sheet;
            autoSync = automatic;
        }

        public string GetFullPath()
        {
            return ValidatePath(SourcePath);
        }

        private static string ValidatePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !path.StartsWith("Assets/", StringComparison.Ordinal))
            {
                throw new InvalidDataException("Assign an Excel asset inside Assets.");
            }

            var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var fullPath = Path.GetFullPath(Path.Combine(root, path));
            if (!fullPath.StartsWith(Path.GetFullPath(Application.dataPath) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(Path.GetExtension(fullPath), ".xlsx", StringComparison.OrdinalIgnoreCase) ||
                Path.GetFileName(fullPath).StartsWith("~$", StringComparison.Ordinal))
            {
                throw new InvalidDataException("Select an .xlsx asset inside Assets, not an Excel lock file.");
            }

            return fullPath;
        }
    }
}
