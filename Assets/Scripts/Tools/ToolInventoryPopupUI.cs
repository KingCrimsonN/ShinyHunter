using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Toggleable pop-up showing all 10 slots as draggable tiles for reordering.
/// Unlike the hotbar, slots here use DraggableToolSlot (which requires
/// ToolSlotUI on the same prefab).
/// </summary>
public class ToolInventoryPopupUI : MonoBehaviour
{
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Transform slotParent;
    [SerializeField] private DraggableToolSlot slotPrefab;
    [SerializeField] private TMPro.TMP_Text descriptionText;
    [Tooltip("Shared floating icon shown while dragging. Should be a UI Image under this popup's Canvas, Raycast Target OFF, inactive by default.")]
    [SerializeField] private Image dragIconTemplate;
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

    [Tooltip("Optional. Any DraggableToolSlot elsewhere in this UI (outside slotParent) is found automatically and driven by its own SlotIndex - list tiles here only if that discovery ever misses one.")]
    [SerializeField] private DraggableToolSlot[] equipSlots;

    /// <summary>Every tile this popup drives. Each shows the inventory slot given by its own SlotIndex - several tiles may share an index and all show it.</summary>
    private readonly List<DraggableToolSlot> tiles = new List<DraggableToolSlot>();

    private void Awake()
    {
        // Part of the persistent Overlay: a duplicate Overlay (destroyed by
        // UIManager) also runs Awake - don't let it replace the surviving
        // instance's drag icon with one that's about to be destroyed.
        if (DraggableToolSlot.DragIcon == null)
            DraggableToolSlot.DragIcon = dragIconTemplate;
        if (popupRoot != null) popupRoot.SetActive(false);
        if (dragIconTemplate != null) dragIconTemplate.gameObject.SetActive(false);
    }

    private void Start()
    {
        int capacity = ToolInventoryManager.Instance.Capacity;
        int equipCapacity = ToolInventoryManager.Instance.EquipCapacity;
        tiles.Clear();

        // Equip tiles. These used to come only from the hand-wired equipSlots
        // array, so a tile added in the editor but not added to that array
        // was never refreshed and just kept its prefab's default icon/amount.
        // Now every DraggableToolSlot in this UI outside the spawn container
        // is driven by its own SlotIndex, whether or not it's listed.
        foreach (var tile in equipSlots)
            AddTile(tile);

        foreach (var tile in GetComponentsInChildren<DraggableToolSlot>(true))
        {
            if (slotParent != null && tile.transform.IsChildOf(slotParent)) continue; // handled below
            AddTile(tile);
        }

        // Inventory tiles: use the ones already placed under slotParent (in
        // hierarchy order) before spawning any - same reasoning, a tile placed
        // by hand shouldn't be ignored.
        int nextIndex = equipCapacity;

        if (slotParent != null)
        {
            foreach (Transform child in slotParent)
            {
                if (nextIndex >= capacity) break;

                var placed = child.GetComponent<DraggableToolSlot>();
                if (placed == null) continue;

                placed.SlotIndex = nextIndex++;
                placed.descriptionText = descriptionText;
                AddTile(placed);
            }
        }

        for (; nextIndex < capacity; nextIndex++)
        {
            var slot = Instantiate(slotPrefab, slotParent);
            slot.SlotIndex = nextIndex;
            slot.descriptionText = descriptionText;
            AddTile(slot);
        }

        ToolInventoryManager.Instance.OnInventoryChanged += Refresh;
        Refresh();
    }

    private void AddTile(DraggableToolSlot tile)
    {
        if (tile == null || tiles.Contains(tile)) return;

        if (tile.descriptionText == null) tile.descriptionText = descriptionText;
        tiles.Add(tile);
    }

    private void OnDestroy()
    {
        if (ToolInventoryManager.Instance != null)
            ToolInventoryManager.Instance.OnInventoryChanged -= Refresh;
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            TogglePopup();
    }

    public void Show()
    {
        popupRoot.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        popupRoot.SetActive(false);
    }


    private void TogglePopup()
    {
        bool opening = !popupRoot.activeSelf;
        popupRoot.SetActive(opening);
        if (opening) Refresh();

        // Free the cursor while browsing, relock when closing.
        Cursor.lockState = opening ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = opening;
    }

    /// <summary>
    /// Writes every slot's contents into its tile. Runs on every inventory
    /// change, so it must never throw: a missing/unassigned tile is skipped
    /// (with a warning) instead of taking the whole inventory event down.
    /// </summary>
    private void Refresh()
    {
        var slots = ToolInventoryManager.Instance.Slots;
        foreach (var tile in tiles)
        {
            var ui = tile != null ? tile.SlotUI : null;
            if (ui == null)
            {
                Debug.LogWarning("ToolInventoryPopupUI: a slot tile has no ToolSlotUI (or was destroyed) - skipping it.", this);
                continue;
            }

            var slot = tile.SlotIndex >= 0 && tile.SlotIndex < slots.Count ? slots[tile.SlotIndex] : null;
            if (slot == null || slot.data == null || slot.count <= 0) ui.SetEmpty();
            else ui.SetItem(slot.data, slot.count);
        }
    }
}
