using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace VM233.ExcelLocalization
{
    [InitializeOnLoad]
    public static class ExcelTableAutoSync
    {
        private const double POLL_SECONDS = 1;
        private static readonly Dictionary<int, State> states = new Dictionary<int, State>();
        private static double nextPoll;

        private sealed class State
        {
            public string observed;
            public string applied;
            public string error;
            public string status = "Waiting for a stable workbook.";
        }

        static ExcelTableAutoSync()
        {
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        public static string GetStatus(ExcelTableBinding binding) =>
            states.TryGetValue(binding.GetInstanceID(), out var state) ? state.status : "Not synchronized in this session.";

        [MenuItem("Tools/Excel Localization/Sync All")]
        public static void SyncAll()
        {
            var count = ExcelTableSynchronizer.SyncAll();
            Debug.Log($"Excel Localization: synchronized {count} changed collections.");
        }

        private static void Update()
        {
            if (EditorApplication.timeSinceStartup < nextPoll || EditorApplication.isCompiling ||
                EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode ||
                BuildPipeline.isBuildingPlayer)
            {
                return;
            }

            nextPoll = EditorApplication.timeSinceStartup + POLL_SECONDS;
            var active = new HashSet<int>();
            foreach (var binding in ExcelTableSynchronizer.FindBindings())
            {
                var id = binding.GetInstanceID();
                active.Add(id);
                if (!binding.AutoSync || binding.Collection == null || string.IsNullOrWhiteSpace(binding.SourcePath))
                {
                    continue;
                }

                if (!states.TryGetValue(id, out var state))
                {
                    states.Add(id, state = new State());
                }

                try
                {
                    var fingerprint = Fingerprint(binding);
                    if (state.observed != fingerprint)
                    {
                        state.observed = fingerprint;
                        state.status = "Waiting for the Excel save to finish.";
                        continue;
                    }

                    if (state.applied == fingerprint)
                    {
                        state.error = null;
                        state.status = "Already matches Excel.";
                        continue;
                    }

                    var changed = ExcelTableSynchronizer.Sync(binding);
                    state.applied = fingerprint;
                    state.error = null;
                    state.status = changed ? "Synchronized from Excel." : "Already matches Excel.";
                }
                catch (Exception exception)
                {
                    state.status = exception.Message;
                    if (state.error != state.status)
                    {
                        state.error = state.status;
                        Debug.LogError("Excel Localization: " + state.status, binding);
                    }
                }
            }

            foreach (var id in new List<int>(states.Keys))
            {
                if (!active.Contains(id))
                {
                    states.Remove(id);
                }
            }
        }

        private static string Fingerprint(ExcelTableBinding binding)
        {
            using (var file = new FileStream(binding.GetFullPath(), FileMode.Open, FileAccess.Read,
                       FileShare.ReadWrite | FileShare.Delete))
            {
                using (var hash = SHA256.Create())
                {
                    return binding.SourcePath + "|" + binding.Worksheet + "|" +
                        AssetDatabase.GetAssetPath(binding.Collection) + "|" +
                        BitConverter.ToString(hash.ComputeHash(file));
                }
            }
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
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
