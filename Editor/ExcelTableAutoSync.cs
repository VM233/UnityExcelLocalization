using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VM233.ExcelLocalization
{
    [InitializeOnLoad]
    public static class ExcelTableAutoSync
    {
        private static readonly HashSet<string> changedAssets = new HashSet<string>(StringComparer.Ordinal);
        private static readonly Dictionary<int, string> statuses = new Dictionary<int, string>();
        private static bool synchronizeAll = true;
        private static bool scheduled;

        static ExcelTableAutoSync()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            Schedule();
        }

        public static string GetStatus(ExcelTableBinding binding)
        {
            if (!binding.AutoSync)
            {
                return "Automatic synchronization is disabled.";
            }

            return statuses.TryGetValue(binding.GetInstanceID(), out var status)
                ? status : "Waiting for the next Excel import.";
        }

        [MenuItem("Tools/Excel Localization/Sync All")]
        public static void SyncAll()
        {
            var count = ExcelTableSynchronizer.SyncAll();
            Debug.Log($"Excel Localization: synchronized {count} changed collections.");
        }

        public static void RequestSync(ExcelTableBinding binding)
        {
            changedAssets.Add(AssetDatabase.GetAssetPath(binding));
            Schedule();
        }

        internal static void OnAssetsImported(string[] imported, string[] deleted, string[] moved,
            string[] movedFrom, bool domainReload)
        {
            synchronizeAll |= domainReload || deleted.Any(IsWorkbook);
            foreach (var path in imported.Concat(deleted).Concat(moved).Concat(movedFrom))
            {
                if (IsWorkbook(path) || path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                {
                    changedAssets.Add(path);
                }
            }

            if (synchronizeAll || changedAssets.Count > 0)
            {
                Schedule();
            }
        }

        private static bool IsWorkbook(string path) => path.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) &&
            !System.IO.Path.GetFileName(path).StartsWith("~$", StringComparison.Ordinal);

        private static void Schedule()
        {
            if (scheduled || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            scheduled = true;
            EditorApplication.delayCall += SynchronizeImportedAssets;
        }

        private static void SynchronizeImportedAssets()
        {
            scheduled = false;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating || BuildPipeline.isBuildingPlayer)
            {
                Schedule();
                return;
            }

            var paths = new HashSet<string>(changedAssets, StringComparer.Ordinal);
            var all = synchronizeAll;
            changedAssets.Clear();
            synchronizeAll = false;
            var bindings = ExcelTableSynchronizer.FindBindings();
            var active = new HashSet<int>(bindings.Select(binding => binding.GetInstanceID()));
            foreach (var id in statuses.Keys.Where(id => !active.Contains(id)).ToArray())
            {
                statuses.Remove(id);
            }

            foreach (var binding in bindings)
            {
                if (!binding.AutoSync || binding.Collection == null ||
                    (!all && !paths.Contains(binding.SourcePath) && !paths.Contains(AssetDatabase.GetAssetPath(binding))))
                {
                    continue;
                }

                var id = binding.GetInstanceID();
                try
                {
                    statuses[id] = ExcelTableSynchronizer.Sync(binding)
                        ? "Synchronized from Excel." : "Already matches Excel.";
                }
                catch (Exception exception)
                {
                    if (!statuses.TryGetValue(id, out var previous) || previous != exception.Message)
                    {
                        Debug.LogError("Excel Localization: " + exception.Message, binding);
                    }

                    statuses[id] = exception.Message;
                }
            }
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                Schedule();
                return;
            }

            if (state != PlayModeStateChange.ExitingEditMode)
            {
                return;
            }

            try
            {
                ExcelTableSynchronizer.SyncAll();
            }
            catch (Exception exception)
            {
                EditorApplication.isPlaying = false;
                Debug.LogError("Excel Localization: " + exception.Message);
            }
        }
    }
}
