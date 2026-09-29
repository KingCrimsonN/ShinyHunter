using UnityEngine;

/// <summary>
/// Escape used to be handled here directly (its own Update() checking
/// Input.GetKeyDown(KeyCode.Escape)) - removed. UIManager's Escape handling
/// is now the single place that decides what Escape does; it calls
/// PlayerStateManager.TryCloseCurrentPopup(), which invokes whatever's
/// registered - see the Freeze(ClosePotions) call below. Having both this
/// and UIManager listening for the same keypress raced (no defined order
/// for two separate Update() calls), which is what let closing this also
/// pop the tablet's Settings page open on the same press. See decision log.
/// </summary>
public class PotionMaker : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject potionUI;

    // UIManager is persistent and lives on the shared Overlay - always go
    // through Instance rather than caching a FindFirstObjectByType result,
    // which can grab a duplicate Overlay that's about to be destroyed.
    private static void SetExtraOpened(bool value)
    {
        if (UIManager.Instance != null) UIManager.Instance.extraOpened = value;
    }

    public void Interact()
    {
        SetExtraOpened(true);
        if (potionUI != null)
        {
            potionUI.SetActive(!potionUI.activeSelf);
            if (potionUI.activeSelf)
                PlayerStateManager.Instance.Freeze(ClosePotions);
        }
    }

    public void ClosePotions()
    {
        SetExtraOpened(false);
        if (potionUI != null)
        {
            potionUI.SetActive(false);
            PlayerStateManager.Instance.Unfreeze();
        }
    }

}
