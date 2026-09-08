using UnityEngine;

/// <summary>
/// Glue between Interactor's IInteractable and the dialogue system. Put this
/// on any NPC/object (needs a Collider for Interactor's raycast to hit).
/// </summary>
// [RequireComponent(typeof(Collider))]
public class NPCDialogueTrigger : MonoBehaviour, IInteractable
{
    [SerializeField] private DialogueData dialogueData;

    public void Interact()
    {
        print("INTERACTING");
        if (dialogueData == null) return;
        DialogueManager.Instance.StartDialogue(dialogueData);
    }
}
