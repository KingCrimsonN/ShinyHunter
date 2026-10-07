using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Put on a Button to make it end the session: back to the main menu, or quit.
/// It hooks its own button's onClick in code, so there's no OnClick target to
/// wire - which is the point: a persistent OnClick call on a prefab (like the
/// Overlay) can't point at a scene object such as SceneTransitionManager, so
/// those entries sat empty in the prefab and only worked where a scene had
/// overridden them by hand. Remove any old OnClick entries from the button.
/// </summary>
[RequireComponent(typeof(Button))]
public class GameSessionButton : MonoBehaviour
{
    public enum SessionAction { ReturnToMainMenu, QuitGame }

    [SerializeField] private SessionAction action = SessionAction.ReturnToMainMenu;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(Execute);
    }

    private void Execute()
    {
        if (action == SessionAction.ReturnToMainMenu) GameSession.ReturnToMainMenu();
        else GameSession.Quit();
    }
}
