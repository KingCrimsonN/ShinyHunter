using UnityEngine;

public class Auntie : MonoBehaviour, IInteractable
{

    [SerializeField] private UIManager uiManager;

    bool canGiveMoney = true;

    // TODO: Implement Dialogue System;
    public void Interact()
    {
        uiManager.ShowDialogue();
        if (!canGiveMoney) return;
        MoneyManager.Instance.AddMoney(InventoryManager.Instance.CalculateCaptureValue());
        canGiveMoney = false;
    }
}
