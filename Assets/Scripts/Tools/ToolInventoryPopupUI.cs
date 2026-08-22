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
    [Tooltip("Shared floating icon shown while dragging. Should be a UI Image under this popup's Canvas, Raycast Target OFF, inactive by default.")]
    [SerializeField] private Image dragIconTemplate;
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

    private DraggableToolSlot[] slotUIs;

    private void Awake()
    {
        DraggableToolSlot.DragIcon = dragIconTemplate;
        if (popupRoot != null) popupRoot.SetActive(false);
        if (dragIconTemplate != null) dragIconTemplate.gameObject.SetActive(false);
    }

    private void Start()
    {
        int capacity = ToolInventoryManager.Instance.Capacity;
        slotUIs = new DraggableToolSlot[capacity];

        for (int i = 0; i < capacity; i++)
        {
            var slot = Instantiate(slotPrefab, slotParent);
            slot.SlotIndex = i;
            // slot.SlotUI.SetKeybindLabel(i == capacity - 1 ? "0" : (i + 1).ToString());
            slotUIs[i] = slot;
        }

        ToolInventoryManager.Instance.OnInventoryChanged += Refresh;
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

    private void TogglePopup()
    {
        bool opening = !popupRoot.activeSelf;
        popupRoot.SetActive(opening);
        if (opening) Refresh();

        // Free the cursor while browsing, relock when closing.
        Cursor.lockState = opening ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = opening;
    }

    private void Refresh()
    {
        var slots = ToolInventoryManager.Instance.Slots;
        for (int i = 0; i < slotUIs.Length; i++)
        {
            var slot = slots[i];
            var ui = slotUIs[i].SlotUI;
            if (slot == null || slot.data == null) ui.SetEmpty();
            else ui.SetItem(slot.data, slot.count);
        }
    }
}
