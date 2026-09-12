using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Auxilium.DevToolbox
{
    /// <summary>
    /// Swaps the selected objects for copies of a prefab, keeping each one's position, rotation and
    /// scale. Perfect for blocking out a level with cubes and then replacing them all with the real
    /// art in one go.
    /// </summary>
    public class ReplaceWithPrefab : EditorWindow
    {
        private GameObject m_Prefab;
        private bool m_KeepName;

        [MenuItem("Tools/Auxilium/Dev Toolbox/Replace With Prefab")]
        public static void Open()
        {
            var window = GetWindow<ReplaceWithPrefab>(false, "Replace", true);
            window.minSize = new Vector2(320f, 200f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(4f);
            ToolboxUI.Header("🔁", "Replace With Prefab", ToolboxUI.Object);

            this.m_Prefab = (GameObject) EditorGUILayout.ObjectField("Prefab", this.m_Prefab, typeof(GameObject), false);
            this.m_KeepName = EditorGUILayout.ToggleLeft("Keep each old object's name", this.m_KeepName);

            int count = Selection.transforms.Length;
            EditorGUILayout.HelpBox(count == 0 ? "Select the objects to replace." : $"{count} object(s) will be replaced.",
                count == 0 ? MessageType.Warning : MessageType.Info);

            EditorGUILayout.Space(4f);
            using (new EditorGUI.DisabledScope(this.m_Prefab == null || count == 0))
            {
                if (GUILayout.Button("Replace", GUILayout.Height(26f))) this.Replace();
            }
        }

        private void Replace()
        {
            Transform[] targets = Selection.transforms;
            var created = new List<GameObject>();

            foreach (Transform t in targets)
            {
                GameObject instance = (GameObject) PrefabUtility.InstantiatePrefab(this.m_Prefab, t.parent);
                Undo.RegisterCreatedObjectUndo(instance, "Replace With Prefab");

                instance.transform.SetPositionAndRotation(t.position, t.rotation);
                instance.transform.localScale = t.localScale;
                instance.transform.SetSiblingIndex(t.GetSiblingIndex());

                if (this.m_KeepName) instance.name = t.name;

                created.Add(instance);
                Undo.DestroyObjectImmediate(t.gameObject);
            }

            Selection.objects = created.ToArray();
        }
    }
}
