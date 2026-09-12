#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace Auxilium.ScreenshotPro
{
    public class ScreenshotEditorWindow : EditorWindow
    {
        // âââ Pref Keys âââââââââââââââââââââââââââââââââââââââââââââââââââââââ
        public const string PREF_PRESET          = "ScreenshotPro_Preset";
        public const string PREF_HIDE_UI         = "ScreenshotPro_HideUI";
        public const string PREF_MODE            = "ScreenshotPro_Mode";
        public const string PREF_FOLDER          = "ScreenshotPro_Folder";
        public const string PREF_HOTKEY          = "ScreenshotPro_Hotkey";
        public const string PREF_WATERMARK_CORNER= "ScreenshotPro_WatermarkCorner";
        public const string PREF_FORMAT          = "ScreenshotPro_Format";
        public const string PREF_JPEG_QUALITY    = "ScreenshotPro_JpegQuality";
        public const string PREF_FILE_PREFIX     = "ScreenshotPro_FilePrefix";
        public const string PREF_OPEN_AFTER      = "ScreenshotPro_OpenAfter";
        public const string PREF_CUSTOM_RES      = "ScreenshotPro_CustomRes";
        public const string PREF_CUSTOM_W        = "ScreenshotPro_CustomW";
        public const string PREF_CUSTOM_H        = "ScreenshotPro_CustomH";
        public const string PREF_TIMER_DELAY     = "ScreenshotPro_TimerDelay";
        public const string PREF_BURST_COUNT     = "ScreenshotPro_BurstCount";
        public const string PREF_POST_PROCESSING = "ScreenshotPro_PostProcessing";

        // âââ Constants âââââââââââââââââââââââââââââââââââââââââââââââââââââââ
        private const string TOOL_NAME               = "Auxilium Screenshot";
        private const string VERSION                 = "v1.1.0";
        private const string PANEL_LOGO_RESOURCE_NAME= "ScreenshotPro_Logo";

        // âââ Enums âââââââââââââââââââââââââââââââââââââââââââââââââââââââââââ
        public enum ResPreset    { Full_HD_1080p, QHD_2K, UHD_4K, UHD_8K }
        public enum CaptureHotkey{ None, F10, F11, F12 }

        // âââ Core Fields âââââââââââââââââââââââââââââââââââââââââââââââââââââ
        private ResPreset                      selectedRes     = ResPreset.UHD_4K;
        private ScreenshotManager.LogoPlacement watermarkCorner = ScreenshotManager.LogoPlacement.BottomRight;
        private bool                           autoHideUI      = false;
        private string                         folderName      = "Captures";
        private Texture2D                      watermarkLogo;
        private ScreenshotManager.CaptureMode  captureMode     = ScreenshotManager.CaptureMode.Safe_UpscaleFromGameView;

        // âââ New Fields âââââââââââââââââââââââââââââââââââââââââââââââââââââââ
        private ScreenshotManager.ImageFormat  imageFormat     = ScreenshotManager.ImageFormat.PNG;
        private int                            jpegQuality     = 90;
        private string                         filePrefix      = "Screenshot";
        private bool                           openAfterCapture= false;
        private bool                           useCustomRes    = false;
        private int                            customWidth     = 1920;
        private int                            customHeight    = 1080;
        private int                            timerDelay      = 0;
        private int                            burstCount      = 1;
        private bool                           usePostProcessing = false;

        // âââ Sound / Hotkey Fields ââââââââââââââââââââââââââââââââââââââââââââ
        private bool         playSoundOnCapture   = false;
        private AudioClip    captureSound;
        private CaptureHotkey hotkey             = CaptureHotkey.None;
        private bool         hotkeyOnlyInPlayMode= true;
        private bool         showPanelLogo        = true;

        // âââ UI State âââââââââââââââââââââââââââââââââââââââââââââââââââââââââ
        private Texture2D panelLogo;
        private Vector2   scroll;
        private bool      didAutoCleanPresets;

        private bool _captureFold    = true;
        private bool _soundFold      = false;
        private bool _watermarkFold  = false;
        private bool _maintenanceFold= false;

        // âââ Preview ââââââââââââââââââââââââââââââââââââââââââââââââââââââââââ
        private Texture2D _lastPreview;
        private string    _lastCapturePath = "";
        private bool      _previewFold     = false;

        // âââ Countdown ââââââââââââââââââââââââââââââââââââââââââââââââââââââââ
        private double _captureAtTime  = -1;
        private int    _countdownValue = 0;

        // âââ Styles (lazily built) ââââââââââââââââââââââââââââââââââââââââââââ
        private GUIStyle _captureButtonStyle;
        private GUIStyle _versionLabelStyle;
        private GUIStyle _statusStyle;

        // âââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââ
        [MenuItem("Tools/Auxilium/Dev Toolbox/Screenshot/Capture Window")]
        public static void ShowWindow()
        {
            var w = GetWindow<ScreenshotEditorWindow>(false, TOOL_NAME, true);
            w.minSize = new Vector2(380, 580);
            w.Show();
        }

        // âââ Unity Callbacks ââââââââââââââââââââââââââââââââââââââââââââââââââ
        private void OnEnable()
        {
            if (panelLogo == null)
                panelLogo = Resources.Load<Texture2D>(PANEL_LOGO_RESOURCE_NAME);

            LoadPrefs();

            if (!didAutoCleanPresets)
            {
                didAutoCleanPresets = true;
                try
                {
                    int removed = GameViewPresetCleanup.RemoveScreenshotProPresets();
                    if (removed > 0) Debug.Log($"[{TOOL_NAME}] Cleaned {removed} legacy GameView presets.");
                }
                catch { }
            }

            ScreenshotManager.OnScreenshotSaved += OnScreenshotSaved;
        }

        private void OnDisable()
        {
            ScreenshotManager.OnScreenshotSaved -= OnScreenshotSaved;
            CancelCountdown();
        }

        private void OnScreenshotSaved(string path)
        {
            _lastCapturePath = path;
            _lastPreview     = null;

            try
            {
                byte[] data = File.ReadAllBytes(path);
                _lastPreview = new Texture2D(2, 2);
                _lastPreview.LoadImage(data);
            }
            catch { }

            _previewFold = true;
            Repaint();
        }

        private void OnGUI()
        {
            EnsureStyles();
            HandleHotkey(Event.current);

            scroll = EditorGUILayout.BeginScrollView(scroll);

            DrawHeader();
            DrawIntegrations();
            EditorGUILayout.Space(4);

            DrawCaptureSection();
            EditorGUILayout.Space(2);
            DrawSoundHotkeySection();
            EditorGUILayout.Space(2);
            DrawWatermarkSection();
            EditorGUILayout.Space(2);
            DrawMaintenanceSection();
            EditorGUILayout.Space(6);

            DrawActions();
            EditorGUILayout.Space(4);
            DrawPreviewSection();
            EditorGUILayout.Space(8);

            EditorGUILayout.EndScrollView();
            SavePrefs();
        }

        // âââ Style Builder ââââââââââââââââââââââââââââââââââââââââââââââââââââ
        private void EnsureStyles()
        {
            if (_captureButtonStyle == null)
            {
                _captureButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize    = 14,
                    fontStyle   = FontStyle.Bold,
                    fixedHeight = 46,
                    alignment   = TextAnchor.MiddleCenter
                };
            }
            if (_versionLabelStyle == null)
            {
                _versionLabelStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                {
                    fontSize = 10
                };
            }
            if (_statusStyle == null)
            {
                _statusStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    wordWrap  = true,
                    alignment = TextAnchor.MiddleLeft
                };
            }
        }

        // âââ Prefs ââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââ
        private void LoadPrefs()
        {
            selectedRes      = (ResPreset)EditorPrefs.GetInt(PREF_PRESET, 2);
            autoHideUI       = EditorPrefs.GetBool(PREF_HIDE_UI, false);
            captureMode      = (ScreenshotManager.CaptureMode)EditorPrefs.GetInt(PREF_MODE, 0);
            folderName       = EditorPrefs.GetString(PREF_FOLDER, "Captures");
            watermarkCorner  = (ScreenshotManager.LogoPlacement)EditorPrefs.GetInt(PREF_WATERMARK_CORNER, 3);
            imageFormat      = (ScreenshotManager.ImageFormat)EditorPrefs.GetInt(PREF_FORMAT, 0);
            jpegQuality      = EditorPrefs.GetInt(PREF_JPEG_QUALITY, 90);
            filePrefix       = EditorPrefs.GetString(PREF_FILE_PREFIX, "Screenshot");
            openAfterCapture = EditorPrefs.GetBool(PREF_OPEN_AFTER, false);
            useCustomRes     = EditorPrefs.GetBool(PREF_CUSTOM_RES, false);
            customWidth      = EditorPrefs.GetInt(PREF_CUSTOM_W, 1920);
            customHeight     = EditorPrefs.GetInt(PREF_CUSTOM_H, 1080);
            timerDelay       = EditorPrefs.GetInt(PREF_TIMER_DELAY, 0);
            burstCount       = EditorPrefs.GetInt(PREF_BURST_COUNT, 1);
            usePostProcessing= EditorPrefs.GetBool(PREF_POST_PROCESSING, false);

            int hkInt = EditorPrefs.GetInt(PREF_HOTKEY, (int)KeyCode.None);
            hotkey = (KeyCode)hkInt switch
            {
                KeyCode.F10 => CaptureHotkey.F10,
                KeyCode.F11 => CaptureHotkey.F11,
                KeyCode.F12 => CaptureHotkey.F12,
                _           => CaptureHotkey.None
            };
        }

        private void SavePrefs()
        {
            EditorPrefs.SetInt(PREF_PRESET, (int)selectedRes);
            EditorPrefs.SetBool(PREF_HIDE_UI, autoHideUI);
            EditorPrefs.SetInt(PREF_MODE, (int)captureMode);
            EditorPrefs.SetString(PREF_FOLDER, folderName);
            EditorPrefs.SetInt(PREF_WATERMARK_CORNER, (int)watermarkCorner);
            EditorPrefs.SetInt(PREF_FORMAT, (int)imageFormat);
            EditorPrefs.SetInt(PREF_JPEG_QUALITY, jpegQuality);
            EditorPrefs.SetString(PREF_FILE_PREFIX, filePrefix);
            EditorPrefs.SetBool(PREF_OPEN_AFTER, openAfterCapture);
            EditorPrefs.SetBool(PREF_CUSTOM_RES, useCustomRes);
            EditorPrefs.SetInt(PREF_CUSTOM_W, customWidth);
            EditorPrefs.SetInt(PREF_CUSTOM_H, customHeight);
            EditorPrefs.SetInt(PREF_TIMER_DELAY, timerDelay);
            EditorPrefs.SetInt(PREF_BURST_COUNT, burstCount);
            EditorPrefs.SetBool(PREF_POST_PROCESSING, usePostProcessing);

            KeyCode hk = hotkey switch
            {
                CaptureHotkey.F10 => KeyCode.F10,
                CaptureHotkey.F11 => KeyCode.F11,
                CaptureHotkey.F12 => KeyCode.F12,
                _                 => KeyCode.None
            };
            EditorPrefs.SetInt(PREF_HOTKEY, (int)hk);
        }

        // âââ Hotkey Handler âââââââââââââââââââââââââââââââââââââââââââââââââââ
        private void HandleHotkey(Event e)
        {
            if (hotkey == CaptureHotkey.None) return;
            if (hotkeyOnlyInPlayMode && !Application.isPlaying) return;
            if (e.type != EventType.KeyDown) return;

            bool match = (hotkey == CaptureHotkey.F10 && e.keyCode == KeyCode.F10)
                      || (hotkey == CaptureHotkey.F11 && e.keyCode == KeyCode.F11)
                      || (hotkey == CaptureHotkey.F12 && e.keyCode == KeyCode.F12);

            if (match) { e.Use(); StartCapture(); }
        }

        // âââ Drawing: Header ââââââââââââââââââââââââââââââââââââââââââââââââââ
        private void DrawHeader()
        {
            Color accent = new Color(0.62f, 0.45f, 0.85f);
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
            GUI.Label(new Rect(r.x + 12f, r.y, r.width - 12f, r.height), "📸  Auxilium Screenshot", style);

            GUILayout.Space(4f);
        }

        // âââ Drawing: GPUI Integration Banner âââââââââââââââââââââââââââââââââ
        private void DrawIntegrations()
        {
#if GPUI_PRO
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.HelpBox(
                "GPU Instancer Pro detected. Detail Managers may need to be disabled for high-res captures.",
                MessageType.Warning);
            if (GUILayout.Button(Icon("d_console.warnicon.sml", "Disable GPUI Managers")))
            {
                foreach (var m in GameObject.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                {
                    if (m == null) continue;
                    string n = m.GetType().Name;
                    if (n.Contains("GPUIDetailManager") || n.Contains("GPUITerrainManager"))
                        m.enabled = false;
                }
            }
            EditorGUILayout.EndVertical();
#endif
        }

        // âââ Drawing: Capture Settings Section âââââââââââââââââââââââââââââââ
        private void DrawCaptureSection()
        {
            _captureFold = EditorGUILayout.BeginFoldoutHeaderGroup(
                _captureFold,
                Icon("d_SceneViewCamera", "  Capture Settings"));

            if (_captureFold)
            {
                EditorGUILayout.BeginVertical("box");

                // Resolution
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(Icon("d_BuildSettings.Editor.Small", "Quality"), GUILayout.Width(120));
                using (new EditorGUI.DisabledScope(useCustomRes))
                    selectedRes = (ResPreset)EditorGUILayout.EnumPopup(selectedRes);
                EditorGUILayout.EndHorizontal();

                // Custom resolution toggle
                useCustomRes = EditorGUILayout.Toggle("  Custom Resolution", useCustomRes);
                if (useCustomRes)
                {
                    EditorGUI.indentLevel++;
                    customWidth  = EditorGUILayout.IntField("Width",  Mathf.Max(1, customWidth));
                    customHeight = EditorGUILayout.IntField("Height", Mathf.Max(1, customHeight));
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space(4);
                Separator();
                EditorGUILayout.Space(4);

                // Format
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(Icon("d_Texture2D Icon", "Format"), GUILayout.Width(120));
                imageFormat = (ScreenshotManager.ImageFormat)EditorGUILayout.EnumPopup(imageFormat);
                EditorGUILayout.EndHorizontal();

                if (imageFormat == ScreenshotManager.ImageFormat.JPEG)
                {
                    EditorGUI.indentLevel++;
                    jpegQuality = EditorGUILayout.IntSlider("JPEG Quality", jpegQuality, 1, 100);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space(4);
                Separator();
                EditorGUILayout.Space(4);

                // Capture mode
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(Icon("d_preAudioLoopOff", "Capture Mode"), GUILayout.Width(120));
                captureMode = (ScreenshotManager.CaptureMode)EditorGUILayout.EnumPopup(captureMode);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(4);
                Separator();
                EditorGUILayout.Space(4);

                // File prefix
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(Icon("d_TextAsset Icon", "File Prefix"), GUILayout.Width(120));
                filePrefix = EditorGUILayout.TextField(filePrefix);
                EditorGUILayout.EndHorizontal();

                // Output folder
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(Icon("d_Folder Icon", "Folder"), GUILayout.Width(120));
                folderName = EditorGUILayout.TextField(folderName);
                if (GUILayout.Button("Browse", GUILayout.Width(56)))
                {
                    string chosen = EditorUtility.OpenFolderPanel("Choose Output Folder", folderName, "");
                    if (!string.IsNullOrEmpty(chosen)) folderName = chosen;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(4);
                Separator();
                EditorGUILayout.Space(4);

                // Hide UI
                autoHideUI = EditorGUILayout.Toggle(
                    Icon("d_scenevis_hidden_hover", "  Hide UI on Capture"), autoHideUI);

                // Post-processing
                usePostProcessing = EditorGUILayout.Toggle(
                    Icon("d_Camera Icon", "  Include Post-Processing"), usePostProcessing);
                if (usePostProcessing)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.HelpBox(
                        "Renders via the main camera so post-processing effects are baked into the output. " +
                        "Capture Mode is ignored; the image is captured at the exact target resolution.",
                        MessageType.Info);
                    EditorGUI.indentLevel--;
                }

                // Open after
                openAfterCapture = EditorGUILayout.Toggle(
                    Icon("d_FolderOpened Icon", "  Open After Capture"), openAfterCapture);

                EditorGUILayout.Space(4);
                Separator();
                EditorGUILayout.Space(4);

                // Timer delay
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(Icon("d_UnityEditor.AnimationWindow", "Timer Delay (sec)"), GUILayout.Width(160));
                timerDelay = EditorGUILayout.IntSlider(timerDelay, 0, 10);
                EditorGUILayout.EndHorizontal();

                // Burst count
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(Icon("d_Grid.BoxTool", "Burst Count"), GUILayout.Width(160));
                burstCount = EditorGUILayout.IntSlider(burstCount, 1, 10);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // âââ Drawing: Sound & Hotkey Section âââââââââââââââââââââââââââââââââ
        private void DrawSoundHotkeySection()
        {
            _soundFold = EditorGUILayout.BeginFoldoutHeaderGroup(
                _soundFold,
                Icon("d_AudioClip Icon", "  Sound & Hotkey"));

            if (_soundFold)
            {
                EditorGUILayout.BeginVertical("box");

                playSoundOnCapture = EditorGUILayout.Toggle(
                    Icon("d_AudioSource Icon", "  Play Sound on Capture"), playSoundOnCapture);

                using (new EditorGUI.DisabledScope(!playSoundOnCapture))
                {
                    EditorGUI.indentLevel++;
                    captureSound = (AudioClip)EditorGUILayout.ObjectField(
                        "Sound Clip", captureSound, typeof(AudioClip), false);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space(4);
                Separator();
                EditorGUILayout.Space(4);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(Icon("d_KeyboardInputMapper", "Hotkey"), GUILayout.Width(120));
                hotkey = (CaptureHotkey)EditorGUILayout.EnumPopup(hotkey);
                EditorGUILayout.EndHorizontal();

                using (new EditorGUI.DisabledScope(hotkey == CaptureHotkey.None))
                {
                    EditorGUI.indentLevel++;
                    hotkeyOnlyInPlayMode = EditorGUILayout.Toggle("Play Mode Only", hotkeyOnlyInPlayMode);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // âââ Drawing: Watermark Section âââââââââââââââââââââââââââââââââââââââ
        private void DrawWatermarkSection()
        {
            _watermarkFold = EditorGUILayout.BeginFoldoutHeaderGroup(
                _watermarkFold,
                Icon("d_RawImage Icon", "  Watermark"));

            if (_watermarkFold)
            {
                EditorGUILayout.BeginVertical("box");

                watermarkLogo = (Texture2D)EditorGUILayout.ObjectField(
                    Icon("d_Texture Icon", "Texture"), watermarkLogo, typeof(Texture2D), false);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(Icon("d_Transform Icon", "Placement"), GUILayout.Width(120));
                watermarkCorner = (ScreenshotManager.LogoPlacement)EditorGUILayout.EnumPopup(watermarkCorner);
                EditorGUILayout.EndHorizontal();

                if (watermarkLogo != null)
                {
                    EditorGUILayout.Space(4);
                    Rect previewRect = GUILayoutUtility.GetRect(position.width - 32, 60, GUILayout.ExpandWidth(true));
                    EditorGUI.DrawPreviewTexture(previewRect, watermarkLogo, null, ScaleMode.ScaleToFit);
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // âââ Drawing: Maintenance Section âââââââââââââââââââââââââââââââââââââ
        private void DrawMaintenanceSection()
        {
            _maintenanceFold = EditorGUILayout.BeginFoldoutHeaderGroup(
                _maintenanceFold,
                Icon("d_SettingsIcon", "  Maintenance"));

            if (_maintenanceFold)
            {
                EditorGUILayout.BeginVertical("box");

                if (GUILayout.Button(Icon("d_Refresh", "Clear Legacy GameView Presets")))
                {
                    int r = GameViewPresetCleanup.RemoveScreenshotProPresets();
                    EditorUtility.DisplayDialog(TOOL_NAME, $"Removed {r} legacy preset(s).", "OK");
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // âââ Drawing: Actions âââââââââââââââââââââââââââââââââââââââââââââââââ
        private void DrawActions()
        {
            bool isPlaying = Application.isPlaying;
            bool capturing = _captureAtTime > 0;

            if (!isPlaying)
                EditorGUILayout.HelpBox("Enter Play Mode to capture screenshots.", MessageType.Info);

            // Countdown display
            if (capturing)
            {
                double rem    = _captureAtTime - EditorApplication.timeSinceStartup;
                int    remInt = Mathf.CeilToInt((float)rem);

                Rect countRect = GUILayoutUtility.GetRect(position.width - 24, 50, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(countRect, new Color(0.1f, 0.5f, 0.5f, 0.3f));

                var countStyle = new GUIStyle(EditorStyles.whiteLargeLabel)
                {
                    fontSize  = 24,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                GUI.Label(countRect, $"Capturing in {remInt}...", countStyle);

                if (GUILayout.Button("Cancel", GUILayout.Height(22)))
                    CancelCountdown();
            }
            else
            {
                // Main capture button
                bool infoShown = burstCount > 1 || timerDelay > 0;
                if (infoShown)
                {
                    string info = "";
                    if (timerDelay > 0) info += $"{timerDelay}s delay  ";
                    if (burstCount > 1) info += $"Ã{burstCount} burst";
                    EditorGUILayout.HelpBox(info.Trim(), MessageType.None);
                }

                using (new EditorGUI.DisabledScope(!isPlaying))
                {
                    GUI.backgroundColor = isPlaying
                        ? new Color(0.15f, 0.72f, 0.68f)
                        : new Color(0.35f, 0.35f, 0.35f);

                    string btnLabel = burstCount > 1
                        ? $"  CAPTURE Ã{burstCount}"
                        : "  CAPTURE";

                    var btnContent = Icon("d_SceneViewCamera", btnLabel);
                    if (GUILayout.Button(btnContent, _captureButtonStyle))
                        StartCapture();

                    GUI.backgroundColor = Color.white;
                }
            }

            EditorGUILayout.Space(4);

            // Open folder button
            if (GUILayout.Button(Icon("d_FolderOpened Icon", "  Open Screenshots Folder")))
            {
                string p = string.IsNullOrEmpty(folderName) || !Path.IsPathRooted(folderName)
                    ? Path.Combine(Application.dataPath, "..", folderName)
                    : folderName;
                if (!Directory.Exists(p)) Directory.CreateDirectory(p);
                EditorUtility.RevealInFinder(p);
            }
        }

        // âââ Drawing: Last Preview Section âââââââââââââââââââââââââââââââââââ
        private void DrawPreviewSection()
        {
            if (_lastPreview == null && string.IsNullOrEmpty(_lastCapturePath)) return;

            _previewFold = EditorGUILayout.BeginFoldoutHeaderGroup(
                _previewFold,
                Icon("d_Texture Icon", "  Last Screenshot"));

            if (_previewFold && _lastPreview != null)
            {
                EditorGUILayout.BeginVertical("box");

                // Thumbnail
                float maxH = 120f;
                float aspect = _lastPreview.width / (float)_lastPreview.height;
                Rect r = GUILayoutUtility.GetRect(position.width - 32, maxH, GUILayout.ExpandWidth(true));
                EditorGUI.DrawPreviewTexture(r, _lastPreview, null, ScaleMode.ScaleToFit);

                // Path label
                if (!string.IsNullOrEmpty(_lastCapturePath))
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label(Path.GetFileName(_lastCapturePath), _statusStyle);
                    if (GUILayout.Button("Show", GUILayout.Width(50)))
                        EditorUtility.RevealInFinder(_lastCapturePath);
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // âââ Capture Logic ââââââââââââââââââââââââââââââââââââââââââââââââââââ
        private void StartCapture()
        {
            if (!Application.isPlaying) return;

            if (timerDelay > 0)
            {
                _captureAtTime = EditorApplication.timeSinceStartup + timerDelay;
                EditorApplication.update += CountdownTick;
                Repaint();
            }
            else
            {
                CaptureNow();
            }
        }

        private void CountdownTick()
        {
            double rem = _captureAtTime - EditorApplication.timeSinceStartup;

            int remInt = Mathf.CeilToInt((float)rem);
            if (remInt != _countdownValue)
            {
                _countdownValue = remInt;
                Repaint();
            }

            if (rem <= 0)
            {
                EditorApplication.update -= CountdownTick;
                _captureAtTime  = -1;
                _countdownValue = 0;
                CaptureNow();
                Repaint();
            }
        }

        private void CancelCountdown()
        {
            EditorApplication.update -= CountdownTick;
            _captureAtTime  = -1;
            _countdownValue = 0;
            Repaint();
        }

        private void CaptureNow()
        {
            if (!Application.isPlaying) return;
            if (playSoundOnCapture && captureSound != null)
                AudioSource.PlayClipAtPoint(captureSound, Vector3.zero);

            var (w, h) = GetResolution();

            ScreenshotManager.TakeScreenshot(new ScreenshotManager.ScreenshotSettings
            {
                width              = w,
                height             = h,
                hideUI             = autoHideUI,
                folder             = folderName,
                filePrefix         = string.IsNullOrWhiteSpace(filePrefix) ? "Screenshot" : filePrefix,
                mode               = captureMode,
                watermark          = watermarkLogo,
                watermarkPlacement = watermarkCorner,
                format             = imageFormat,
                jpegQuality        = jpegQuality,
                openAfterCapture   = openAfterCapture,
                burstCount         = burstCount,
                usePostProcessing  = usePostProcessing
            });
        }

        private (int w, int h) GetResolution()
        {
            if (useCustomRes) return (Mathf.Max(1, customWidth), Mathf.Max(1, customHeight));
            return selectedRes switch
            {
                ResPreset.Full_HD_1080p => (1920, 1080),
                ResPreset.QHD_2K        => (2560, 1440),
                ResPreset.UHD_4K        => (3840, 2160),
                ResPreset.UHD_8K        => (7680, 4320),
                _                       => (3840, 2160)
            };
        }

        // âââ UI Helpers âââââââââââââââââââââââââââââââââââââââââââââââââââââââ
        private static GUIContent Icon(string iconName, string text = "")
        {
            GUIContent iconContent = EditorGUIUtility.IconContent(iconName);
            return (iconContent != null && iconContent.image != null)
                ? new GUIContent(text, iconContent.image)
                : new GUIContent(text);
        }

        private static void Separator()
        {
            Rect r = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(r, new Color(0.5f, 0.5f, 0.5f, 0.3f));
        }
    }
}
#endif
