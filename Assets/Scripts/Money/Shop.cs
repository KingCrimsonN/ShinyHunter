using UnityEngine;

public class Shop : MonoBehaviour, IInteractable
{

    public GameObject shopUI;

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
        if (shopUI != null)
        {
            shopUI.SetActive(!shopUI.activeSelf);
            if (shopUI.activeSelf)
                PlayerStateManager.Instance.Freeze();
        }
    }

    public void CloseShop()
    {

        if (shopUI != null)
        {
            SetExtraOpened(false);
            shopUI.SetActive(false);
            PlayerStateManager.Instance.Unfreeze();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && shopUI != null && shopUI.activeSelf)
        {
            CloseShop();
        }
    }

}
