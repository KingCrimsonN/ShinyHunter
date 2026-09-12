using UnityEngine;
using UnityEditor;

namespace Auxilium.ScreenshotPro
{
    public static class ScreenshotProSettings
    {
        public static void CaptureFromSavedSettings()
        {
            if (!Application.isPlaying) return;

            int    resIndex     = EditorPrefs.GetInt(ScreenshotEditorWindow.PREF_PRESET, 2);
            bool   hideUI       = EditorPrefs.GetBool(ScreenshotEditorWindow.PREF_HIDE_UI, false);
            int    modeIndex    = EditorPrefs.GetInt(ScreenshotEditorWindow.PREF_MODE, 0);
            string folder       = EditorPrefs.GetString(ScreenshotEditorWindow.PREF_FOLDER, "Captures");
            int    cornerIndex  = EditorPrefs.GetInt(ScreenshotEditorWindow.PREF_WATERMARK_CORNER, 3);
            int    fmtIndex     = EditorPrefs.GetInt(ScreenshotEditorWindow.PREF_FORMAT, 0);
            int    jpegQ        = EditorPrefs.GetInt(ScreenshotEditorWindow.PREF_JPEG_QUALITY, 90);
            string prefix       = EditorPrefs.GetString(ScreenshotEditorWindow.PREF_FILE_PREFIX, "Screenshot");
            bool   openAfter    = EditorPrefs.GetBool(ScreenshotEditorWindow.PREF_OPEN_AFTER, false);
            bool   customRes    = EditorPrefs.GetBool(ScreenshotEditorWindow.PREF_CUSTOM_RES, false);
            int    customW      = EditorPrefs.GetInt(ScreenshotEditorWindow.PREF_CUSTOM_W, 1920);
            int    customH      = EditorPrefs.GetInt(ScreenshotEditorWindow.PREF_CUSTOM_H, 1080);
            int    burst        = EditorPrefs.GetInt(ScreenshotEditorWindow.PREF_BURST_COUNT, 1);

            int w, h;
            if (customRes)
            {
                w = customW; h = customH;
            }
            else
            {
                (w, h) = resIndex switch
                {
                    0 => (1920, 1080),
                    1 => (2560, 1440),
                    3 => (7680, 4320),
                    _ => (3840, 2160)
                };
            }

            ScreenshotManager.TakeScreenshot(new ScreenshotManager.ScreenshotSettings
            {
                width              = w,
                height             = h,
                hideUI             = hideUI,
                folder             = folder,
                filePrefix         = prefix,
                mode               = (ScreenshotManager.CaptureMode)modeIndex,
                watermark          = null,
                watermarkPlacement = (ScreenshotManager.LogoPlacement)cornerIndex,
                format             = (ScreenshotManager.ImageFormat)fmtIndex,
                jpegQuality        = jpegQ,
                openAfterCapture   = openAfter,
                burstCount         = burst
            });
        }
    }
}
