using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Auxilium.DevToolbox
{
    /// <summary>
    /// Saves the open scenes every few minutes while you work, so a crash or a mistaken close never
    /// costs more than a couple of minutes. It runs quietly in the background once switched on.
    /// </summary>
    [InitializeOnLoad]
    public static class AutoSave
    {
        private const string PrefEnabled = "Auxilium.DevToolbox.AutoSave.Enabled";
        private const string PrefMinutes = "Auxilium.DevToolbox.AutoSave.Minutes";
        private const string PrefLog = "Auxilium.DevToolbox.AutoSave.Log";

        private static double s_NextSave;

        static AutoSave()
        {
            EditorApplication.update += Tick;
            s_NextSave = EditorApplication.timeSinceStartup + IntervalSeconds();
        }

        public static bool Enabled
        {
            get => EditorPrefs.GetBool(PrefEnabled, false);
            set { EditorPrefs.SetBool(PrefEnabled, value); Reschedule(); }
        }

        public static int Minutes
        {
            get => Mathf.Clamp(EditorPrefs.GetInt(PrefMinutes, 5), 1, 120);
            set { EditorPrefs.SetInt(PrefMinutes, Mathf.Clamp(value, 1, 120)); Reschedule(); }
        }

        public static bool LogToConsole
        {
            get => EditorPrefs.GetBool(PrefLog, true);
            set => EditorPrefs.SetBool(PrefLog, value);
        }

        private static double IntervalSeconds() => Minutes * 60.0;

        private static void Reschedule() => s_NextSave = EditorApplication.timeSinceStartup + IntervalSeconds();

        private static void Tick()
        {
            if (!Enabled) return;

            // Never save while playing or compiling, both of which make a save pointless or unsafe.
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;

            if (EditorApplication.timeSinceStartup < s_NextSave) return;
            s_NextSave = EditorApplication.timeSinceStartup + IntervalSeconds();

            if (!EditorSceneManager.SaveOpenScenes()) return;
            AssetDatabase.SaveAssets();

            if (LogToConsole) Debug.Log($"[Auxilium Auto-Save] Saved at {DateTime.Now:HH:mm:ss}.");
        }
    }
}
