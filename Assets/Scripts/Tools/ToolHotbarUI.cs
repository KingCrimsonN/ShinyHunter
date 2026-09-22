using UnityEngine;

/// <summary>
/// Always-on-screen overlay showing all 10 slots and which one is equipped.
/// Read-only - reordering happens in ToolInventoryPopupUI instead.
///
/// Part of the persistent Overlay: UIManager hides this whole object in the
/// hub and shows it on expeditions. Slots are built once (Start, on first
/// enable); event subscriptions follow OnEnable/OnDisable so a hidden
/// hotbar doesn't do work, and it re-syncs whenever it's shown again.
/// </summary>
public class ToolHotbarUI : MonoBehaviour
{
    [SerializeField] private Transform slotParent;
    [SerializeField] private ToolSlotUI slotPrefab;

    private ToolSlotUI[] slotUIs;

    private void OnEnable()
    {
        Subscribe();
        if (slotUIs != null) SyncAll(); // shown again after being hidden - inventory may have changed meanwhile
    }

    private void Start()
    {
        int capacity = ToolInventoryManager.Instance.Capacity;
        int equipCapacity = ToolInventoryManager.Instance.EquipCapacity;
        slotUIs = new ToolSlotUI[equipCapacity];

        // Use the tiles already placed under slotParent (in hierarchy order)
        // first. Spawning a full set on top of them left the placed ones
        // untouched - stuck on the prefab's default icon/amount - next to a
        // duplicate set that actually worked.
        int filled = 0;
        foreach (Transform child in slotParent)
        {
            if (filled >= equipCapacity) break;

            var placed = child.GetComponent<ToolSlotUI>();
            if (placed != null) slotUIs[filled++] = placed;
        }

        for (; filled < equipCapacity; filled++)
            slotUIs[filled] = Instantiate(slotPrefab, slotParent);

        for (int i = 0; i < equipCapacity; i++)
            slotUIs[i].SetKeybindLabel(i == capacity - 1 ? "0" : (i + 1).ToString());

        Subscribe(); // OnEnable can run before the manager exists in the very first scene
        SyncAll();
    }

    private void OnDisable()
    {
        if (ToolInventoryManager.Instance != null)
        {
            ToolInventoryManager.Instance.OnInventoryChanged -= Refresh;
            ToolInventoryManager.Instance.OnEquippedChanged -= HighlightEquipped;
        }
    }

    private void Subscribe()
    {
        var tools = ToolInventoryManager.Instance;
        if (tools == null) return;

        // -= first so calling this from both OnEnable and Start never double-subscribes
        tools.OnInventoryChanged -= Refresh;
        tools.OnInventoryChanged += Refresh;
        tools.OnEquippedChanged -= HighlightEquipped;
        tools.OnEquippedChanged += HighlightEquipped;
    }

    private void SyncAll()
    {
        Refresh();
        HighlightEquipped(ToolInventoryManager.Instance.EquippedIndex);
    }

    private void Refresh()
    {
        if (slotUIs == null) return;

        var slots = ToolInventoryManager.Instance.Slots;
        for (int i = 0; i < slotUIs.Length; i++)
        {
            var slot = slots[i];
            if (slot == null || slot.data == null) slotUIs[i].SetEmpty();
            else slotUIs[i].SetItem(slot.data, slot.count);
        }
    }

    private void HighlightEquipped(int index)
    {
        if (slotUIs == null) return;

        for (int i = 0; i < slotUIs.Length; i++)
            slotUIs[i].SetSelected(i == index);
    }
}
