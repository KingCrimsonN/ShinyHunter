using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent (DontDestroyOnLoad) central point for freezing/unfreezing
/// player input - movement, interaction, the stick, and tool use - so other
/// systems (dialogue, popups, minigames) call ONE thing instead of each
/// doing their own player lookups and enable/disable logic.
///
/// The player prefab itself is NOT persistent, so references are re-fetched
/// on every scene load (SceneManager.sceneLoaded) rather than cached once.
///
/// NOTE: the four target scripts don't share one convention for being
/// paused - FirstPersonController/Interactor use the standard `.enabled`
/// toggle, while PlayerCapture/ToolEquipController use their own bool flags
/// (`isActive`/`CanUse`). This manager absorbs that inconsistency so callers
/// don't need to know about it.
/// </summary>
public class PlayerStateManager : MonoBehaviour
{
    public static PlayerStateManager Instance { get; private set; }

    private FirstPersonController movement;
    private Interactor interactor;
    private PlayerCapture playerCapture;
    private ToolEquipController toolEquip;

    public bool IsFrozen { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        RefreshReferences();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshReferences();

        // A scene load happening while frozen would otherwise leave the NEW
        // scene's player references never having had Freeze() applied to
        // them at all - re-apply whatever the current state is to whatever
        // we just found, so a frozen state survives the scene boundary
        // (and an unfrozen one stays unfrozen).
        ApplyState(IsFrozen);
    }

    /// <summary>Re-finds all four target components in the current scene. Called automatically on scene load; safe to call manually too.</summary>
    public void RefreshReferences()
    {
        movement = FindFirstObjectByType<FirstPersonController>();
        interactor = FindFirstObjectByType<Interactor>();
        playerCapture = FindFirstObjectByType<PlayerCapture>();
        toolEquip = FindFirstObjectByType<ToolEquipController>();
    }

    public void Freeze()
    {
        IsFrozen = true;
        ApplyState(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Unfreeze()
    {
        IsFrozen = false;
        ApplyState(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ApplyState(bool frozen)
    {
        if (movement != null) movement.enabled = !frozen;
        if (interactor != null) interactor.enabled = !frozen;
        if (playerCapture != null) playerCapture.isActive = !frozen;
        if (toolEquip != null) toolEquip.CanUse = !frozen;
    }
}