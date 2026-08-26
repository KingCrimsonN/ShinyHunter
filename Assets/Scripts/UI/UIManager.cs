using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{

    [SerializeField] private GameObject tabletUI;
    [SerializeField] private GameObject creatureInventoryUI;
    [SerializeField] private GameObject inventoryPage;
    [SerializeField] private GameObject mapPage;
    [SerializeField] private GameObject settingsPage;





    [SerializeField] private FirstPersonController playerMovement;
    [SerializeField] private PlayerCapture playerCapture;
    [SerializeField] private ToolEquipController toolEquip;

    [SerializeField] private TMP_Text interactionText;

    private void Start()
    {
        if (tabletUI != null) tabletUI.SetActive(false);
        playerMovement = FindFirstObjectByType<FirstPersonController>();
        playerCapture = FindFirstObjectByType<PlayerCapture>();
        toolEquip = FindFirstObjectByType<ToolEquipController>();
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (tabletUI != null)
                tabletUI.SetActive(!tabletUI.activeSelf);
            if (creatureInventoryUI != null)
                creatureInventoryUI.SetActive(tabletUI.activeSelf);
            if (playerCapture != null)
                playerCapture.enabled = !tabletUI.activeSelf;
            Time.timeScale = tabletUI.activeSelf ? 0 : 1;
            playerMovement.enabled = !tabletUI.activeSelf;
            if (toolEquip != null) toolEquip.CanUse = !tabletUI.activeSelf;
            Cursor.lockState = tabletUI.activeSelf ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = tabletUI.activeSelf;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (tabletUI != null && tabletUI.activeSelf)
            {
                tabletUI.SetActive(false);
                if (creatureInventoryUI != null)
                    creatureInventoryUI.SetActive(false);
                if (playerCapture != null)
                    playerCapture.enabled = true;
                Time.timeScale = 1;
                playerMovement.enabled = true;
                if (toolEquip != null) toolEquip.CanUse = true;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    public void ShowCreatures()
    {
        if (creatureInventoryUI != null)
            creatureInventoryUI.SetActive(true);
    }

    public void HideCreatures()
    {
        if (creatureInventoryUI != null)
        {
            creatureInventoryUI.SetActive(false);
        }
    }

    public void ShowInteractionText(string text)
    {
        if (interactionText != null)
        {
            interactionText.text = text;
            interactionText.gameObject.SetActive(true);
        }
    }

    public void HideInteractionText()
    {
        if (interactionText != null)
        {
            interactionText.gameObject.SetActive(false);
        }
    }

    public void ShowInventoryPage()
    {
        if (inventoryPage != null)
            inventoryPage.SetActive(true);
        if (mapPage != null)
            mapPage.SetActive(false);
        if (settingsPage != null)
            settingsPage.SetActive(false);
    }

    public void ShowMapPage()
    {
        if (inventoryPage != null)
            inventoryPage.SetActive(false);
        if (mapPage != null)
            mapPage.SetActive(true);
        if (settingsPage != null)
            settingsPage.SetActive(false);
    }

    public void ShowSettingsPage()
    {
        if (inventoryPage != null)
            inventoryPage.SetActive(false);
        if (mapPage != null)
            mapPage.SetActive(false);
        if (settingsPage != null)
            settingsPage.SetActive(true);
    }
}
