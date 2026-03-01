
#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace TurnBasedTactics.Editor
{
    /// <summary>
    /// One-shot editor utility: loads Demo_Forge_01 additively into the active combat scene
    /// and reparents the Forge environment under WorldRoot/ArenaGeometry.
    /// Run via menu: Tools > Import Forge Demo.
    /// </summary>
    public static class ForgeSceneImporter
    {
        private const string ForgeScenePath = "Assets/PolygonDungeonRealms/Scenes/Demo_Forge_01.unity";

        [MenuItem("Tools/Import Forge Demo")]
        public static void ImportForgeDemo()
        {
            // Find ArenaGeometry in the active scene
            var arenaGeometry = GameObject.Find("WorldRoot/ArenaGeometry");
            if (arenaGeometry == null)
            {
                Debug.LogError("[ForgeSceneImporter] Could not find WorldRoot/ArenaGeometry in the active scene. " +
                               "Make sure Combat_RuinsPrototype_01 is loaded.");
                return;
            }

            // Load Forge demo additively
            var forgeScene = EditorSceneManager.OpenScene(ForgeScenePath, OpenSceneMode.Additive);
            if (!forgeScene.IsValid())
            {
                Debug.LogError("[ForgeSceneImporter] Failed to load Demo_Forge_01.");
                return;
            }

            // Find the root objects in the Forge scene
            var forgeRoots = forgeScene.GetRootGameObjects();
            int movedCount = 0;

            foreach (var root in forgeRoots)
            {
                if (root.name == "Forge")
                {
                    // Move the entire Forge environment under ArenaGeometry
                    SceneManager.MoveGameObjectToScene(root, arenaGeometry.scene);
                    root.transform.SetParent(arenaGeometry.transform, true);
                    movedCount += root.transform.childCount;
                    Debug.Log($"[ForgeSceneImporter] Moved 'Forge' ({root.transform.childCount} children) under ArenaGeometry.");
                }
                else if (root.name == "Forge_Lights_Camera")
                {
                    // Move lights under WorldRoot (not ArenaGeometry, since lights are separate)
                    var worldRoot = GameObject.Find("WorldRoot");
                    if (worldRoot != null)
                    {
                        SceneManager.MoveGameObjectToScene(root, worldRoot.scene);
                        root.transform.SetParent(worldRoot.transform, true);
                        Debug.Log($"[ForgeSceneImporter] Moved 'Forge_Lights_Camera' under WorldRoot.");
                    }
                }
            }

            // Close the now-empty Forge scene
            EditorSceneManager.CloseScene(forgeScene, true);

            // Mark the active scene as dirty so it can be saved
            EditorSceneManager.MarkSceneDirty(arenaGeometry.scene);

            Debug.Log($"[ForgeSceneImporter] Import complete. Forge environment is now under ArenaGeometry.");
        }
    }
}
#endif
