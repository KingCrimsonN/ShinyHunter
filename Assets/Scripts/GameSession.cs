using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ending a play session. The game's "persistent" objects (the GameManagers
/// root with every manager on it, and the Overlay with the HUD/tablet) are
/// DontDestroyOnLoad, so loading the main menu does NOT get rid of them: the
/// Overlay kept rendering - and blocking clicks - on top of the menu, and the
/// old inventory/money/stews would have leaked into the next game.
/// ReturnToMainMenu destroys those roots first, so the menu is clean and the
/// next "Start" builds a brand-new session from the Hub scene's own copies.
///
/// Deliberately NOT done by sweeping the whole DontDestroyOnLoad scene: that
/// also contains other packages' objects (DOTween's "[DOTween]" driver, debug
/// tools) which must survive. Only the roots of OUR singletons are destroyed.
/// </summary>
public static class GameSession
{
    public static void ReturnToMainMenu()
    {
        // The tablet freezes the game with timeScale 0 - the menu must not inherit that.
        Time.timeScale = 1f;

        DestroyPersistentRoots();
        SceneManager.LoadScene(SceneNames.MainMenu);
    }

    public static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// Every manager lives on one of these roots: GameManagers (PlayerStateManager
    /// stands in for the whole group - inventory, tools, money, stews, health,
    /// transitions, sound all share it) and the Overlay (UIManager, plus the
    /// DialogueManager if it's parented there). Destroyed rather than reset:
    /// the next session's Awakes then rebuild everything exactly as a first
    /// launch does, so there's no "reset every field" list to keep in sync.
    /// </summary>
    private static void DestroyPersistentRoots()
    {
        var roots = new HashSet<GameObject>();
        AddRoot(roots, PlayerStateManager.Instance);
        AddRoot(roots, InventoryManager.Instance);
        AddRoot(roots, SceneTransitionManager.Instance);
        AddRoot(roots, UIManager.Instance);
        AddRoot(roots, DialogueManager.Instance);

        // Destroy (end of frame), not DestroyImmediate: this usually runs from
        // a button that lives on one of these very roots.
        foreach (var root in roots)
            Object.Destroy(root);
    }

    private static void AddRoot(HashSet<GameObject> roots, Component component)
    {
        if (component != null) roots.Add(component.transform.root.gameObject);
    }
}
