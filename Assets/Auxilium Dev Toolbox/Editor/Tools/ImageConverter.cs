using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Auxilium.DevToolbox
{
    /// <summary>
    /// Converts image files between JPEG and PNG, in a batch, from the Project window. Select the
    /// textures, pick a target format, and it writes new files next to the originals.
    /// </summary>
    public class ImageConverter : EditorWindow
    {
        private enum Target
        {
            Png,
            Jpg
        }

        private Target m_Target = Target.Png;
        private int m_JpgQuality = 90;
        private bool m_DeleteOriginal;
        private Vector2 m_Scroll;
        private readonly List<string> m_Report = new List<string>();

        [MenuItem("Tools/Auxilium/Dev Toolbox/Image Converter")]
        public static void Open()
        {
            var window = GetWindow<ImageConverter>(false, "Image Converter", true);
            window.minSize = new Vector2(360f, 320f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(4f);
            ToolboxUI.Header("🖼️", "Image Converter", ToolboxUI.Media);
            EditorGUILayout.HelpBox("Select image files in the Project window, then convert them here.", MessageType.Info);

            this.m_Target = (Target) EditorGUILayout.EnumPopup("Convert To", this.m_Target);

            using (new EditorGUI.DisabledScope(this.m_Target != Target.Jpg))
            {
                this.m_JpgQuality = EditorGUILayout.IntSlider("JPEG Quality", this.m_JpgQuality, 1, 100);
            }

            this.m_DeleteOriginal = EditorGUILayout.ToggleLeft("Delete the original after converting", this.m_DeleteOriginal);

            EditorGUILayout.Space(4f);
            if (GUILayout.Button($"Convert Selected To {this.m_Target}", GUILayout.Height(26f))) this.Convert();

            EditorGUILayout.Space(6f);
            this.m_Scroll = EditorGUILayout.BeginScrollView(this.m_Scroll);
            foreach (string line in this.m_Report) EditorGUILayout.LabelField(line, EditorStyles.miniLabel);
            EditorGUILayout.EndScrollView();
        }

        private void Convert()
        {
            this.m_Report.Clear();
            int done = 0;

            foreach (Object obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path)) continue;

                string ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext != ".png" && ext != ".jpg" && ext != ".jpeg")
                {
                    this.m_Report.Add($"Skipped (not an image): {Path.GetFileName(path)}");
                    continue;
                }

                if (this.ConvertFile(path)) done++;
            }

            if (done > 0) AssetDatabase.Refresh();
            this.m_Report.Add(done == 0 ? "Nothing converted. Select some PNG or JPG files first." : $"Converted {done} file(s).");
            this.Repaint();
        }

        private bool ConvertFile(string path)
        {
            byte[] source = File.ReadAllBytes(path);

            var texture = new Texture2D(2, 2);
            bool loaded = texture.LoadImage(source);
            if (!loaded)
            {
                Object.DestroyImmediate(texture);
                this.m_Report.Add($"Could not read: {Path.GetFileName(path)}");
                return false;
            }

            byte[] output = this.m_Target == Target.Png ? texture.EncodeToPNG() : texture.EncodeToJPG(this.m_JpgQuality);
            Object.DestroyImmediate(texture);

            string newExt = this.m_Target == Target.Png ? ".png" : ".jpg";
            string newPath = Path.ChangeExtension(path, newExt);

            // Never silently overwrite a different file that already sits at the target name.
            if (File.Exists(newPath) && Path.GetFullPath(newPath) != Path.GetFullPath(path))
            {
                newPath = Path.Combine(
                    Path.GetDirectoryName(path),
                    Path.GetFileNameWithoutExtension(path) + "_converted" + newExt);
            }

            File.WriteAllBytes(newPath, output);

            if (this.m_DeleteOriginal && Path.GetFullPath(newPath) != Path.GetFullPath(path))
            {
                AssetDatabase.DeleteAsset(path);
            }

            this.m_Report.Add($"{Path.GetFileName(path)}  ->  {Path.GetFileName(newPath)}");
            return true;
        }
    }
}
