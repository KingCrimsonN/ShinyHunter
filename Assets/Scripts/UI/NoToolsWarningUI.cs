using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "You have no capture tools" confirmation, shown by Doors.Interact() when
/// the player tries to leave the hub with nothing in their tool inventory
/// (the stick is always available regardless - this only checks purchased/
/// found ToolData items, see Doors.HasAnyTools). Yes proceeds with whatever
/// action the door wanted to take anyway (normally opening the stew-selection
/// carousel); No just closes the warning, leaving the player in the hub free
/// to go buy something first.
///
/// Scene-scoped (Hub only) - not persistent, same convention as the other
/// Hub popups (BrewingStationUI, CreatureTransformStationUI, Shop).
/// </summary>
public class NoToolsWarningUI : MonoBehaviour
{
    public static NoToolsWarningUI Instance { get; private set; }

    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    private Action onConfirmLeaveAnyway;

    private void Awake()
    {
        Instance = this;
        if (popupRoot != null) popupRoot.SetActive(false);

        if (yesButton != null) yesButton.onClick.AddListener(OnYes);
        if (noButton != null) noButton.onClick.AddListener(OnNo);
    }

    /// <param name="onConfirmLeaveAnyway">Invoked if the player picks Yes - whatever the door would normally have done.</param>
    public void Open(Action onConfirmLeaveAnyway)
    {
        this.onConfirmLeaveAnyway = onConfirmLeaveAnyway;

        if (popupRoot != null) popupRoot.SetActive(true);
        PlayerStateManager.Instance.Freeze();
    }

    private void OnYes()
    {
        var callback = onConfirmLeaveAnyway;
        Close();
        callback?.Invoke();
    }

    private void OnNo()
    {
        Close();
    }

    private void Close()
    {
        if (popupRoot != null) popupRoot.SetActive(false);
        PlayerStateManager.Instance.Unfreeze();
        onConfirmLeaveAnyway = null;
    }
}
