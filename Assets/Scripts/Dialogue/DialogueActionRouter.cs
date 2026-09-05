using UnityEngine;

/// <summary>
/// Template for dispatching dialogue choice actionIds to actual scene
/// functionality. Scene-specific (not persistent) - place one wherever a
/// dialogue in that scene has choices with custom effects. Add a case per
/// actionId string used in your DialogueChoice entries.
/// </summary>
public class DialogueActionRouter : MonoBehaviour
{
    private void OnEnable()
    {
        DialogueManager.Instance.OnChoiceAction += HandleAction;
    }

    private void OnDisable()
    {
        if (DialogueManager.Instance != null)
            DialogueManager.Instance.OnChoiceAction -= HandleAction;
    }

    private void HandleAction(string actionId)
    {
        print("HANDLE OPETION");
        switch (actionId)
        {
            case "OpenTransformStation":
                print("OPENING TRANSFORM");
                CreatureTransformStationUI.Instance.Open();
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
