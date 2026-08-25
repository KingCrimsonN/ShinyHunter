using UnityEngine;

public class UIManager : MonoBehaviour
{

    [SerializeField] private GameObject inventoryUI;
    [SerializeField] private FirstPersonController playerMovement;
    [SerializeField] private ToolEquipController toolEquip;


    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (inventoryUI != null)
                inventoryUI.SetActive(!inventoryUI.activeSelf);
            Time.timeScale = inventoryUI.activeSelf ? 0 : 1;
            playerMovement.enabled = !inventoryUI.activeSelf;
            if (toolEquip != null) toolEquip.CanUse = !inventoryUI.activeSelf;
            Cursor.lockState = inventoryUI.activeSelf ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = inventoryUI.activeSelf;
        }
    }
}
