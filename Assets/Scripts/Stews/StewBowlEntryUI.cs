using UnityEngine;

/// <summary>
/// One of the stew inventory's constant bowl slots, in one of three states:
/// filled (shows the stew through the shared StewDetailsUI), vacant (usable but
/// empty) or locked (not unlocked yet). Display only.
/// </summary>
public class StewBowlEntryUI : MonoBehaviour
{
    [Tooltip("The shared stew read-out. Shown only while the slot is filled.")]
    [SerializeField] private StewDetailsUI details;
    [Tooltip("Optional - what to show while filled, if more than the Details object itself (e.g. the slot's filled frame). Empty = the Details object.")]
    [SerializeField] private GameObject filledState;
    [Tooltip("Shown while the slot is empty but usable.")]
    [SerializeField] private GameObject vacantState;
    [Tooltip("Shown while the slot is not unlocked yet.")]
    [SerializeField] private GameObject lockedState;

    public void SetFilled(StewInstance stew)
    {
        SetStates(filled: true, vacant: false, locked: false);
        if (details != null) details.Show(stew);
    }

    public void SetVacant() => SetStates(filled: false, vacant: true, locked: false);

    public void SetLocked() => SetStates(filled: false, vacant: false, locked: true);

    private void SetStates(bool filled, bool vacant, bool locked)
    {
        GameObject filledObject = filledState != null ? filledState : details != null ? details.gameObject : null;
        if (filledObject != null) filledObject.SetActive(filled);
        if (vacantState != null) vacantState.SetActive(vacant);
        if (lockedState != null) lockedState.SetActive(locked);
    }
}
