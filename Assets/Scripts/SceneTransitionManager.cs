using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent (DontDestroyOnLoad) full-screen fade for scene transitions.
/// Must be persistent: the blackout has to stay visible THROUGH a
/// SceneManager.LoadScene call, and a CanvasGroup living in the scene being
/// unloaded would be destroyed mid-fade - along with any coroutine running
/// on it - before the fade-back-in half could ever execute.
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [SerializeField] private CanvasGroup blackoutCanvasGroup;
    [SerializeField] private float fadeOutDuration = 1f;
    [Tooltip("How long to hold on full black after the new scene has loaded, before fading back in.")]
    [SerializeField] private float holdDuration = 0.25f;
    [SerializeField] private float fadeInDuration = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (blackoutCanvasGroup != null)
        {
            blackoutCanvasGroup.alpha = 0f;
            blackoutCanvasGroup.blocksRaycasts = false;
            blackoutCanvasGroup.gameObject.SetActive(false);
        }
    }

    /// <summary>Fades to black, loads the given scene, holds, fades back in.</summary>
    public void TransitionToScene(string sceneName)
    {
        StartCoroutine(TransitionRoutine(sceneName));
    }

    private IEnumerator TransitionRoutine(string sceneName)
    {
        yield return Fade(0f, 1f, fadeOutDuration);

        SceneManager.LoadScene(sceneName);

        yield return new WaitForSeconds(holdDuration);
        yield return Fade(1f, 0f, fadeInDuration);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (blackoutCanvasGroup == null) yield break;

        blackoutCanvasGroup.gameObject.SetActive(true);
        blackoutCanvasGroup.blocksRaycasts = true;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            blackoutCanvasGroup.alpha = Mathf.Lerp(from, to, duration > 0f ? t / duration : 1f);
            yield return null;
        }

        blackoutCanvasGroup.alpha = to;

        if (to <= 0f)
        {
            blackoutCanvasGroup.blocksRaycasts = false;
            blackoutCanvasGroup.gameObject.SetActive(false);
        }
    }
}