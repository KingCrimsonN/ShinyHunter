using UnityEngine;

/// <summary>
/// Template for dispatching dialogue choice actionIds to actual scene
/// functionality. Scene-specific (not persistent) - place one wherever a
/// dialogue in that scene has choices with custom effects. Add a case per
/// actionId string used in your DialogueChoice entries.
/// </summary>
public class DialogueActionRouter : MonoBehaviour
{
    private DialogueManager subscribedTo;

    // DialogueManager is persistent and its Awake may not have run yet when
    // this OnEnable does (scene start order is undefined) - the old
    // `Instance.OnChoiceAction += ...` here threw a NullReferenceException in
    // that case and the router NEVER subscribed, so choice actions like
    // GetTools silently did nothing. Start is guaranteed to run after every
    // Awake, so it retries if OnEnable couldn't.
    private void OnEnable() => Subscribe();

    private void Start() => Subscribe();

    private void OnDisable()
    {
        if (subscribedTo != null) subscribedTo.OnChoiceAction -= HandleAction;
        subscribedTo = null;
    }

    private void Subscribe()
    {
        var manager = DialogueManager.Instance;
        if (manager == null || subscribedTo == manager) return;

        manager.OnChoiceAction += HandleAction;
        subscribedTo = manager;
    }

    private void HandleAction(string actionId)
    {
        print("HANDLE OPETION");
        switch (actionId)
        {
            case "GetTools":
                print("GIVING TOOLS");
                ToolInventoryManager.Instance.AddDefaultTools();
                break;

            case "ClaimMoney":
                // TODO: wire up to your money system, e.g. MoneyManager.Instance.AddMoney(amount);
                Debug.Log("ClaimMoney action fired - hook up your money system here.");
                break;

            default:
                Debug.LogWarning($"DialogueActionRouter: unhandled actionId '{actionId}'");
                break;
        }
    }
}
