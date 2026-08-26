using UnityEngine;

public class Auntie : MonoBehaviour, IInteractable
{

    bool canGiveMoney = true;

    // TODO: Implement Dialogue System;
    public void Interact()
    {
        if (!canGiveMoney) return;
        MoneyManager.Instance.AddMoney(InventoryManager.Instance.CalculateCaptureValue());
        canGiveMoney = false;
    }
}
