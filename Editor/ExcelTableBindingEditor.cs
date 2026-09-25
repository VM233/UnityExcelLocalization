using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VM233.ExcelLocalization
{
    [CustomEditor(typeof(ExcelTableBinding))]
    public sealed class ExcelTableBindingEditor : UnityEditor.Editor
    {
        private string message;

        public override void OnInspectorGUI()
        {
            var binding = (ExcelTableBinding)target;
            if (DrawDefaultInspector())
            {
                ExcelTableAutoSync.RequestSync(binding);
            }
            if (GUILayout.Button("Choose Excel File"))
            {
                var path = EditorUtility.OpenFilePanel("Select localization workbook", "", "xlsx");
                if (!string.IsNullOrEmpty(path))
                {
                    var root = Path.GetFullPath(Application.dataPath)
                        .Replace('\\', '/') + "/";
                    path = Path.GetFullPath(path).Replace('\\', '/');
                    if (path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                    {
                        Undo.RecordObject(binding, "Choose Excel workbook");
                        var assetPath = "Assets/" + path.Substring(root.Length);
                        AssetDatabase.ImportAsset(assetPath);
                        binding.Configure(assetPath, binding.Collection, binding.Worksheet,
                            binding.AutoSync);
                        EditorUtility.SetDirty(binding);
                        ExcelTableAutoSync.RequestSync(binding);
                        message = null;
                    }
                    else
                    {
                        message = "Choose a workbook inside Assets.";
                    }
                }
            }

            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("Sync Now"))
                {
                    try
                    {
                        message = ExcelTableSynchronizer.Sync(binding)
                            ? "Synchronized from Excel." : "Already matches Excel.";
                    }
                    catch (Exception exception)
                    {
                        message = exception.Message;
                        Debug.LogError("Excel Localization: " + message, binding);
                    }
                }
            }

            EditorGUILayout.HelpBox(message ?? ExcelTableAutoSync.GetStatus(binding), MessageType.Info);
        }
    }
}
