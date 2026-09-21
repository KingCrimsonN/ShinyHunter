using UnityEngine;

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
                PlayerStateManager.Instance.Freeze();
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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && potionUI != null && potionUI.activeSelf)
        {
            ClosePotions();
        }
    }

}
