using System.IO;
using UnityEditor;
using UnityEngine;

namespace Auxilium.DevToolbox
{
    /// <summary>
    /// A scratch pad for the project. Jot down todos, reminders and handover notes, and they stay
    /// with the project rather than a sticky note on your monitor. Saved next to the project
    /// settings, so it travels with the repo but never ends up in a build.
    /// </summary>
    public class ProjectNotes : EditorWindow
    {
        private const string FileName = "AuxiliumNotes.txt";

        private string m_Text = string.Empty;
        private Vector2 m_Scroll;
        private bool m_Loaded;

        [MenuItem("Tools/Auxilium/Dev Toolbox/Project Notes")]
        public static void Open()
        {
            var window = GetWindow<ProjectNotes>(false, "Notes", true);
            window.minSize = new Vector2(320f, 260f);
            window.Show();
        }

        private static string FilePath()
        {
            string projectSettings = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "ProjectSettings");
            return Path.Combine(projectSettings, FileName);
        }

        private void OnEnable()
        {
            this.Load();
        }

        private void Load()
        {
            string path = FilePath();
            this.m_Text = File.Exists(path) ? File.ReadAllText(path) : string.Empty;
            this.m_Loaded = true;
        }

        private void Save()
        {
            File.WriteAllText(FilePath(), this.m_Text);
        }

        private void OnGUI()
        {
            if (!this.m_Loaded) this.Load();

            EditorGUILayout.Space(4f);
            ToolboxUI.Header("📝", "Project Notes", ToolboxUI.Data);

            this.m_Scroll = EditorGUILayout.BeginScrollView(this.m_Scroll);
            var area = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
            string edited = EditorGUILayout.TextArea(this.m_Text, area, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            if (edited != this.m_Text)
            {
                this.m_Text = edited;
                this.Save();
            }

            EditorGUILayout.LabelField("Saved to ProjectSettings, travels with the repo.", EditorStyles.miniLabel);
        }
    }
}
