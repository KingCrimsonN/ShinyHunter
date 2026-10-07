using UnityEngine;

/// <summary>
/// Glue between Interactor's IInteractable and the dialogue system. Put this
/// on any NPC/object (needs a Collider for Interactor's raycast to hit).
/// </summary>
// [RequireComponent(typeof(Collider))]
public class NPCDialogueTrigger : MonoBehaviour, IInteractable
{
    [SerializeField] private DialogueData dialogueData;
    [Tooltip("Optional: an empty child placed at the NPC's head - the camera turns to look at it when the dialogue starts. If empty, roughly head height is guessed from the collider.")]
    [SerializeField] private Transform lookTarget;


    public void Interact()
    {
        print("INTERACTING");
        if (dialogueData == null) return;
        DialogueManager.Instance.StartDialogue(dialogueData, DialogueManager.GetHeadPoint(transform, lookTarget));
    }
}
