using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Fallback drop target covering an entire grid panel (inventory side or
/// selection side), so dropping into EMPTY space still works - individual
/// CreatureTransformEntryUI instances only exist where there's already an
/// entry, so a grid with nothing in it needs something else to catch drops.
/// Put this on the background Image of each side's grid/scroll panel.
/// </summary>
public class TransformDropZoneUI : MonoBehaviour, IDropHandler
{
    [SerializeField] private TransformEntrySide side;

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        var draggedEntry = eventData.pointerDrag.GetComponent<CreatureTransformEntryUI>();
        if (draggedEntry == null || draggedEntry.Side == side) return; // dropped back on its own side - no-op

        draggedEntry.MoveAmount(int.MaxValue);
    }
}
