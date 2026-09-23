using UnityEngine;

/// <summary>
/// The hub's exit door - opens the stew-selection carousel, UNLESS the
/// player has no capture tools at all, in which case NoToolsWarningUI gates
/// it first (Yes = proceed anyway, No = stay in the hub). The stick
/// (PlayerCapture) is always available regardless and isn't counted here -
/// this only checks the purchasable ToolData inventory. See decision log.
/// </summary>
public class Doors : MonoBehaviour, IInteractable
{
    [SerializeField] private string sceneName;


    public void UseDoor()
    {
        if (HasAnyTools() || NoToolsWarningUI.Instance == null)
        {
            OpenStewSelection();
        }
        else
        {
            NoToolsWarningUI.Instance.Open(OpenStewSelection);
        }
    }

    private static void OpenStewSelection()
    {
        ExpeditionStewSelectionUI.Instance.Open();
    }

    /// <summary>True if ANY tool slot (equipped or not) holds anything. Fails OPEN (returns true, no warning) if the tool inventory can't be checked at all, rather than blocking the player from ever leaving.</summary>
    private static bool HasAnyTools()
    {
        var tools = ToolInventoryManager.Instance;
        if (tools == null || tools.Slots == null) return true;

        foreach (var slot in tools.Slots)
        {
            if (slot != null && slot.data != null && slot.count > 0) return true;
        }

        return false;
    }

    public void Interact()
    {
        UseDoor();
    }
}
