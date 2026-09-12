using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Auxilium.DevToolbox
{
    /// <summary>
    /// Remembers what you have selected and lets you step back and forward through it, the way a
    /// browser remembers pages. Handy when you jump between a handful of objects and keep losing
    /// your place. Back is Alt+Left, forward is Alt+Right.
    /// </summary>
    [InitializeOnLoad]
    public static class SelectionHistory
    {
        private const int MaxEntries = 50;

        private static readonly List<Object> s_History = new List<Object>();
        private static int s_Index = -1;
        private static bool s_Suppress;

        static SelectionHistory()
        {
            Selection.selectionChanged += OnSelectionChanged;
        }

        private static void OnSelectionChanged()
        {
            // Ignore the change we caused ourselves when stepping through the history.
            if (s_Suppress) return;

            Object active = Selection.activeObject;
            if (active == null) return;
            if (s_Index >= 0 && s_Index < s_History.Count && s_History[s_Index] == active) return;

            // Drop anything ahead of the current point, the way a new page clears forward history.
            if (s_Index < s_History.Count - 1)
            {
                s_History.RemoveRange(s_Index + 1, s_History.Count - s_Index - 1);
            }

            s_History.Add(active);
            if (s_History.Count > MaxEntries) s_History.RemoveAt(0);

            s_Index = s_History.Count - 1;
        }

        [MenuItem("Tools/Auxilium/Dev Toolbox/Selection Back &LEFT")]
        private static void Back()
        {
            if (s_Index <= 0) return;
            s_Index--;
            Apply();
        }

        [MenuItem("Tools/Auxilium/Dev Toolbox/Selection Forward &RIGHT")]
        private static void Forward()
        {
            if (s_Index >= s_History.Count - 1) return;
            s_Index++;
            Apply();
        }

        private static void Apply()
        {
            // Skip over anything that has since been deleted.
            while (s_Index >= 0 && s_History[s_Index] == null) s_Index--;
            if (s_Index < 0) return;

            s_Suppress = true;
            Selection.activeObject = s_History[s_Index];
            EditorGUIUtility.PingObject(s_History[s_Index]);
            s_Suppress = false;
        }
    }
}
