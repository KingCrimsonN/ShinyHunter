using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Auxilium.DevToolbox
{
    /// <summary>
    /// Finds and removes the empty "missing script" slots left on your objects after a script or a
    /// package is deleted. They clutter the inspector and throw errors, so clearing them out in one
    /// sweep keeps a project healthy.
    /// </summary>
    public class MissingScriptRemover : EditorWindow
    {
        private Vector2 m_Scroll;
        private readonly List<string> m_Report = new List<string>();
        private int m_Found;

        [MenuItem("Tools/Auxilium/Dev Toolbox/Missing Script Remover")]
        public static void Open()
        {
            var window = GetWindow<MissingScriptRemover>(false, "Missing Scripts", true);
            window.minSize = new Vector2(360f, 300f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(4f);
            ToolboxUI.Header("🧹", "Missing Script Remover", ToolboxUI.Cleanup);
            EditorGUILayout.HelpBox(
                "Scans for the leftover components whose script was deleted or moved, and removes " +
                "them. These show up as a broken 'Missing Script' line in the inspector.",
                MessageType.Info);

            EditorGUILayout.Space(4f);

            if (GUILayout.Button("Scan And Clean Open Scenes", GUILayout.Height(26f))) this.CleanScenes();
            if (GUILayout.Button("Scan And Clean Selected Objects", GUILayout.Height(24f))) this.CleanSelection();
            if (GUILayout.Button("Scan And Clean All Prefabs", GUILayout.Height(24f))) this.CleanPrefabs();

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField($"Removed from {this.m_Found} object(s) last run.", EditorStyles.miniBoldLabel);

            this.m_Scroll = EditorGUILayout.BeginScrollView(this.m_Scroll);
            foreach (string line in this.m_Report) EditorGUILayout.LabelField(line, EditorStyles.miniLabel);
            EditorGUILayout.EndScrollView();
        }

        private void Begin()
        {
            this.m_Report.Clear();
            this.m_Found = 0;
        }

        private void CleanObject(GameObject go)
        {
            int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
            if (count <= 0) return;

            Undo.RegisterCompleteObjectUndo(go, "Remove Missing Scripts");
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            this.m_Found++;
            this.m_Report.Add($"{count} removed from {go.name}");
        }

        private void CleanScenes()
        {
            this.Begin();

            for (int s = 0; s < SceneManager.sceneCount; s++)
            {
                Scene scene = SceneManager.GetSceneAt(s);
                if (!scene.isLoaded) continue;

                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) this.CleanObject(t.gameObject);
                }
            }

            if (this.m_Found > 0) EditorSceneManager.MarkAllScenesDirty();
            this.Finish();
        }

        private void CleanSelection()
        {
            this.Begin();

            foreach (GameObject go in Selection.gameObjects)
            {
                foreach (Transform t in go.GetComponentsInChildren<Transform>(true)) this.CleanObject(t.gameObject);
            }

            if (this.m_Found > 0) EditorSceneManager.MarkAllScenesDirty();
            this.Finish();
        }

        private void CleanPrefabs()
        {
            if (!EditorUtility.DisplayDialog(
                "Clean All Prefabs",
                "This opens every prefab in the project, strips missing scripts, and saves it. " +
                "Make sure your work is saved or under version control first.",
                "Go Ahead", "Cancel"))
            {
                return;
            }

            this.Begin();

            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                EditorUtility.DisplayProgressBar("Missing Script Remover", path, (float) i / guids.Length);

                GameObject root = PrefabUtility.LoadPrefabContents(path);
                int before = this.m_Found;

                foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) this.CleanObject(t.gameObject);

                if (this.m_Found > before) PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
            }

            EditorUtility.ClearProgressBar();
            this.Finish();
        }

        private void Finish()
        {
            if (this.m_Found == 0) this.m_Report.Add("No missing scripts found. All clean.");
            this.Repaint();
        }
    }
}
