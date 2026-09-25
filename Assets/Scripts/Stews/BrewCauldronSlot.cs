using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>One of the 8 cauldron slots - drop target for ingredients, click to remove.</summary>
public class BrewCauldronSlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private Button closeButton;

    private int slotIndex;
    private BrewingStationUI station;

    public void Setup(int slotIndex, BrewingStationUI station)
    {
        this.slotIndex = slotIndex;
        this.station = station;
        closeButton.onClick.AddListener(Close);
        closeButton.gameObject.SetActive(false);
    }

    public void Display(ResourceData resource, bool unlocked)
    {
        if (lockedOverlay != null) lockedOverlay.SetActive(!unlocked);

        if (icon != null)
        {
            icon.sprite = resource != null ? resource.icon : null;
            icon.enabled = resource != null;
        }
        if (nameText != null) nameText.text = resource != null ? resource.resourceName : string.Empty;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        var draggedEntry = eventData.pointerDrag.GetComponent<BrewIngredientEntryUI>();
        if (draggedEntry == null) return;

        station.PlaceIngredient(draggedEntry.Resource, slotIndex);
        BrewIngredientEntryUI.DragIcon.gameObject.SetActive(false);
        closeButton.gameObject.SetActive(true);
    }

    private void Close()
    {
        station.ClearSlot(slotIndex);
        closeButton.gameObject.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // station.ClearSlot(slotIndex); // click a filled slot to take the ingredient back out

    }
}