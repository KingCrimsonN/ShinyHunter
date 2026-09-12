#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Auxilium.ScreenshotPro
{
    public static class ScreenshotProShortcuts
    {
        [MenuItem("Tools/Auxilium/Dev Toolbox/Screenshot/Capture (Hotkey)/Capture F10 _F10", priority = 200)]
        private static void CaptureF10() => TryCapture(KeyCode.F10);

        [MenuItem("Tools/Auxilium/Dev Toolbox/Screenshot/Capture (Hotkey)/Capture F11 _F11", priority = 201)]
        private static void CaptureF11() => TryCapture(KeyCode.F11);

        [MenuItem("Tools/Auxilium/Dev Toolbox/Screenshot/Capture (Hotkey)/Capture F12 _F12", priority = 202)]
        private static void CaptureF12() => TryCapture(KeyCode.F12);

        [MenuItem("Tools/Auxilium/Dev Toolbox/Screenshot/Capture (Hotkey)/Capture F10 _F10", true)]
        private static bool ValidateF10() => Validate(KeyCode.F10);

        [MenuItem("Tools/Auxilium/Dev Toolbox/Screenshot/Capture (Hotkey)/Capture F11 _F11", true)]
        private static bool ValidateF11() => Validate(KeyCode.F11);

        [MenuItem("Tools/Auxilium/Dev Toolbox/Screenshot/Capture (Hotkey)/Capture F12 _F12", true)]
        private static bool ValidateF12() => Validate(KeyCode.F12);

        private static bool Validate(KeyCode key)
        {
            if (!Application.isPlaying) return false;
            var chosen = (KeyCode)EditorPrefs.GetInt(ScreenshotEditorWindow.PREF_HOTKEY, (int)KeyCode.None);
            return chosen == key;
        }

        private static void TryCapture(KeyCode key)
        {
            if (!Application.isPlaying) return;

            var chosen = (KeyCode)EditorPrefs.GetInt(ScreenshotEditorWindow.PREF_HOTKEY, (int)KeyCode.None);
            if (chosen == key)
            {
                ScreenshotProSettings.CaptureFromSavedSettings();
            }
        }
    }
}
#endif