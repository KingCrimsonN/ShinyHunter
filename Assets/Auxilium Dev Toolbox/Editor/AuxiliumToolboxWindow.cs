using System;
using UnityEditor;
using UnityEngine;

namespace Auxilium.DevToolbox
{
    /// <summary>
    /// The hub for the Auxilium Dev Toolbox. It lists every tool in tidy colour coded groups, opens
    /// them, and holds the settings for the ones that run in the background. Open it from Tools,
    /// Auxilium, Dev Toolbox, or from the Window menu.
    /// </summary>
    public class AuxiliumToolboxWindow : EditorWindow
    {
        private const string DiscordUrl = "https://discord.gg/an4yMHNdes";
        private const string PublisherUrl = "https://assetstore.unity.com/publishers/121252";

        private static readonly Color CleanupColor = new Color(0.85f, 0.42f, 0.30f);
        private static readonly Color SceneColor = new Color(0.30f, 0.60f, 0.85f);
        private static readonly Color ObjectColor = new Color(0.90f, 0.62f, 0.24f);
        private static readonly Color MediaColor = new Color(0.62f, 0.45f, 0.85f);
        private static readonly Color DataColor = new Color(0.28f, 0.72f, 0.55f);

        private Texture2D m_Logo;
        private Vector2 m_Scroll;
        private GUIStyle m_Card;
        private GUIStyle m_Title;
        private GUIStyle m_Desc;

        [MenuItem("Tools/Auxilium/Dev Toolbox/Open Toolbox", priority = 0)]
        [MenuItem("Window/Auxilium/Dev Toolbox")]
        public static void Open()
        {
            var window = GetWindow<AuxiliumToolboxWindow>(false, "Dev Toolbox", true);
            window.minSize = new Vector2(400f, 500f);
            window.Show();
        }

        private void OnEnable()
        {
            this.m_Logo = Resources.Load<Texture2D>("Auxlogo");
        }

        private void BuildStyles()
        {
            if (this.m_Card != null) return;

            this.m_Card = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(12, 10, 8, 8),
                margin = new RectOffset(2, 2, 2, 4)
            };

            this.m_Title = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };

