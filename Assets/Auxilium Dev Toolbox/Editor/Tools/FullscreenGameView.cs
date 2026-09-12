using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR_WIN
using System.Runtime.InteropServices;
#endif

namespace Auxilium.DevToolbox
{
    /// <summary>
    /// Opens the Game view as a true fullscreen window, edge to edge, hiding the editor toolbar and,
    /// on Windows, the taskbar too, so you can feel the game the way a player will without building.
    /// The same shortcut brings you back, and you can change that shortcut in the toolbox window.
    /// </summary>
    /// <remarks>
    /// Unity has no fullscreen Game view, so this reaches the internal GameView window by reflection,
    /// shows it as a borderless popup sized to the display, and on Windows pins it above everything
    /// and hides the taskbar. Everything is undone on the way out and when the editor quits, so you
    /// never get stranded in a black fullscreen.
    /// </remarks>
    [InitializeOnLoad]
    public static class FullscreenGameView
    {
        private const string KeyPref = "Auxilium.DevToolbox.Fullscreen.Key";
        private const string CtrlPref = "Auxilium.DevToolbox.Fullscreen.Ctrl";
        private const string ShiftPref = "Auxilium.DevToolbox.Fullscreen.Shift";
        private const string AltPref = "Auxilium.DevToolbox.Fullscreen.Alt";

        private static readonly Type GameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        private static EditorWindow s_Instance;

        static FullscreenGameView()
        {
            HookGlobalKey();
            EditorApplication.playModeStateChanged += _ => { if (!Application.isPlaying) Restore(); };
            EditorApplication.quitting += Restore;
        }

        // SETTINGS: ------------------------------------------------------------------------------

        public static KeyCode ToggleKey
        {
            get => (KeyCode) EditorPrefs.GetInt(KeyPref, (int) KeyCode.F9);
            set => EditorPrefs.SetInt(KeyPref, (int) value);
        }

        public static bool Ctrl { get => EditorPrefs.GetBool(CtrlPref, false); set => EditorPrefs.SetBool(CtrlPref, value); }
        public static bool Shift { get => EditorPrefs.GetBool(ShiftPref, false); set => EditorPrefs.SetBool(ShiftPref, value); }
        public static bool Alt { get => EditorPrefs.GetBool(AltPref, false); set => EditorPrefs.SetBool(AltPref, value); }

        public static bool IsFullscreen => s_Instance != null;

        public static string ShortcutLabel()
        {
            string s = string.Empty;
            if (Ctrl) s += "Ctrl+";
            if (Shift) s += "Shift+";
            if (Alt) s += "Alt+";
            return s + ToggleKey;
        }

        // TOGGLE: --------------------------------------------------------------------------------

        [MenuItem("Tools/Auxilium/Dev Toolbox/Fullscreen Game View")]
        public static void Toggle()
        {
            if (s_Instance != null) { Restore(); return; }
            if (GameViewType == null)
            {
                Debug.LogWarning("[Auxilium Dev Toolbox] Could not find the Game view on this Unity version.");
                return;
            }

            s_Instance = (EditorWindow) ScriptableObject.CreateInstance(GameViewType);
            HideToolbar(s_Instance);

            Resolution res = Screen.currentResolution;
            s_Instance.ShowPopup();
            s_Instance.position = new Rect(0f, 0f, res.width, res.height);
            s_Instance.Focus();

#if UNITY_EDITOR_WIN
            HideTaskbar(true);
            EditorApplication.delayCall += () =>
            {
                IntPtr hwnd = GetForegroundWindow();
                if (hwnd != IntPtr.Zero) SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, res.width, res.height, SWP_SHOWWINDOW);
            };
#endif
        }

        public static void Restore()
        {
            if (s_Instance != null)
            {
                s_Instance.Close();
                s_Instance = null;
            }
#if UNITY_EDITOR_WIN
            HideTaskbar(false);
#endif
        }

        private static void HideToolbar(EditorWindow window)
        {
            PropertyInfo prop = GameViewType.GetProperty("showToolbar", BindingFlags.Instance | BindingFlags.NonPublic);
            if (prop != null) { prop.SetValue(window, false); return; }

            MethodInfo method = GameViewType.GetMethod("SetShowToolbar", BindingFlags.Instance | BindingFlags.NonPublic);
            if (method != null) method.Invoke(window, new object[] { false });
        }

        // GLOBAL KEY: ----------------------------------------------------------------------------

        private static void HookGlobalKey()
        {
            // The field's delegate type has changed across editor versions, so build a matching
            // delegate by reflection rather than assuming one, then remove any old copy and add ours.
            FieldInfo field = typeof(EditorApplication).GetField("globalEventHandler", BindingFlags.Static | BindingFlags.NonPublic);
            if (field == null) return;

            MethodInfo method = typeof(FullscreenGameView).GetMethod(nameof(OnGlobalKey), BindingFlags.Static | BindingFlags.NonPublic);
            Delegate ours = Delegate.CreateDelegate(field.FieldType, method);

            var existing = field.GetValue(null) as Delegate;
            existing = Delegate.Remove(existing, ours);
            field.SetValue(null, Delegate.Combine(existing, ours));
        }

        private static void OnGlobalKey()
        {
            Event e = Event.current;
            if (e == null || e.type != EventType.KeyDown) return;

            // A hidden taskbar is unnerving, so Escape is always an emergency way back out.
            if (s_Instance != null && e.keyCode == KeyCode.Escape) { Restore(); e.Use(); return; }

            if (e.keyCode != ToggleKey) return;
            if (e.control != Ctrl || e.shift != Shift || e.alt != Alt) return;

            Toggle();
            e.Use();
        }

        // WINDOWS: -------------------------------------------------------------------------------

#if UNITY_EDITOR_WIN
        [DllImport("user32.dll")] private static extern IntPtr FindWindow(string className, string windowName);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        private static void HideTaskbar(bool hide)
        {
            IntPtr tray = FindWindow("Shell_TrayWnd", null);
            if (tray != IntPtr.Zero) ShowWindow(tray, hide ? SW_HIDE : SW_SHOW);
        }
#endif
    }
}
