using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Drag-and-drop reordering for one popup slot. Requires ToolSlotUI on the
/// same object, and a Canvas with a GraphicRaycaster + an EventSystem in
/// the scene (Unity adds both automatically when you create a Canvas via
/// GameObject > UI > Canvas).
///
/// DragIcon is a single shared "ghost" image set once by ToolInventoryPopupUI -
/// every slot drags the same floating icon rather than each needing its own.
/// </summary>
[RequireComponent(typeof(ToolSlotUI))]
public class DraggableToolSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    /// <summary>Set once by ToolInventoryPopupUI.Awake().</summary>
    public static Image DragIcon;

    private static DraggableToolSlot draggedFrom;

    public TMPro.TMP_Text descriptionText;

    [SerializeField] public int SlotIndex;

    [SerializeField] private GameObject highlight;

    private ToolSlotUI slotUI;

    /// <summary>
    /// Resolved on first use rather than in Awake: a slot that starts INACTIVE
    /// in the hierarchy (e.g. equip slots under a hidden panel) never gets an
    /// Awake, but the popup still needs to write its contents into it on every
    /// inventory change.
    /// </summary>
    public ToolSlotUI SlotUI
    {
        get
        {
            if (slotUI == null) slotUI = GetComponent<ToolSlotUI>();
            return slotUI;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!SlotUI.HasItem || DragIcon == null) return;

        draggedFrom = this;
        DragIcon.sprite = SlotUI.CurrentIcon;
        DragIcon.gameObject.SetActive(true);
        DragIcon.transform.position = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (draggedFrom != this || DragIcon == null) return;
        DragIcon.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (DragIcon != null) DragIcon.gameObject.SetActive(false);
        draggedFrom = null;
    }

    public void SetDescriptionText(TMPro.TMP_Text text)
    {
        descriptionText = text;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (draggedFrom == null || draggedFrom == this) return;
        ToolInventoryManager.Instance.SwapSlots(draggedFrom.SlotIndex, SlotIndex);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (descriptionText != null && SlotUI.HasItem)
        {
            descriptionText.text = ToolInventoryManager.Instance.GetToolDescription(SlotIndex);
            if (highlight != null)
                highlight.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (descriptionText != null)
        {
            descriptionText.text = string.Empty;
            if (highlight != null)
                highlight.SetActive(false);
        }
    }
}
