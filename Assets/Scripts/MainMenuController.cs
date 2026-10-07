using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The main menu scene's own, scene-only script (nothing persistent): Start
/// loads the game, Quit exits. Replaces using SceneTransitionManager here -
/// that is a persistent manager meant for in-game fades, and the menu needs
/// none of the persistent managers at all (the Hub scene brings its own
/// GameManagers + Overlay, which become the persistent ones). So the menu
/// scene should contain NO GameManagers instance.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string gameSceneName = SceneNames.Hub;

    private void Start()
    {
        // Arriving from a game: the tablet may have left the game paused and the cursor hidden/locked.
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>Wire to the Start button's OnClick.</summary>
    public void StartGame()
    {
        if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
        {
            Debug.LogError($"MainMenuController: scene '{gameSceneName}' can't be loaded - is it in the Build Settings?");
            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>Wire to the Quit button's OnClick.</summary>
    public void QuitGame() => GameSession.Quit();
}
