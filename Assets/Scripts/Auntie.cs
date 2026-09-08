using UnityEngine;

public class Auntie : MonoBehaviour, IInteractable
{

    // [SerializeField] private UIManager uiManager;

    bool canGiveMoney = true;

    [SerializeField] private DialogueData dialogueData;

    public void Interact()
    {
        print("INTERACTING");
        if (dialogueData == null) return;
        DialogueManager.Instance.StartDialogue(dialogueData);
    }

    // TODO: Implement Dialogue System;
    // public void Interact()
    // {
    //     // uiManager.ShowDialogue();
    //     // if (!canGiveMoney) return;
    //     // MoneyManager.Instance.AddMoney(InventoryManager.Instance.CalculateCaptureValue());
    //     // canGiveMoney = false;
    // }
}
