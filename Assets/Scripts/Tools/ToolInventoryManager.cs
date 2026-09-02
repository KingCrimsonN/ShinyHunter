using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fixed 10-slot inventory for tools/active items. Slot index maps directly
/// to hotbar keybind (0-8 -> keys 1-9, slot 9 -> key 0).
///
/// The "equipped" pointer is a slot INDEX, not an item reference - dragging
/// items around in the popup reorders slots, and whichever item ends up in
/// the currently-selected slot number is what's equipped (same convention
/// most survival/hotbar games use).
/// </summary>
public class ToolInventoryManager : MonoBehaviour
{
    public static ToolInventoryManager Instance { get; private set; }

    [Serializable]
    public class ToolSlot
    {
        public ToolData data;
        public int count;
    }

    [SerializeField] private int capacity = 25;
    [SerializeField] private int equipCapacity = 3;

    [Header("Starting Loadout (optional)")]
    [Tooltip("Filled into slots 0, 1, 2... at game start, useful for testing.")]
    [SerializeField] private List<ToolData> startingTools;

    [Header("Debug (read-only at runtime)")]
    [SerializeField] private ToolSlot[] slots;

    private int equippedIndex = 0;

    public int Capacity => capacity;
    public int EquipCapacity => equipCapacity;
    public int EquippedIndex => equippedIndex;
    public IReadOnlyList<ToolSlot> Slots => slots;
    public ToolSlot EquippedSlot => IsValidIndex(equippedIndex) ? slots[equippedIndex] : null;

    /// <summary>Fires whenever slot contents change (add/remove/swap).</summary>
    public event Action OnInventoryChanged;
    /// <summary>Fires with the new index whenever the equipped slot changes.</summary>
    public event Action<int> OnEquippedChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        slots = new ToolSlot[capacity];
        for (int i = 0; i < capacity; i++)
            slots[i] = new ToolSlot();

        if (startingTools != null)
        {
            foreach (var tool in startingTools)
                AddTool(tool, 1);
        }
    }

    /// <summary>
    /// Adds up to `amount` of a tool: fills existing stacks first, then
    /// empty slots. Returns the leftover amount that didn't fit (0 if it
    /// all fit) so calling code can decide what to do with overflow.
    /// </summary>
    public int AddTool(ToolData data, int amount = 1)
    {
        if (data == null || amount <= 0) return amount;

        for (int i = 0; i < slots.Length && amount > 0; i++)
        {
            if (slots[i].data == data && slots[i].count < data.maxStack)
            {
                int space = data.maxStack - slots[i].count;
                int toAdd = Mathf.Min(space, amount);
                slots[i].count += toAdd;
                amount -= toAdd;
            }
        }

        for (int i = 0; i < slots.Length && amount > 0; i++)
        {
            if (slots[i].data == null)
            {
                int toAdd = Mathf.Min(data.maxStack, amount);
                slots[i].data = data;
                slots[i].count = toAdd;
                amount -= toAdd;
            }
        }

        OnInventoryChanged?.Invoke();
        return amount;
    }

    public void RemoveFromSlot(int index, int amount = 1)
    {
        if (!IsValidIndex(index) || slots[index].data == null) return;

        slots[index].count -= amount;
        if (slots[index].count <= 0)
        {
            slots[index].data = null;
            slots[index].count = 0;
        }

        OnInventoryChanged?.Invoke();
    }

    /// <summary>Swaps the contents of two slots (used by popup drag-and-drop reordering).</summary>
    public void SwapSlots(int a, int b)
    {
        if (!IsValidIndex(a) || !IsValidIndex(b) || a == b) return;

        (slots[a], slots[b]) = (slots[b], slots[a]);
        OnInventoryChanged?.Invoke();
    }

    public void SetEquippedIndex(int index)
    {
        if (!IsValidIndex(index) || index == equippedIndex) return;
        equippedIndex = index;
        OnEquippedChanged?.Invoke(equippedIndex);
    }

    /// <summary>direction: +1 for next slot, -1 for previous, wraps around.</summary>
    public void CycleEquipped(int direction)
    {
        if (equipCapacity == 0) return;
        int next = ((equippedIndex + direction) % equipCapacity + equipCapacity) % equipCapacity;
        SetEquippedIndex(next);
    }

    public string GetToolDescription(int index)
    {
        if (!IsValidIndex(index) || slots[index].data == null) return string.Empty;
        return slots[index].data.description;
    }

    /// <summary>
    /// The "Sort" button's action for a fixed-slot inventory: gather the
    /// filled slots, sort them (here: alphabetically by name), and rewrite
    /// the array so sorted items sit at the front and empties collapse to
    /// the end. Equip index is NOT touched, so whatever ends up in the
    /// currently-selected slot number becomes what's equipped (same rule as
    /// SwapSlots - see its comment).
    /// </summary>
    public void SortSlots()
    {
        var filled = new List<ToolSlot>();
        foreach (var slot in slots)
            if (slot.data != null) filled.Add(slot);

        filled.Sort((a, b) => string.Compare(a.data.toolName, b.data.toolName, StringComparison.OrdinalIgnoreCase));

        for (int i = 0; i < slots.Length; i++)
        {
            if (i < filled.Count)
            {
                slots[i].data = filled[i].data;
                slots[i].count = filled[i].count;
            }
            else
            {
                slots[i].data = null;
                slots[i].count = 0;
            }
        }

        OnInventoryChanged?.Invoke();
    }

    private bool IsValidIndex(int index) => slots != null && index >= 0 && index < slots.Length;
}
