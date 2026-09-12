using UnityEditor;
using UnityEngine;

namespace Auxilium.DevToolbox
{
    /// <summary>
    /// Saves and restores Scene view camera positions, so you can flip between the spots you keep
    /// coming back to, a boss arena, a menu, a far corner, without flying there each time.
    /// </summary>
    public class CameraBookmarks : EditorWindow
    {
        private const int Slots = 6;
        private const string Prefix = "Auxilium.DevToolbox.CamBookmark.";

        [MenuItem("Tools/Auxilium/Dev Toolbox/Camera Bookmarks")]
        public static void Open()
        {
            var window = GetWindow<CameraBookmarks>(false, "Camera Bookmarks", true);
            window.minSize = new Vector2(320f, 260f);
            window.Show();
        }

        private static string Key(int slot) => Prefix + Application.dataPath.GetHashCode() + "." + slot;

        private void OnGUI()
        {
            EditorGUILayout.Space(4f);
            ToolboxUI.Header("🔖", "Camera Bookmarks", ToolboxUI.Scene);

            EditorGUILayout.HelpBox("These remember where the Scene view is looking, not your Camera objects. Save spots you keep coming back to, then Go to fly there.", MessageType.Info);

            SceneView sv = SceneView.lastActiveSceneView;
            if (sv == null)
            {
                EditorGUILayout.HelpBox("Open a Scene view and click in it first, then try again.", MessageType.Info);
                return;
            }

            for (int i = 0; i < Slots; i++)
            {
                bool has = EditorPrefs.HasKey(Key(i) + ".set");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Slot {i + 1}" + (has ? "  (saved)" : ""), GUILayout.Width(110f));

                if (GUILayout.Button("Save")) this.Save(sv, i);

                using (new EditorGUI.DisabledScope(!has))
                {
                    if (GUILayout.Button("Go")) this.Restore(sv, i);
                    if (GUILayout.Button("Clear", GUILayout.Width(50f))) this.Clear(i);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void Save(SceneView sv, int slot)
        {
            string k = Key(slot);
            EditorPrefs.SetBool(k + ".set", true);
            SetVector(k + ".pivot", sv.pivot);
            EditorPrefs.SetFloat(k + ".rx", sv.rotation.x);
            EditorPrefs.SetFloat(k + ".ry", sv.rotation.y);
            EditorPrefs.SetFloat(k + ".rz", sv.rotation.z);
            EditorPrefs.SetFloat(k + ".rw", sv.rotation.w);
            EditorPrefs.SetFloat(k + ".size", sv.size);
        }

        private void Restore(SceneView sv, int slot)
        {
            string k = Key(slot);
            Vector3 pivot = GetVector(k + ".pivot");
            Quaternion rot = new Quaternion(
                EditorPrefs.GetFloat(k + ".rx"),
                EditorPrefs.GetFloat(k + ".ry"),
                EditorPrefs.GetFloat(k + ".rz"),
                EditorPrefs.GetFloat(k + ".rw"));
            float size = EditorPrefs.GetFloat(k + ".size", 10f);

            // Setting the properties moves the Scene camera at once and reliably, where the animated
            // LookAt sometimes did not take. LookAt then eases the last little bit for a smooth stop.
            sv.pivot = pivot;
            sv.rotation = rot;
            sv.size = size;
            sv.LookAt(pivot, rot, size);
            sv.Repaint();
        }

        private void Clear(int slot)
        {
            string k = Key(slot);
            foreach (string suffix in new[] { ".set", ".pivot.x", ".pivot.y", ".pivot.z", ".rx", ".ry", ".rz", ".rw", ".size" })
            {
                EditorPrefs.DeleteKey(k + suffix);
            }
        }

        private static void SetVector(string key, Vector3 v)
        {
            EditorPrefs.SetFloat(key + ".x", v.x);
            EditorPrefs.SetFloat(key + ".y", v.y);
            EditorPrefs.SetFloat(key + ".z", v.z);
        }

        private static Vector3 GetVector(string key)
        {
            return new Vector3(
                EditorPrefs.GetFloat(key + ".x"),
                EditorPrefs.GetFloat(key + ".y"),
                EditorPrefs.GetFloat(key + ".z"));
        }
    }
}
