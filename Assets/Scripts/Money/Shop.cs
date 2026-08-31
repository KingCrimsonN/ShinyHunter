using UnityEngine;

public class Shop : MonoBehaviour, IInteractable
{

    public GameObject shopUI;

    private UIManager uiManager;

    private void Awake()
    {
        uiManager = FindFirstObjectByType<UIManager>();
    }

    public void Interact()
    {
        uiManager.extraOpened = true;
        if (shopUI != null)
        {
            shopUI.SetActive(!shopUI.activeSelf);
            if (shopUI.activeSelf)
                uiManager.LockPlayer();
        }
    }

    public void CloseShop()
    {

        if (shopUI != null)
        {
            uiManager.extraOpened = false;
            shopUI.SetActive(false);
            uiManager.UnlockPlayer();
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
