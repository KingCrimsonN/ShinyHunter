using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sub-panel opened from the brewing UI to browse the stew (bowl) inventory.
/// Always shows StewInventoryManager.MaxSlots slots in the grid: the first
/// Count are filled with a stew, the rest of the usable ones are vacant, and the
/// ones beyond the unlocked capacity are locked.
/// </summary>
public class StewInventoryPanelUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Transform gridParent;
    [Tooltip("The slot prefab - one is spawned per slot (6), each with filled / vacant / locked states.")]
    [SerializeField] private StewBowlEntryUI entryPrefab;
    [SerializeField] private Button closeButton;

    private readonly List<StewBowlEntryUI> spawned = new List<StewBowlEntryUI>();

    private void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
    }

    public void Show()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void Refresh()
    {
        // Anything left in the grid at design time is only a preview of the look.
        foreach (Transform child in gridParent)
            Destroy(child.gameObject);
        spawned.Clear();

        var manager = StewInventoryManager.Instance;
        var bowls = manager.Bowls;

        for (int i = 0; i < manager.MaxSlots; i++)
        {
            var slot = Instantiate(entryPrefab, gridParent);
            slot.gameObject.SetActive(true);

            if (i < bowls.Count) slot.SetFilled(bowls[i]);
            else if (i < manager.Capacity) slot.SetVacant();
            else slot.SetLocked();

            spawned.Add(slot);
        }
    }
}
