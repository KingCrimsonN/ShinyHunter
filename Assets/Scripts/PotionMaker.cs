using UnityEngine;

public class PotionMaker : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject potionUI;

    private UIManager uiManager;

    private void Awake()
    {
        uiManager = FindFirstObjectByType<UIManager>();
    }

    public void Interact()
    {
        uiManager.extraOpened = true;
        if (potionUI != null)
        {
            potionUI.SetActive(!potionUI.activeSelf);
            if (potionUI.activeSelf)
                uiManager.LockPlayer();
        }
    }

    public void ClosePotions()
    {
        uiManager.extraOpened = true;
        if (potionUI != null)
        {
            potionUI.SetActive(false);
            uiManager.UnlockPlayer();
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
