using UnityEngine;

public class Shop : MonoBehaviour, IInteractable
{

    public GameObject shopUI;

    public void Interact()
    {
        if (shopUI != null)
        {
            shopUI.SetActive(!shopUI.activeSelf);
            Time.timeScale = shopUI.activeSelf ? 0 : 1;
            Cursor.lockState = shopUI.activeSelf ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = shopUI.activeSelf;
        }
    }

    public void CloseShop()
    {
        if (shopUI != null)
        {
            shopUI.SetActive(false);
            Time.timeScale = 1;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
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
