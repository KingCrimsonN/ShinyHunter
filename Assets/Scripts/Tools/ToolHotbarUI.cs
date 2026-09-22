using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Carousel-style hotbar for the 3 equip slots: a large centered card for the
/// currently equipped tool, flanked by two small cards for the other two.
/// Read-only - reordering which tool sits in which equip slot happens in
/// ToolInventoryPopupUI instead; this only ever reacts to ToolInventoryManager.
///
/// Assumes exactly 3 equip slots (ToolInventoryManager.EquipCapacity) - that's
/// the whole premise of a fixed left/center/right layout. If EquipCapacity is
/// ever changed away from 3, the rotation animation has no defined shape for
/// that and falls back to a plain snap (see HandleEquippedChanged).
///
/// Selection-change "animation": since there's no new item sliding in from
/// outside (only ever 3 tools total), a change is a ROTATION of the three
/// already-visible icons between the three fixed card positions - not a
/// sliding track like ExpeditionStewSelectionUI's carousel. Moving to the
/// NEXT slot: each icon moves one card to the left, and the vacated right
/// card is filled by whatever just fell off the left (wraps around);
/// PREVIOUS is the mirror. On top of that, the center card's frame briefly
/// flashes to an alternate sprite as a "this is now equipped" cue.
///
/// Part of the persistent Overlay: UIManager hides this whole object in the
/// hub. Event subscriptions follow OnEnable/OnDisable/Start like the rest of
/// the Overlay's components (see CLAUDE.md convention #3).
/// </summary>
public class ToolHotbarUI : MonoBehaviour
{
    [Header("Cards")]
    [Tooltip("The big, centered card - the currently equipped/held tool.")]
    [SerializeField] private ToolSlotUI centerCard;
    [Tooltip("The small card to the left of center.")]
    [SerializeField] private ToolSlotUI leftCard;
    [Tooltip("The small card to the right of center.")]
    [SerializeField] private ToolSlotUI rightCard;

    [Header("Center Frame Flash")]
    [Tooltip("The center card's frame border Image (e.g. item_slot) - briefly swapped to the flash sprite on every selection change.")]
    [SerializeField] private Image centerFrame;
    [Tooltip("Shown for flashDuration seconds on every selection change (e.g. item_slot_change). Left empty = no flash.")]
    [SerializeField] private Sprite centerFrameFlashSprite;
    [SerializeField] private float flashDuration = 0.15f;

    /// <summary>The frame's own sprite at startup (e.g. item_slot), restored after each flash - captured automatically so it doesn't need to be assigned twice.</summary>
    private Sprite centerFrameNormalSprite;

    // Which equip-slot index each fixed screen position is currently showing.
    private int leftIndex, centerIndexShown, rightIndex;
    private int cachedEquippedIndex;
    private bool initialized;

    private Coroutine flashRoutine;

    private void Awake()
    {
        if (centerFrame != null) centerFrameNormalSprite = centerFrame.sprite;
    }

    private void OnEnable()
    {
        Subscribe();

        // Shown again after being hidden (e.g. returning from the hub) -
        // resync without animating or flashing; the player didn't watch
        // anything change while the hotbar was off-screen.
        if (initialized && ToolInventoryManager.Instance != null)
            SyncPositions(ToolInventoryManager.Instance.EquippedIndex);
    }

    private void Start()
    {
        Subscribe(); // OnEnable can run before the manager exists in the very first scene

        if (ToolInventoryManager.Instance != null)
            SyncPositions(ToolInventoryManager.Instance.EquippedIndex);
    }

    private void OnDisable()
    {
        if (ToolInventoryManager.Instance != null)
        {
            ToolInventoryManager.Instance.OnInventoryChanged -= RefreshContent;
            ToolInventoryManager.Instance.OnEquippedChanged -= HandleEquippedChanged;
        }
    }

    private void Subscribe()
    {
        var tools = ToolInventoryManager.Instance;
        if (tools == null) return;

        // -= first so calling this from both OnEnable and Start never double-subscribes
        tools.OnInventoryChanged -= RefreshContent;
        tools.OnInventoryChanged += RefreshContent;
        tools.OnEquippedChanged -= HandleEquippedChanged;
        tools.OnEquippedChanged += HandleEquippedChanged;
    }

    /// <summary>Snaps all three positions to match equippedIndex - no rotation, no flash. Used on first sync and whenever the hotbar is (re)shown.</summary>
    private void SyncPositions(int equippedIndex)
    {
        int cap = ToolInventoryManager.Instance.EquipCapacity;
        if (cap <= 0) return;

        cachedEquippedIndex = equippedIndex;
        centerIndexShown = equippedIndex;
        leftIndex = ((equippedIndex - 1) % cap + cap) % cap;
        rightIndex = (equippedIndex + 1) % cap;
        initialized = true;

        RefreshContent();
        ResetCenterFrame();
    }

    private void HandleEquippedChanged(int newIndex)
    {
        if (!initialized)
        {
            SyncPositions(newIndex);
            return;
        }

        int cap = ToolInventoryManager.Instance.EquipCapacity;
        int delta = ((newIndex - cachedEquippedIndex) % cap + cap) % cap;
        cachedEquippedIndex = newIndex;

        if (cap == 3 && delta == 1) RotateNext();
        else if (cap == 3 && delta == 2) RotatePrevious();
        else SyncPositions(newIndex); // EquipCapacity isn't 3 - no rotation shape defined, just snap

        PlayFlash();
    }

    /// <summary>Moved to the NEXT slot: icons shift one card to the left, wrapping the vacated right card in from the left.</summary>
    private void RotateNext()
    {
        (leftIndex, centerIndexShown, rightIndex) = (centerIndexShown, rightIndex, leftIndex);
        RefreshContent();
    }

    /// <summary>Moved to the PREVIOUS slot - the mirror of RotateNext.</summary>
    private void RotatePrevious()
    {
        (leftIndex, centerIndexShown, rightIndex) = (rightIndex, leftIndex, centerIndexShown);
        RefreshContent();
    }

    private void RefreshContent()
    {
        if (!initialized) return;

        var slots = ToolInventoryManager.Instance.Slots;
        SetCard(leftCard, leftIndex, slots);
        SetCard(centerCard, centerIndexShown, slots);
        SetCard(rightCard, rightIndex, slots);
    }

    private static void SetCard(ToolSlotUI card, int index, IReadOnlyList<ToolInventoryManager.ToolSlot> slots)
    {
        if (card == null || index < 0 || index >= slots.Count) return;

        var slot = slots[index];
        if (slot == null || slot.data == null) card.SetEmpty();
        else card.SetItem(slot.data, slot.count);
    }

    private void PlayFlash()
    {
        if (centerFrame == null || centerFrameFlashSprite == null) return;

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        centerFrame.sprite = centerFrameFlashSprite;
        yield return new WaitForSeconds(flashDuration);
        ResetCenterFrame();
        flashRoutine = null;
    }

    private void ResetCenterFrame()
    {
        if (flashRoutine != null) { StopCoroutine(flashRoutine); flashRoutine = null; }
        if (centerFrame != null) centerFrame.sprite = centerFrameNormalSprite;
    }
}
