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

    /// <summary>True from the moment a transition starts until its fade-in has finished.</summary>
    public bool IsTransitioning { get; private set; }

    /// <summary>
    /// Fades to black, loads the given scene, holds, fades back in. Returns
    /// false (and does nothing) if a transition is already running or the
    /// scene can't be loaded - so callers can avoid consuming anything
    /// (e.g. a stew) for a transition that never happens.
    ///
    /// The player is frozen for the whole transition and released once the
    /// new scene has loaded. PlayerStateManager re-applies its frozen state
    /// to the new scene's player on load, so there's no window where the
    /// player can move (or re-trigger the door) mid-transition.
    /// </summary>
    public bool TransitionToScene(string sceneName)
    {
        if (IsTransitioning) return false;

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"SceneTransitionManager: scene '{sceneName}' can't be loaded - is it added to the Build Settings?");
            return false;
        }

        StartCoroutine(TransitionRoutine(sceneName));
        return true;
    }

    public void TransitionToSceneImmediate(string sceneName)
    {
        if (IsTransitioning) return;

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"SceneTransitionManager: scene '{sceneName}' can't be loaded - is it added to the Build Settings?");
            return;
        }

        SceneManager.LoadScene(sceneName);
        Destroy(gameObject);
    }

    private IEnumerator TransitionRoutine(string sceneName)
    {
        IsTransitioning = true;
        if (PlayerStateManager.Instance != null) PlayerStateManager.Instance.Freeze();

        yield return Fade(0f, 1f, fadeOutDuration);

        SceneManager.LoadScene(sceneName);

        yield return new WaitForSeconds(holdDuration);

        if (PlayerStateManager.Instance != null) PlayerStateManager.Instance.Unfreeze();
        yield return Fade(1f, 0f, fadeInDuration);

        IsTransitioning = false;
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