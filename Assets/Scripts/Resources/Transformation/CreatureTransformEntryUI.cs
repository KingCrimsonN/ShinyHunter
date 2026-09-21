using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One entry, shown on EITHER side of the transform station (inventory or
/// selection) - same prefab, same script, just configured with a different
/// TransformEntrySide. Handles all three move interactions:
///   - Drag: moves the WHOLE stack shown on this entry.
///   - Double-click: moves exactly one.
///   - Shift+click: moves the whole stack (same result as drag, click-triggered).
/// Also acts as its own drop target, so dropping onto an existing entry on
/// the opposite side works - see TransformDropZoneUI for the empty-space case.
/// </summary>
public class CreatureTransformEntryUI : MonoBehaviour,
    IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    // [SerializeField] private TMP_Text rarityText;
    [SerializeField] private TMP_Text countText;

    /// <summary>Shared floating drag ghost, set once by CreatureTransformStationUI.Awake().</summary>
    public static Image DragIcon;

    public TransformEntrySide Side { get; private set; }

    private CreatureData species;
    private CreatureData.Rarity rarity;
    private CreatureTransformStationUI station;

    public void Setup(CreatureData species, CreatureData.Rarity rarity, int count, TransformEntrySide side, CreatureTransformStationUI station)
    {
        this.species = species;
        this.rarity = rarity;
        this.Side = side;
        this.station = station;

        if (icon != null) icon.sprite = species.GetIcon(rarity);
        if (nameText != null) nameText.text = species.creatureName;
        // if (rarityText != null) rarityText.text = rarity.ToString();
        if (countText != null) countText.text = "x" + count;
    }

    /// <summary>Moves up to `amount` from this entry's side to the other side. Station clamps to what's actually available.</summary>
    public void MoveAmount(int amount)
    {
        if (DragIcon != null)
            DragIcon.gameObject.SetActive(false);
        if (station == null) return;

        if (Side == TransformEntrySide.Inventory)
            station.MoveToSelection(species, rarity, amount);
        else
            station.MoveFromSelection(species, rarity, amount);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (eventData.clickCount >= 2)
            MoveAmount(1); // double-click - exactly one
        else if (shiftHeld)
            MoveAmount(int.MaxValue); // shift+click - whole stack (clamped by the station)
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (DragIcon == null || icon == null || icon.sprite == null) return;

        DragIcon.sprite = icon.sprite;
        DragIcon.gameObject.SetActive(true);
        DragIcon.transform.position = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (DragIcon != null && DragIcon.gameObject.activeSelf)
            DragIcon.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (DragIcon != null)
            DragIcon.gameObject.SetActive(false);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (DragIcon != null)
            DragIcon.gameObject.SetActive(false);
        if (eventData.pointerDrag == null) return;

        var draggedEntry = eventData.pointerDrag.GetComponent<CreatureTransformEntryUI>();
        if (draggedEntry == null || draggedEntry.Side == Side) return; // dropped back on its own side - no-op

        draggedEntry.MoveAmount(int.MaxValue); // drag = whole stack
    }
}
