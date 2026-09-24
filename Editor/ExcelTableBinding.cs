using System;
using System.IO;
using UnityEditor.Localization;
using UnityEngine;

namespace VM233.ExcelLocalization
{
    [CreateAssetMenu(menuName = "Localization/Excel Table Binding", fileName = "ExcelTableBinding")]
    public sealed class ExcelTableBinding : ScriptableObject
    {
        [SerializeField]
        private string sourcePath;

        [SerializeField]
        private string worksheet = "Strings";

        [SerializeField]
        private StringTableCollection collection;

        [SerializeField]
        private bool autoSync = true;

        public string SourcePath => sourcePath;

        public string Worksheet => worksheet;

        public StringTableCollection Collection => collection;

        public bool AutoSync => autoSync;

        public void Configure(string path, StringTableCollection target, string sheet = "Strings", bool automatic = true)
        {
            sourcePath = path.Replace('\\', '/');
            collection = target;
            worksheet = sheet;
            autoSync = automatic;
        }

        public string GetFullPath()
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || Path.IsPathRooted(sourcePath))
            {
                throw new InvalidDataException(name + ": use a project-relative Excel path.");
            }

            var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var fullPath = Path.GetFullPath(Path.Combine(root, sourcePath));
            if (!fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(Path.GetExtension(fullPath), ".xlsx", StringComparison.OrdinalIgnoreCase) ||
                Path.GetFileName(fullPath).StartsWith("~$", StringComparison.Ordinal))
            {
                throw new InvalidDataException(name + ": select an .xlsx file inside the project, not an Excel lock file.");
            }

            return fullPath;
        }
    }
}
