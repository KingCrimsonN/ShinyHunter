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

    [SerializeField] private DraggableToolSlot[] slotUIs;
    [SerializeField] private DraggableToolSlot[] equipSlots;

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
        slotUIs = new DraggableToolSlot[capacity];

        for (int i = 0; i < equipCapacity; i++)
        {
            slotUIs[i] = equipSlots[i];
        }

        for (int i = equipCapacity; i < capacity; i++)
        {
            var slot = Instantiate(slotPrefab, slotParent);
            slot.SlotIndex = i;
            slot.descriptionText = descriptionText;
            // slot.SlotUI.SetKeybindLabel(i == capacity - 1 ? "0" : (i + 1).ToString());
            slotUIs[i] = slot;
        }

        ToolInventoryManager.Instance.OnInventoryChanged += Refresh;
        Refresh();
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
        for (int i = 0; i < slotUIs.Length && i < slots.Count; i++)
        {
            var ui = slotUIs[i] != null ? slotUIs[i].SlotUI : null;
            if (ui == null)
            {
                Debug.LogWarning($"ToolInventoryPopupUI: slot tile {i} is missing (unassigned in equipSlots, or has no ToolSlotUI) - skipping it.", this);
                continue;
            }

            var slot = slots[i];
            if (slot == null || slot.data == null || slot.count <= 0) ui.SetEmpty();
            else ui.SetItem(slot.data, slot.count);
        }
    }
}