            this.m_Desc = new GUIStyle(EditorStyles.label) { wordWrap = true, fontSize = 11 };
            this.m_Desc.normal.textColor = new Color(0.66f, 0.66f, 0.68f);
        }

        private void OnGUI()
        {
            this.BuildStyles();
            this.m_Scroll = EditorGUILayout.BeginScrollView(this.m_Scroll);

            this.DrawHeader();

            if (this.Section("🧹  Housekeeping", CleanupColor))
            {
                this.Card("🧹", "Missing Script Remover", "Clear the broken Missing Script lines left when a script or package is deleted.", CleanupColor, "Open", MissingScriptRemover.Open);
                this.Card("🧽", "Clean Empty Folders", "Sweep out the empty folders that pile up as you move assets around.", CleanupColor, "Clean", CleanEmptyFolders.Clean);
            }

            if (this.Section("🎬  Scene and Play", SceneColor))
            {
                this.Card("🕹️", "Scene Quick Switch", "Open any scene in the project with one click, no hunting in the Project window.", SceneColor, "Open", SceneQuickSwitch.Open);
                this.DrawFullscreen();
                this.Card("⏱️", "Time Scale", "Slow down or speed up play mode with a slider, to study or skip a moment.", SceneColor, "Open", TimeScaleControl.Open);
                this.Card("🔖", "Camera Bookmarks", "Save Scene view camera spots and jump back to them.", SceneColor, "Open", CameraBookmarks.Open);
            }

            if (this.Section("🧰  Objects and Level", ObjectColor))
            {
                this.Card("✏️", "Batch Rename", "Rename all selected objects or assets with numbering or find and replace.", ObjectColor, "Open", BatchRename.Open);
                this.Card("📐", "Align Objects", "Line the selection up on an axis, or spread it out evenly.", ObjectColor, "Open", AlignObjects.Open);
                this.Card("🎲", "Randomize Transform", "Scatter props with random rotation, scale and a little position jitter.", ObjectColor, "Open", RandomizeTransform.Open);
                this.Card("🔁", "Replace With Prefab", "Swap the selected objects for a prefab, keeping each one's transform.", ObjectColor, "Open", ReplaceWithPrefab.Open);
                this.Card("📍", "Snap To Ground", "Drop the selection onto the collider below it. Shortcut F8.", ObjectColor, "Snap", SnapToGround.Snap);
                this.Card("📦", "Group Selected", "Parent the selection under a fresh empty at its centre. Ctrl+Shift+G.", ObjectColor, "Group", QuickTransformTools.GroupSelected);
                this.Card("♻️", "Reset Transform", "Zero position and rotation and set scale to one on the selection.", ObjectColor, "Reset", QuickTransformTools.ResetTransform);
                this.Card("👁️", "Toggle Active", "Show or hide the selection. Shortcut Ctrl+Shift+A.", ObjectColor, "Toggle", ToggleActive.Toggle);
                this.Info("📋", "Copy and Paste Transform", "Copy one object's world transform onto others. Tools, Auxilium, Dev Toolbox, Transform.", ObjectColor);
            }

            if (this.Section("📸  Media", MediaColor))
            {
                this.Card("📸", "Screenshot", "Capture the game view at high resolution, with hotkeys, formats and a watermark.", MediaColor, "Open", () => EditorApplication.ExecuteMenuItem("Tools/Auxilium/Dev Toolbox/Screenshot/Capture Window"));
                this.Card("🖼️", "Image Converter", "Batch convert selected image files between JPEG and PNG.", MediaColor, "Open", ImageConverter.Open);
            }

            if (this.Section("💾  Data and Notes", DataColor))
            {
                this.Card("🔧", "PlayerPrefs Editor", "List, edit, add and delete PlayerPrefs without writing throwaway code.", DataColor, "Open", PlayerPrefsEditor.Open);
                this.Card("📝", "Project Notes", "A scratch pad that travels with the project. Todos, reminders, handover notes.", DataColor, "Open", ProjectNotes.Open);
                this.Info("↩️", "Selection History", "Step back and forward through what you have selected. Alt+Left and Alt+Right.", DataColor);
                this.DrawAutoSave();
            }

            EditorGUILayout.Space(10f);
            this.DrawFooter();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            Rect band = GUILayoutUtility.GetRect(0f, 120f, GUILayout.ExpandWidth(true));

            float logoBottom = band.y + 16f;
            if (this.m_Logo != null)
            {
                float w = Mathf.Min(150f, band.width - 120f);
                float h = w * this.m_Logo.height / this.m_Logo.width;
                var r = new Rect(band.x + (band.width - w) * 0.5f, band.y + 16f, w, h);
                GUI.DrawTexture(r, this.m_Logo, ScaleMode.ScaleToFit);
                logoBottom = r.yMax;
            }

            var title = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 16 };
            GUI.Label(new Rect(band.x, logoBottom + 8f, band.width, 20f), "Dev Toolbox", title);

            var sub = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(band.x, logoBottom + 28f, band.width, 14f), "A pocketful of everyday tools. Made by Auxilium.", sub);

            EditorGUILayout.Space(16f);
        }

        private bool Section(string label, Color color)
        {
            string key = "Auxilium.DevToolbox.Fold." + label;
            bool expanded = EditorPrefs.GetBool(key, true);

            EditorGUILayout.Space(6f);
            Rect r = GUILayoutUtility.GetRect(0f, 22f, GUILayout.ExpandWidth(true));

            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(new Rect(r.x, r.y, 4f, r.height), color);
                var tint = color; tint.a = 0.14f;
                EditorGUI.DrawRect(new Rect(r.x + 4f, r.y, r.width - 4f, r.height), tint);
            }

            var style = new GUIStyle(EditorStyles.boldLabel);
            style.normal.textColor = color * 1.1f;
            string arrow = expanded ? "▼" : "▶";
            GUI.Label(new Rect(r.x + 12f, r.y, r.width - 12f, r.height), $"{arrow}  {label}", style);

            // Whole header row toggles the fold, so there is a big easy target rather than a tiny arrow.
            if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
            {
                EditorPrefs.SetBool(key, !expanded);
                Event.current.Use();
                this.Repaint();
            }

            EditorGUIUtility.AddCursorRect(r, MouseCursor.Link);
            return expanded;
        }

        private void Card(string emoji, string title, string desc, Color accent, string button, Action run)
        {
            Rect rect = EditorGUILayout.BeginVertical(this.m_Card);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(emoji, GUILayout.Width(22f));
            GUILayout.Label(title, this.m_Title);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(button, GUILayout.Width(66f), GUILayout.Height(20f))) run();
            EditorGUILayout.EndHorizontal();

            GUILayout.Label(desc, this.m_Desc);

            EditorGUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3f, rect.height), accent);
            }
        }

        private void Info(string emoji, string title, string desc, Color accent)
        {
            Rect rect = EditorGUILayout.BeginVertical(this.m_Card);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(emoji, GUILayout.Width(22f));
            GUILayout.Label(title, this.m_Title);
            EditorGUILayout.EndHorizontal();

            GUILayout.Label(desc, this.m_Desc);

            EditorGUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3f, rect.height), accent);
            }
        }

        private void DrawFullscreen()
        {
            Rect rect = EditorGUILayout.BeginVertical(this.m_Card);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("🖥️", GUILayout.Width(22f));
            GUILayout.Label("Fullscreen Game View", this.m_Title);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(FullscreenGameView.IsFullscreen ? "Exit" : "Enter", GUILayout.Width(66f), GUILayout.Height(20f))) FullscreenGameView.Toggle();
            EditorGUILayout.EndHorizontal();

            GUILayout.Label("Play edge to edge with the toolbar and taskbar hidden. Press the shortcut or Escape to come back.", this.m_Desc);

            EditorGUILayout.Space(2f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Shortcut", GUILayout.Width(58f));
            FullscreenGameView.Ctrl = GUILayout.Toggle(FullscreenGameView.Ctrl, "Ctrl", EditorStyles.miniButton, GUILayout.Width(44f));
            FullscreenGameView.Shift = GUILayout.Toggle(FullscreenGameView.Shift, "Shift", EditorStyles.miniButton, GUILayout.Width(46f));
            FullscreenGameView.Alt = GUILayout.Toggle(FullscreenGameView.Alt, "Alt", EditorStyles.miniButton, GUILayout.Width(40f));
            FullscreenGameView.ToggleKey = (KeyCode) EditorGUILayout.EnumPopup(FullscreenGameView.ToggleKey);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3f, rect.height), SceneColor);
            }
        }

        private void DrawAutoSave()
        {
            Rect rect = EditorGUILayout.BeginVertical(this.m_Card);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("💾", GUILayout.Width(22f));
            GUILayout.Label("Auto-Save", this.m_Title);
            EditorGUILayout.EndHorizontal();

            GUILayout.Label("Saves your open scenes every few minutes, so a crash never costs much.", this.m_Desc);

            EditorGUILayout.Space(2f);
            AutoSave.Enabled = EditorGUILayout.ToggleLeft("Save automatically", AutoSave.Enabled);
            using (new EditorGUI.DisabledScope(!AutoSave.Enabled))
            {
                AutoSave.Minutes = EditorGUILayout.IntSlider("Every (minutes)", AutoSave.Minutes, 1, 60);
                AutoSave.LogToConsole = EditorGUILayout.ToggleLeft("Log each save to the Console", AutoSave.LogToConsole);
            }

            EditorGUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3f, rect.height), DataColor);
            }
        }

        private void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("🌟  More From Auxilium", GUILayout.Height(26f), GUILayout.Width(180f))) Application.OpenURL(PublisherUrl);
            GUILayout.Space(6f);
            if (GUILayout.Button("💬  Discord", GUILayout.Height(26f), GUILayout.Width(96f))) Application.OpenURL(DiscordUrl);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            var foot = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
            GUILayout.Label("Thank you for choosing Auxilium.", foot);
            GUILayout.Label("Don't forget to leave us a review !", foot);
            GUILayout.Space(6f);
        }
    }
}
