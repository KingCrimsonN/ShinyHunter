using UnityEngine;

/// <summary>
/// Always-on-screen overlay showing all 10 slots and which one is equipped.
/// Read-only - reordering happens in ToolInventoryPopupUI instead.
/// </summary>
public class ToolHotbarUI : MonoBehaviour
{
    [SerializeField] private Transform slotParent;
    [SerializeField] private ToolSlotUI slotPrefab;

    private ToolSlotUI[] slotUIs;

    private void Start()
    {
        int capacity = ToolInventoryManager.Instance.Capacity;
        int equipCapacity = ToolInventoryManager.Instance.EquipCapacity;
        slotUIs = new ToolSlotUI[equipCapacity];

        for (int i = 0; i < equipCapacity; i++)
        {
            var slotUI = Instantiate(slotPrefab, slotParent);
            slotUI.SetKeybindLabel(i == capacity - 1 ? "0" : (i + 1).ToString());
            slotUIs[i] = slotUI;
        }

        ToolInventoryManager.Instance.OnInventoryChanged += Refresh;
        ToolInventoryManager.Instance.OnEquippedChanged += HighlightEquipped;

        Refresh();
        HighlightEquipped(ToolInventoryManager.Instance.EquippedIndex);
    }

    private void OnDestroy()
    {
        if (ToolInventoryManager.Instance != null)
        {
            ToolInventoryManager.Instance.OnInventoryChanged -= Refresh;
            ToolInventoryManager.Instance.OnEquippedChanged -= HighlightEquipped;
        }
    }

    private void Refresh()
    {
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
        for (int i = 0; i < slotUIs.Length; i++)
            slotUIs[i].SetSelected(i == index);
    }
}
