using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Auxilium.DevToolbox
{
    /// <summary>
    /// Finds and deletes the empty folders that pile up after you move assets around, keeping the
    /// Project window tidy. It shows what it will remove and asks before deleting.
    /// </summary>
    public static class CleanEmptyFolders
    {
        [MenuItem("Tools/Auxilium/Dev Toolbox/Clean Empty Folders")]
        public static void Clean()
        {
            List<string> empties = FindEmpty();

            if (empties.Count == 0)
            {
                EditorUtility.DisplayDialog("Clean Empty Folders", "No empty folders found. All tidy.", "OK");
                return;
            }

            string preview = string.Join("\n", empties.Take(15));
            if (empties.Count > 15) preview += $"\n... and {empties.Count - 15} more";

            if (!EditorUtility.DisplayDialog(
                "Clean Empty Folders",
                $"Delete {empties.Count} empty folder(s)?\n\n{preview}",
                "Delete", "Cancel"))
            {
                return;
            }

            foreach (string folder in empties) AssetDatabase.DeleteAsset(folder);
            AssetDatabase.Refresh();

            Debug.Log($"[Auxilium Dev Toolbox] Removed {empties.Count} empty folder(s).");
        }

        private static List<string> FindEmpty()
        {
            var result = new List<string>();
            string root = Application.dataPath;

            foreach (string dir in Directory.GetDirectories(root, "*", SearchOption.AllDirectories))
            {
                // A folder counts as empty when it holds no files other than its own meta data and
                // no non empty subfolders. Deepest folders are checked first by sorting on length.
                bool hasFile = Directory.GetFiles(dir).Any(f => !f.EndsWith(".meta"));
                bool hasSubDir = Directory.GetDirectories(dir).Length > 0;

                if (!hasFile && !hasSubDir)
                {
                    result.Add("Assets" + dir.Substring(root.Length).Replace('\\', '/'));
                }
            }

            // Longest paths first, so a nested empty chain clears from the inside out.
            result.Sort((a, b) => b.Length.CompareTo(a.Length));
            return result;
        }
    }
}
