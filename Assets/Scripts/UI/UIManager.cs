using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{

    [SerializeField] private GameObject tabletUI;
    [SerializeField] private GameObject creatureInventoryUI;
    [SerializeField] private GameObject inventoryPage;
    [SerializeField] private GameObject mapPage;
    [SerializeField] private GameObject settingsPage;
    [SerializeField] private GameObject dialogPanel;





    [SerializeField] private FirstPersonController playerMovement;
    [SerializeField] private PlayerCapture playerCapture;
    [SerializeField] private ToolEquipController toolEquip;

    [SerializeField] private TMP_Text interactionText;

    public bool extraOpened;

    private void Start()
    {
        if (tabletUI != null) tabletUI.SetActive(false);
        playerMovement = FindFirstObjectByType<FirstPersonController>();
        playerCapture = FindFirstObjectByType<PlayerCapture>();
        toolEquip = FindFirstObjectByType<ToolEquipController>();
        extraOpened = false;
    }

    // Update is called once per frame
    private void Update()
    {
        if (extraOpened)
            return;
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleTabletUI();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (tabletUI.activeSelf)
            {
                HideTabletUI();
                return;
            }
            if (CreatureTransformStationUI.Instance.IsOpen())
            {
                CreatureTransformStationUI.Instance.Close();
                return;
            }
            ToggleTabletUI();
            ShowSettingsPage();
        }
    }

    public void ShowDialogue()
    {
        dialogPanel.SetActive(true);
    }

    public void HideDialogue()
    {
        dialogPanel.SetActive(false);
    }

    public void ToggleTabletUI()
    {
        if (tabletUI != null)
            tabletUI.SetActive(!tabletUI.activeSelf);
        if (creatureInventoryUI != null)
            creatureInventoryUI.SetActive(tabletUI.activeSelf);
        if (playerCapture != null)
            playerCapture.isActive = !tabletUI.activeSelf;
        if (tabletUI.activeSelf)
            LockPlayer();
        else
            UnlockPlayer();
        // Time.timeScale = tabletUI.activeSelf ? 0 : 1;
        // playerMovement.enabled = !tabletUI.activeSelf;
        // if (toolEquip != null) toolEquip.CanUse = !tabletUI.activeSelf;
        // Cursor.lockState = tabletUI.activeSelf ? CursorLockMode.None : CursorLockMode.Locked;
        // Cursor.visible = tabletUI.activeSelf;
    }

    public void LockPlayer()
    {
        Time.timeScale = 0;
        playerMovement.enabled = false;
        if (toolEquip != null) toolEquip.CanUse = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void UnlockPlayer()
    {
        Time.timeScale = 1;
        playerMovement.enabled = true;
        if (toolEquip != null) toolEquip.CanUse = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void HideTabletUI()
    {
        if (tabletUI != null && tabletUI.activeSelf)
        {
            tabletUI.SetActive(false);
            if (creatureInventoryUI != null)
                creatureInventoryUI.SetActive(false);
            if (playerCapture != null)
                playerCapture.enabled = true;
            UnlockPlayer();
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
