using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One tile, shown on EITHER side of the transform station (inventory or
/// selection) - same prefab, same script, just configured with a different
/// TransformEntrySide. It shows a CritterStack: ONE critter in individual
/// view (the default - no count, its own sparkle), or a whole species+rarity
/// group in grouped view ("xN", sparkle if any in it is double-yield).
/// Handles all three move interactions:
///   - Drag: moves everything on this tile (just the one critter, individually).
///   - Double-click: moves exactly one.
///   - Shift+click: moves everything on this tile (same as drag, click-triggered).
/// Also acts as its own drop target, so dropping onto an existing entry on
/// the opposite side works - see TransformDropZoneUI for the empty-space case.
/// </summary>
public class CreatureTransformEntryUI : MonoBehaviour,
    IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private Image frame;
    [SerializeField] private TMP_Text nameText;
    // [SerializeField] private TMP_Text rarityText;
    [SerializeField] private TMP_Text countText;
    [Tooltip("Shown when this critter yields double resources (or, for a grouped tile, when any in the group does) - flagged at capture time. E.g. a sparkle graphic layered on top of the icon.")]
    [SerializeField] private GameObject ExtraIngredientOverlay;

    /// <summary>Shared floating drag ghost, set once by CreatureTransformStationUI.Awake().</summary>
    public static Image DragIcon;

    public TransformEntrySide Side { get; private set; }

    private CritterStack stack;
    private CreatureData.Rarity rarity;
    private CreatureTransformStationUI station;


    // [SerializeField] private Sprite[] rarityFrames; // normal, uncommon, rare, legendary
    [SerializeField] private CritterFrames critterFrames;

    public void Setup(CritterStack stack, TransformEntrySide side, CreatureTransformStationUI station)
    {
        this.stack = stack;
        this.rarity = stack.Rarity;
        this.Side = side;
        this.station = station;

        var species = stack.Species;
        if (icon != null) icon.sprite = species.GetIcon(rarity);
        if (nameText != null) nameText.text = species.creatureName;
        // Individual tiles are always exactly one critter - no "x1" on every tile.
        if (countText != null) countText.text = stack.IsGroup ? "x" + stack.Count : string.Empty;

        if (frame != null && critterFrames != null) frame.sprite = critterFrames.rarityFrames[(int)rarity];

        // The stack only holds the critters on THIS side (the station builds
        // each side's tiles separately), so this is exactly what's shown here.
        if (ExtraIngredientOverlay != null) ExtraIngredientOverlay.SetActive(stack.DoubleYieldCount > 0);
    }

    /// <summary>Moves up to `amount` from this entry's side to the other side. Station clamps to what's actually available.</summary>
    public void MoveAmount(int amount)
    {
        if (DragIcon != null)
            DragIcon.gameObject.SetActive(false);
        if (station == null || stack == null) return;

        if (Side == TransformEntrySide.Inventory)
            station.MoveToSelection(stack, amount);
        else
            station.MoveFromSelection(stack, amount);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (eventData.clickCount >= 2)
            MoveAmount(1); // double-click - exactly one
        else if (shiftHeld)
            MoveAmount(int.MaxValue); // shift+click - whole stack (clamped by the station)
        else if (station != null && stack != null)
            station.ShowIngredientPopup(stack, (RectTransform)transform); // plain click - what would this critter give? (the first click of a double-click lands here too; the move that follows closes the pop-up again)
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

    public void OnPointerEnter(PointerEventData eventData)
    {
        frame.sprite = critterFrames.selectedFrames[(int)rarity];
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        frame.sprite = critterFrames.rarityFrames[(int)rarity];
    }
}
