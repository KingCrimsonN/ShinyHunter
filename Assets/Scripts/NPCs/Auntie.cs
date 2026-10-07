using UnityEngine;

public class Auntie : MonoBehaviour, IInteractable
{

    // [SerializeField] private UIManager uiManager;

    bool canGiveMoney = true;

    [SerializeField] private DialogueData dialogueData;
    [Tooltip("Optional: an empty child placed at the NPC's head - the camera turns to look at it when the dialogue starts. If empty, roughly head height is guessed from the collider.")]
    [SerializeField] private Transform lookTarget;


    public void Interact()
    {
        print("INTERACTING");
        if (dialogueData == null) return;
        DialogueManager.Instance.StartDialogue(dialogueData, DialogueManager.GetHeadPoint(transform, lookTarget));
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
