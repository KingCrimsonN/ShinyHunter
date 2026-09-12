using UnityEditor;
using UnityEngine;

namespace Auxilium.DevToolbox
{
    /// <summary>
    /// Renames all selected objects at once with a base name, a running number, and an optional
    /// find and replace. Works on scene objects and on project assets.
    /// </summary>
    public class BatchRename : EditorWindow
    {
        private string m_BaseName = "Object";
        private int m_StartNumber = 1;
        private int m_Padding = 2;
        private bool m_UseNumber = true;

        private string m_Find = string.Empty;
        private string m_Replace = string.Empty;

        [MenuItem("Tools/Auxilium/Dev Toolbox/Batch Rename")]
        public static void Open()
        {
            var window = GetWindow<BatchRename>(false, "Batch Rename", true);
            window.minSize = new Vector2(340f, 300f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(4f);
            ToolboxUI.Header("✏️", "Batch Rename", ToolboxUI.Object);

            int count = Selection.objects.Length;
            EditorGUILayout.HelpBox(
                count == 0 ? "Select some objects or assets to rename." : $"{count} object(s) selected.",
                count == 0 ? MessageType.Warning : MessageType.Info);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Rename With A Number", EditorStyles.boldLabel);
            this.m_UseNumber = EditorGUILayout.Toggle("Use Numbering", this.m_UseNumber);

            using (new EditorGUI.DisabledScope(!this.m_UseNumber))
            {
                this.m_BaseName = EditorGUILayout.TextField("Base Name", this.m_BaseName);
                this.m_StartNumber = EditorGUILayout.IntField("Start Number", this.m_StartNumber);
                this.m_Padding = Mathf.Clamp(EditorGUILayout.IntField("Digits", this.m_Padding), 0, 8);

                if (this.m_UseNumber)
                {
                    EditorGUILayout.LabelField("Preview", $"{this.m_BaseName}{this.m_StartNumber.ToString().PadLeft(this.m_Padding, '0')}", EditorStyles.miniLabel);
                }
            }

            using (new EditorGUI.DisabledScope(count == 0 || !this.m_UseNumber))
            {
                if (GUILayout.Button("Rename With Number", GUILayout.Height(24f))) this.RenameNumbered();
            }

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Find And Replace", EditorStyles.boldLabel);
            this.m_Find = EditorGUILayout.TextField("Find", this.m_Find);
            this.m_Replace = EditorGUILayout.TextField("Replace", this.m_Replace);

            using (new EditorGUI.DisabledScope(count == 0 || string.IsNullOrEmpty(this.m_Find)))
            {
                if (GUILayout.Button("Find And Replace", GUILayout.Height(24f))) this.RenameReplace();
            }
        }

        private void RenameNumbered()
        {
            Object[] objects = Selection.objects;
            int number = this.m_StartNumber;

            foreach (Object obj in objects)
            {
                string name = this.m_BaseName + number.ToString().PadLeft(this.m_Padding, '0');
                this.Apply(obj, name);
                number++;
            }

            AssetDatabase.SaveAssets();
        }

        private void RenameReplace()
        {
            foreach (Object obj in Selection.objects)
            {
                if (obj == null || string.IsNullOrEmpty(obj.name)) continue;
                string name = obj.name.Replace(this.m_Find, this.m_Replace);
                this.Apply(obj, name);
            }

            AssetDatabase.SaveAssets();
        }

        private void Apply(Object obj, string newName)
        {
            if (obj == null || newName == obj.name) return;

            string path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path) && AssetDatabase.IsMainAsset(obj))
            {
                AssetDatabase.RenameAsset(path, newName);
            }
            else
            {
                Undo.RecordObject(obj, "Batch Rename");
                obj.name = newName;
            }
        }
    }
}
