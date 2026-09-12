using UnityEditor;
using UnityEngine;

namespace Auxilium.DevToolbox
{
    /// <summary>
    /// A slider for Time.timeScale, so you can slow play mode down to study a moment or speed it up
    /// to skip ahead, without touching code. It only takes effect in play mode and resets to normal
    /// when you stop.
    /// </summary>
    public class TimeScaleControl : EditorWindow
    {
        private static readonly float[] Presets = { 0f, 0.25f, 0.5f, 1f, 2f, 4f };

        [MenuItem("Tools/Auxilium/Dev Toolbox/Time Scale")]
        public static void Open()
        {
            var window = GetWindow<TimeScaleControl>(false, "Time Scale", true);
            window.minSize = new Vector2(320f, 130f);
            window.maxSize = new Vector2(600f, 150f);
            window.Show();
        }

        private void OnEnable() => EditorApplication.playModeStateChanged += this.OnPlayModeChanged;
        private void OnDisable() => EditorApplication.playModeStateChanged -= this.OnPlayModeChanged;

        private void OnPlayModeChanged(PlayModeStateChange change)
        {
            // Leave the project in a normal state when play stops, so a slow scale never lingers.
            if (change == PlayModeStateChange.EnteredEditMode) Time.timeScale = 1f;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6f);
            ToolboxUI.Header("⏱️", "Time Scale", ToolboxUI.Scene);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Time Scale only changes anything in play mode.", MessageType.Info);
            }

            float value = EditorGUILayout.Slider("Scale", Time.timeScale, 0f, 8f);
            Time.timeScale = value;

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal();
            foreach (float preset in Presets)
            {
                if (GUILayout.Button(preset == 0f ? "Pause" : preset + "x", GUILayout.Height(22f)))
                {
                    Time.timeScale = preset;
                }
            }
            EditorGUILayout.EndHorizontal();

            this.Repaint();
        }
    }
}
