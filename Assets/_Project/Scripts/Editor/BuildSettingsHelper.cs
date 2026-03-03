using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TurnBasedTactics.Editor
{
    /// <summary>
    /// Editor utility that ensures all _Project scenes are registered in Build Settings.
    /// Run via menu: TurnBasedTactics → Setup Build Settings.
    /// Also auto-runs on first domain reload if scenes are missing.
    /// </summary>
    public static class BuildSettingsHelper
    {
        private const string ProjectScenesPath = "Assets/_Project/Scenes";

        [MenuItem("TurnBasedTactics/Setup Build Settings")]
        public static void SetupBuildSettings()
        {
            var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { ProjectScenesPath });
            if (sceneGuids.Length == 0)
            {
                Debug.LogWarning("[BuildSettingsHelper] No scenes found in " + ProjectScenesPath);
                return;
            }

            var existingScenes = new HashSet<string>(
                EditorBuildSettings.scenes.Select(s => s.path));

            var newScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            int added = 0;

            foreach (var guid in sceneGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!existingScenes.Contains(path))
                {
                    newScenes.Add(new EditorBuildSettingsScene(path, true));
                    added++;
                    Debug.Log($"[BuildSettingsHelper] Added scene to build: {path}");
                }
            }

            if (added > 0)
            {
                EditorBuildSettings.scenes = newScenes.ToArray();
                Debug.Log($"[BuildSettingsHelper] Build settings updated. {added} scene(s) added. Total: {newScenes.Count}");
            }
            else
            {
                Debug.Log("[BuildSettingsHelper] All project scenes already in build settings.");
            }
        }

        /// <summary>
        /// Auto-check on domain reload — adds missing scenes without prompt.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void AutoCheck()
        {
            // Only run if build settings are empty (first time setup)
            if (EditorBuildSettings.scenes.Length == 0)
            {
                Debug.Log("[BuildSettingsHelper] Build settings empty — auto-adding project scenes.");
                SetupBuildSettings();
            }
        }
    }
}
