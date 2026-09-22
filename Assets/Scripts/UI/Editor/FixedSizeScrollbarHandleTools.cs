using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Editor helpers for adding FixedSizeScrollbarHandle to every Scrollbar in the
/// project in one go (Tools > Scrollbars).
///
/// Project-wide: adds it to every Scrollbar in the project's own prefabs
/// (Assets/Prefabs and Assets/Scripts) and in the scenes that are OPEN right now.
/// Prefab files are saved by this; scenes are only marked dirty - save them
/// yourself. To cover a scene that isn't open, open it and run this again.
/// Scrollbars that are part of a prefab INSTANCE are skipped on purpose: they
/// get the component from the prefab asset itself (adding it again in the
/// scene/outer prefab would create a duplicate override).
/// </summary>
public static class FixedSizeScrollbarHandleTools
{
    private static readonly string[] PrefabFolders = { "Assets/Prefabs", "Assets/Scripts" };

    [MenuItem("Tools/Scrollbars/Add Fixed Handle To All Scrollbars")]
    private static void AddToAll()
    {
        bool go = EditorUtility.DisplayDialog(
            "Add Fixed Handle To All Scrollbars",
            "This adds FixedSizeScrollbarHandle to every Scrollbar in the project's prefabs " +
            "(Assets/Prefabs, Assets/Scripts) and in the currently open scenes.\n\n" +
            "Prefab files are SAVED automatically (prefabs open in Prefab Mode are skipped). " +
            "Open scenes are only marked as changed - save them afterwards.\n\n" +
            "Continue?",
            "Add", "Cancel");
        if (!go) return;

        int added = 0, prefabsSaved = 0, skippedInstances = 0, skippedOpenPrefabs = 0, scenesChanged = 0;
        var openStage = PrefabStageUtility.GetCurrentPrefabStage();

        try
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", PrefabFolders))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (openStage != null && openStage.assetPath == path)
                {
                    skippedOpenPrefabs++;
                    continue;
                }

                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    int here = AddInHierarchy(root, false, ref skippedInstances);
                    if (here > 0)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        prefabsSaved++;
                        added += here;
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                int here = 0;
                foreach (GameObject root in scene.GetRootGameObjects())
                    here += AddInHierarchy(root, true, ref skippedInstances);

                if (here > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    scenesChanged++;
                    added += here;
                }
            }
        }
        finally
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        string summary =
            $"Added FixedSizeScrollbarHandle to {added} scrollbar(s): {prefabsSaved} prefab(s) saved, " +
            $"{scenesChanged} open scene(s) changed (save them).\n" +
            $"Skipped {skippedInstances} scrollbar(s) that belong to prefab instances (they take it from their prefab) " +
            $"and {skippedOpenPrefabs} prefab(s) currently open in Prefab Mode.";

        Debug.Log("[Scrollbars] " + summary);
        EditorUtility.DisplayDialog("Fixed Scrollbar Handles", summary, "OK");
    }

    [MenuItem("Tools/Scrollbars/Add Fixed Handle To Selected")]
    private static void AddToSelection()
    {
        int added = 0;

        foreach (GameObject selected in Selection.gameObjects)
        {
            foreach (Scrollbar bar in selected.GetComponentsInChildren<Scrollbar>(true))
            {
                if (bar.GetComponent<FixedSizeScrollbarHandle>() != null) continue;

                Undo.AddComponent<FixedSizeScrollbarHandle>(bar.gameObject);
                added++;
            }
        }

        Debug.Log($"[Scrollbars] Added FixedSizeScrollbarHandle to {added} selected scrollbar(s).");
    }

    /// <summary>Adds the component to every Scrollbar under root that doesn't have it (and isn't part of a prefab instance). Returns how many were added.</summary>
    private static int AddInHierarchy(GameObject root, bool withUndo, ref int skippedInstances)
    {
        int added = 0;

        foreach (Scrollbar bar in root.GetComponentsInChildren<Scrollbar>(true))
        {
            if (bar.GetComponent<FixedSizeScrollbarHandle>() != null) continue;

            if (PrefabUtility.IsPartOfPrefabInstance(bar.gameObject))
            {
                skippedInstances++;
                continue;
            }

            if (withUndo) Undo.AddComponent<FixedSizeScrollbarHandle>(bar.gameObject);
            else bar.gameObject.AddComponent<FixedSizeScrollbarHandle>();

            added++;
        }

        return added;
    }
}
