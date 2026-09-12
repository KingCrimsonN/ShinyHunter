using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Auxilium.ScreenshotPro
{
    public static class ScreenshotManager
    {
        // ─── Enums ───────────────────────────────────────────────────────────
        public enum LogoPlacement  { TopLeft, TopRight, BottomLeft, BottomRight }
        public enum CaptureMode    { Safe_UpscaleFromGameView = 0, Pro_NativeGameView = 1 }
        public enum ImageFormat    { PNG = 0, JPEG = 1 }

        // ─── Settings Struct ──────────────────────────────────────────────────
        public struct ScreenshotSettings
        {
            public int           width;
            public int           height;
            public bool          hideUI;
            public string        folder;
            public string        filePrefix;
            public CaptureMode   mode;
            public Texture2D     watermark;
            public LogoPlacement watermarkPlacement;
            public ImageFormat   format;
            public int           jpegQuality;  // 1-100
            public bool          openAfterCapture;
            public int           burstCount;        // >= 1
            public bool          usePostProcessing; // render via camera to capture PP effects
        }

        // ─── Events ───────────────────────────────────────────────────────────
        /// <summary>Raised (on the main thread) after each screenshot is saved, with its full path.</summary>
        public static event Action<string> OnScreenshotSaved;

        // ─── Legacy API (backwards-compatible) ───────────────────────────────
        public static void TakeScreenshot(
            int width, int height, bool hideUI, string folder,
            CaptureMode mode, Texture2D logo = null,
            LogoPlacement placement = LogoPlacement.BottomRight)
        {
            TakeScreenshot(new ScreenshotSettings
            {
                width              = width,
                height             = height,
                hideUI             = hideUI,
                folder             = folder,
                filePrefix         = "Screenshot",
                mode               = mode,
                watermark          = logo,
                watermarkPlacement = placement,
                format             = ImageFormat.PNG,
                jpegQuality        = 90,
                openAfterCapture   = false,
                burstCount         = 1
            });
        }

        // ─── Primary API ──────────────────────────────────────────────────────
        public static void TakeScreenshot(ScreenshotSettings settings)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return;

            string dir = Path.Combine(
                Application.dataPath, "..",
                string.IsNullOrWhiteSpace(settings.folder) ? "Captures" : settings.folder);

            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            int count  = Mathf.Max(1, settings.burstCount);
            string ext = settings.format == ImageFormat.JPEG ? "jpg" : "png";
            string pfx = string.IsNullOrWhiteSpace(settings.filePrefix) ? "Screenshot" : settings.filePrefix;

            for (int i = 0; i < count; i++)
            {
                string suffix = count > 1 ? $"_{i + 1:D2}" : "";
                string path   = Path.Combine(dir, $"{pfx}_{DateTime.Now:yyyyMMdd_HHmmss}{suffix}.{ext}");
                ScreenshotCaptureRunner.Run(CaptureRoutine(settings, path));
            }
#endif
        }

#if UNITY_EDITOR
        // ─── Coroutine ────────────────────────────────────────────────────────
        private static IEnumerator CaptureRoutine(ScreenshotSettings s, string path)
        {
            yield return new WaitForEndOfFrame();

            // Optionally hide all canvases
            Canvas[] canvases = s.hideUI
                ? UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                : null;
            bool[] states = null;

            if (canvases != null)
            {
                states = new bool[canvases.Length];
                for (int i = 0; i < canvases.Length; i++)
                {
                    states[i] = canvases[i].enabled;
                    canvases[i].enabled = false;
                }
                yield return new WaitForEndOfFrame();
            }

            Texture2D shot = null;

            if (s.usePostProcessing)
            {
                // ── Post-processing path ──────────────────────────────────────
                // Render the main camera directly into a RenderTexture at the
                // target resolution. This runs the full pipeline (including any
                // post-processing stack attached to the camera) instead of the
                // bare screen blit that ScreenCapture.CaptureScreenshotAsTexture
                // would produce.
                Camera cam = Camera.main;
                if (cam == null)
                    cam = UnityEngine.Object.FindFirstObjectByType<Camera>();

                if (cam != null)
                {
                    RenderTexture rt = new RenderTexture(s.width, s.height, 24,
                                                         RenderTextureFormat.ARGB32);
                    rt.antiAliasing = 1;

                    RenderTexture prevTarget = cam.targetTexture;
                    cam.targetTexture = rt;
                    cam.Render();
                    cam.targetTexture = prevTarget;

                    RenderTexture prev = RenderTexture.active;
                    RenderTexture.active = rt;

                    shot = new Texture2D(s.width, s.height, TextureFormat.RGBA32, false);
                    shot.ReadPixels(new Rect(0, 0, s.width, s.height), 0, 0);
                    shot.Apply();

                    RenderTexture.active = prev;
                    rt.Release();
                    UnityEngine.Object.DestroyImmediate(rt);
                }
                else
                {
                    Debug.LogWarning("[Screenshot Pro] Post-processing capture: no camera found. " +
                                     "Falling back to ScreenCapture.");
                    shot = ScreenCapture.CaptureScreenshotAsTexture();
                }
            }
            else
            {
                // ── Standard path ─────────────────────────────────────────────
                shot = ScreenCapture.CaptureScreenshotAsTexture();
            }

            if (shot != null)
            {
                // When using the post-processing path the texture is already at
                // the requested resolution, so skip the GPU scale step.
                bool needsScale = !s.usePostProcessing
                               && (s.mode == CaptureMode.Safe_UpscaleFromGameView
                                   || shot.width  != s.width
                                   || shot.height != s.height);

                Texture2D final = needsScale ? ScaleTextureGPU(shot, s.width, s.height) : shot;

                // Watermark
                if (s.watermark != null)
                    ApplyWatermark(final, s.watermark, s.watermarkPlacement);

                // Encode
                byte[] bytes = s.format == ImageFormat.JPEG
                    ? final.EncodeToJPG(Mathf.Clamp(s.jpegQuality, 1, 100))
                    : final.EncodeToPNG();

                File.WriteAllBytes(path, bytes);
                Debug.Log($"[Screenshot Pro] Saved: {path}");

                // Cleanup
                if (final != shot) UnityEngine.Object.DestroyImmediate(final);
                UnityEngine.Object.DestroyImmediate(shot);

                // Notify subscribers (e.g. preview thumbnail in editor window)
                OnScreenshotSaved?.Invoke(path);

                // Open in OS file browser
                if (s.openAfterCapture)
                    UnityEditor.EditorUtility.RevealInFinder(path);
            }

            // Restore canvases
            if (canvases != null)
                for (int i = 0; i < canvases.Length; i++)
                    if (canvases[i] != null) canvases[i].enabled = states[i];
        }

        // ─── GPU-accelerated Scaling ──────────────────────────────────────────
        /// <summary>
        /// Bilinear-upscale/downscale using the GPU via RenderTexture.
        /// Dramatically faster than the old CPU SetPixel loop for 4K/8K targets.
        /// </summary>
        private static Texture2D ScaleTextureGPU(Texture2D src, int targetW, int targetH)
        {
            RenderTexture rt = RenderTexture.GetTemporary(targetW, targetH, 0, RenderTextureFormat.ARGB32);
            rt.filterMode = FilterMode.Bilinear;
            Graphics.Blit(src, rt);

            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;

            Texture2D result = new Texture2D(targetW, targetH, TextureFormat.RGBA32, false);
            result.ReadPixels(new Rect(0, 0, targetW, targetH), 0, 0);
            result.Apply();

            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return result;
        }

        // ─── Watermark ────────────────────────────────────────────────────────
        private static void ApplyWatermark(Texture2D bg, Texture2D logo, LogoPlacement placement)
        {
            if (!logo.isReadable)
            {
                Debug.LogWarning("[Screenshot Pro] Watermark texture is not readable. " +
                                 "Enable Read/Write Import Setting on the texture asset.");
                return;
            }

            int w = Mathf.RoundToInt(bg.width * 0.15f);
            int h = Mathf.RoundToInt(logo.height * ((float)w / logo.width));

            // GPU-scale the logo
            RenderTexture rt = RenderTexture.GetTemporary(w, h);
            Graphics.Blit(logo, rt);
            Texture2D scaledLogo = new Texture2D(w, h, TextureFormat.RGBA32, false);
            RenderTexture.active = rt;
            scaledLogo.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            scaledLogo.Apply();
            RenderTexture.ReleaseTemporary(rt);

            int pad = Mathf.RoundToInt(bg.width * 0.02f);
            int x = (placement == LogoPlacement.TopRight || placement == LogoPlacement.BottomRight)
                  ? bg.width - w - pad : pad;
            int y = (placement == LogoPlacement.TopLeft  || placement == LogoPlacement.TopRight)
                  ? bg.height - h - pad : pad;

            Color[] logoPix = scaledLogo.GetPixels();
            Color[] bgPix   = bg.GetPixels(x, y, w, h);
            for (int i = 0; i < logoPix.Length; i++)
                bgPix[i] = Color.Lerp(bgPix[i], logoPix[i], logoPix[i].a);

            bg.SetPixels(x, y, w, h, bgPix);
            bg.Apply();
            UnityEngine.Object.DestroyImmediate(scaledLogo);
        }
#endif
    }
}
