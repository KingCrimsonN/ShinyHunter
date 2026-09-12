using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Auxilium.DevToolbox
{
    /// <summary>
    /// Lists every scene in the project and opens any of them with one click, so you stop hunting
    /// through the Project window to switch scenes. It offers to save the current scene first.
    /// </summary>
    public class SceneQuickSwitch : EditorWindow
    {
        private Vector2 m_Scroll;
        private string m_Filter = string.Empty;
        private readonly List<string> m_Paths = new List<string>();

        [MenuItem("Tools/Auxilium/Dev Toolbox/Scene Quick Switch")]
        public static void Open()
        {
            var window = GetWindow<SceneQuickSwitch>(false, "Scenes", true);
            window.minSize = new Vector2(300f, 320f);
            window.Refresh();
            window.Show();
        }

        private void OnFocus() => this.Refresh();

        private void Refresh()
        {
            this.m_Paths.Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:Scene"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith("Assets/")) this.m_Paths.Add(path);
            }
            this.m_Paths.Sort();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(4f);
            ToolboxUI.Header("🕹️", "Scene Quick Switch", ToolboxUI.Scene);
            this.m_Filter = EditorGUILayout.TextField("Filter", this.m_Filter);
            EditorGUILayout.Space(4f);

            this.m_Scroll = EditorGUILayout.BeginScrollView(this.m_Scroll);

            foreach (string path in this.m_Paths)
            {
                string name = Path.GetFileNameWithoutExtension(path);
                if (!string.IsNullOrEmpty(this.m_Filter) && name.IndexOf(this.m_Filter, System.StringComparison.OrdinalIgnoreCase) < 0) continue;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(name, GUILayout.MinWidth(80f));

                if (GUILayout.Button("Open", GUILayout.Width(56f)))
                {
                    this.OpenScene(path, OpenSceneMode.Single);
                }
                if (GUILayout.Button("Add", GUILayout.Width(46f)))
                {
                    this.OpenScene(path, OpenSceneMode.Additive);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            if (this.m_Paths.Count == 0) EditorGUILayout.HelpBox("No scenes found in the project.", MessageType.Info);
        }

        private void OpenScene(string path, OpenSceneMode mode)
        {
            if (mode == OpenSceneMode.Single && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(path, mode);
        }
    }
}
