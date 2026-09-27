using UnityEngine;

public interface IInteractable
{
    void Interact();
}

/// <summary>
/// Raycasts forward each frame for an IInteractable and shows/hides "Press E
/// to interact" accordingly.
///
/// Disabled centrally by PlayerStateManager.Freeze while any popup (shop,
/// transform table, brewing, dialogue, a door's stew carousel...) owns the
/// player - OnDisable hides the prompt right then, so it can't linger over
/// whatever just opened. This matters because disabling a component mid-frame
/// does NOT cancel an already-running Update() call: if Interact() itself
/// triggers the freeze, the SAME Update() call that just called it keeps
/// running afterward, so relying only on "Update() stops running once
/// disabled" would still let the prompt get shown one more time on that
/// exact frame before actually disappearing. See decision log.
/// </summary>
public class Interactor : MonoBehaviour
{
    public Transform interactionSource;
    public float interactionRange = 2f;

    private void OnDisable()
    {
        if (UIManager.Instance != null) UIManager.Instance.HideInteractionText();
    }

    private void Update()
    {
        IInteractable interactable = null;

        if (Physics.Raycast(interactionSource.position, interactionSource.forward, out RaycastHit hit, interactionRange))
            hit.collider.TryGetComponent(out interactable);

        if (interactable != null && Input.GetKeyDown(KeyCode.E))
        {
            interactable.Interact();
            // Interact() may open a popup (freezing the player, disabling
            // this component - see OnDisable above). Hide immediately and
            // bail out rather than falling through to ShowInteractionText
            // below, which would otherwise re-show it on this exact frame
            // regardless of what Interact() just did.
            UIManager.Instance.HideInteractionText();
            return;
        }

        if (interactable != null)
            UIManager.Instance.ShowInteractionText("Press E to interact");
        else
            UIManager.Instance.HideInteractionText();
    }
}
