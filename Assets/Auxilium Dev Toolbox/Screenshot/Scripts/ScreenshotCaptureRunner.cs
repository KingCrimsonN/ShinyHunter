using System.Collections;
using UnityEngine;

namespace Auxilium.ScreenshotPro
{
    public class ScreenshotCaptureRunner : MonoBehaviour
    {
        private static ScreenshotCaptureRunner _instance;

        public static void Run(IEnumerator routine)
        {
            if (!Application.isPlaying) return;

            if (_instance == null)
            {
                var go = new GameObject("ScreenshotPro_Internal_Runner") { hideFlags = HideFlags.HideAndDontSave };
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<ScreenshotCaptureRunner>();
            }
            _instance.StartCoroutine(routine);
        }
    }
}