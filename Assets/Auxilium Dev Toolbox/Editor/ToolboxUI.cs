using UnityEditor;
using UnityEngine;

namespace Auxilium.DevToolbox
{
    /// <summary>
    /// Shared look for every tool window in the toolbox, so they all wear the same coloured header
    /// and read as one product rather than a pile of separate windows.
    /// </summary>
    public static class ToolboxUI
    {
        public static readonly Color Cleanup = new Color(0.85f, 0.42f, 0.30f);
        public static readonly Color Scene = new Color(0.30f, 0.60f, 0.85f);
        public static readonly Color Object = new Color(0.90f, 0.62f, 0.24f);
        public static readonly Color Media = new Color(0.62f, 0.45f, 0.85f);
        public static readonly Color Data = new Color(0.28f, 0.72f, 0.55f);

        /// <summary>
        /// Draws the coloured title bar a tool window opens with. Call it first in OnGUI.
        /// </summary>
        public static void Header(string emoji, string title, Color accent)
        {
            Rect r = GUILayoutUtility.GetRect(0f, 30f, GUILayout.ExpandWidth(true));

            if (Event.current.type == EventType.Repaint)
            {
                Color tint = accent; tint.a = 0.16f;
                EditorGUI.DrawRect(r, tint);
                EditorGUI.DrawRect(new Rect(r.x, r.y, 4f, r.height), accent);
                EditorGUI.DrawRect(new Rect(r.x, r.yMax - 1f, r.width, 1f), new Color(0f, 0f, 0f, 0.2f));
            }

            var style = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12, alignment = TextAnchor.MiddleLeft };
            style.normal.textColor = accent * 1.15f;
            GUI.Label(new Rect(r.x + 12f, r.y, r.width - 12f, r.height), $"{emoji}  {title}", style);

            GUILayout.Space(4f);
        }

        /// <summary>
        /// A small footer that reminds a happy user to leave a review, the cheapest marketing there
        /// is. Optional, drop it at the bottom of a tool window.
        /// </summary>
        public static void ReviewFooter()
        {
            GUILayout.FlexibleSpace();
            var foot = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
            GUILayout.Label("Made by Auxilium. A review keeps these coming.", foot);
            GUILayout.Space(4f);
        }
    }
}
