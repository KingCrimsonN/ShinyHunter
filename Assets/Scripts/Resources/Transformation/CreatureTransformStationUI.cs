using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Creature-to-resource transform station. Opened externally (call
/// Instance.Open() from your interactable object's script - no toggle key
/// of its own). Holds a local, transient "selection" of creatures staged
/// for transformation; nothing is actually consumed until CompleteTransform().
///
/// Two-phase transform, to leave room for your animation:
///   1. BeginTransform() - validates selection, snapshots it, closes the
///      popup, fires OnTransformInitiated(snapshot). Nothing is consumed yet.
///   2. CompleteTransform() - call this once your animation finishes. THIS
///      is what actually removes the creatures from InventoryManager and
///      grants resources via ResourceInventoryManager.
/// </summary>
public class CreatureTransformStationUI : MonoBehaviour
{
    public static CreatureTransformStationUI Instance { get; private set; }

    [Header("Popup")]
    [SerializeField] private GameObject popupRoot;

    [Header("Grids")]
    [SerializeField] private CreatureTransformEntryUI entryPrefab;
    [Tooltip("Parent for the source-side (owned creatures) entries.")]
    [SerializeField] private Transform inventoryGridParent;
    [Tooltip("Parent for the destination-side (staged for transform) entries.")]
    [SerializeField] private Transform selectionGridParent;
    [Tooltip("Shared floating icon shown while dragging. UI Image under this popup's Canvas, Raycast Target OFF, inactive by default.")]
    [SerializeField] private Image dragIconTemplate;

    private readonly Dictionary<(CreatureData species, CreatureData.Rarity rarity), int> selection =
        new Dictionary<(CreatureData, CreatureData.Rarity), int>();

    public event Action OnSelectionChanged;
    /// <summary>Fired when Transform is pressed, with a snapshot of what was staged. Start your animation here.</summary>
    public event Action<IReadOnlyDictionary<(CreatureData species, CreatureData.Rarity rarity), int>> OnTransformInitiated;
    /// <summary>Fired once CompleteTransform() has actually granted resources.</summary>
    public event Action OnTransformCompleted;

    private void Awake()
    {
        Instance = this;

        CreatureTransformEntryUI.DragIcon = dragIconTemplate;
        if (dragIconTemplate != null) dragIconTemplate.gameObject.SetActive(false);
        if (popupRoot != null) popupRoot.SetActive(false);
    }

    // ---------------- Open / Close ----------------

    public void Open()
    {
        selection.Clear(); // starts fresh each time - remove this line if you'd rather staged items persist between opens

        if (popupRoot != null) popupRoot.SetActive(true);
        // Freezes movement, Interactor, the stick and tool use together
        // (and frees the cursor) - see PlayerStateManager.
        PlayerStateManager.Instance.Freeze();

        // -= first so an Open() without a matching Close() can't double-subscribe
        InventoryManager.Instance.OnInventoryChanged -= RefreshGrids;
        InventoryManager.Instance.OnInventoryChanged += RefreshGrids;
        RefreshGrids();
    }

    public void Close()
    {
        if (popupRoot != null) popupRoot.SetActive(false);
        PlayerStateManager.Instance.Unfreeze();

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= RefreshGrids;
    }

    // ---------------- Queries ----------------

    public int GetAvailableCount(CreatureData species, CreatureData.Rarity rarity)
    {
        int owned = InventoryManager.Instance.GetCount(species, rarity);
        int staged = GetSelectedCount(species, rarity);
        return Mathf.Max(0, owned - staged);
    }

    public int GetSelectedCount(CreatureData species, CreatureData.Rarity rarity)
    {
        return selection.TryGetValue((species, rarity), out int c) ? c : 0;
    }

    /// <summary>
    /// How many of the AVAILABLE (not-yet-staged) units of this species+rarity
    /// are sparkle-flagged - drives the sparkle badge on the inventory side.
    /// Sparkle-flagged units are treated as "claimed" by staging FIRST
    /// (matching InventoryManager.RemoveCreatures' actual consumption order),
    /// so this is whatever's left over: total sparkle minus however many are
    /// already staged. See CreatureTransformEntryUI.
    /// </summary>
    public int GetAvailableSparkleCount(CreatureData species, CreatureData.Rarity rarity)
    {
        int total = InventoryManager.Instance.GetSparkleCount(species, rarity);
        int staged = GetSelectedCount(species, rarity);
        return Mathf.Max(0, total - staged);
    }

    /// <summary>How many of the STAGED units of this species+rarity are sparkle-flagged - drives the sparkle badge on the selection side. See GetAvailableSparkleCount.</summary>
    public int GetStagedSparkleCount(CreatureData species, CreatureData.Rarity rarity)
    {
        int total = InventoryManager.Instance.GetSparkleCount(species, rarity);
        int staged = GetSelectedCount(species, rarity);
        return Mathf.Min(total, staged);
    }

    public IReadOnlyDictionary<(CreatureData species, CreatureData.Rarity rarity), int> GetSelection() => selection;

    // ---------------- Move actions (called by CreatureTransformEntryUI / TransformDropZoneUI) ----------------

    public void MoveToSelection(CreatureData species, CreatureData.Rarity rarity, int amount)
    {
        amount = Mathf.Min(amount, GetAvailableCount(species, rarity));
        if (amount <= 0) return;

        ApplyMoveToSelection(species, rarity, amount);
        NotifyChanged();
    }

    public void MoveFromSelection(CreatureData species, CreatureData.Rarity rarity, int amount)
    {
        amount = Mathf.Min(amount, GetSelectedCount(species, rarity));
        if (amount <= 0) return;

        ApplyMoveFromSelection(species, rarity, amount);
        NotifyChanged();
    }

    /// <summary>Wire to the "Put All" button.</summary>
    public void PutAllInSelection()
    {
        bool changed = false;

        foreach (var kvp in InventoryManager.Instance.GetAll())
        {
            int available = GetAvailableCount(kvp.Key.species, kvp.Key.rarity);
            if (available <= 0) continue;

            ApplyMoveToSelection(kvp.Key.species, kvp.Key.rarity, available);
            changed = true;
        }

        if (changed) NotifyChanged();
    }

    /// <summary>Wire to the "Take All Back" button.</summary>
    public void TakeAllBackFromSelection()
    {
        if (selection.Count == 0) return;

        selection.Clear();
        NotifyChanged();
    }

    private void ApplyMoveToSelection(CreatureData species, CreatureData.Rarity rarity, int amount)
    {
        var key = (species, rarity);
        selection[key] = GetSelectedCount(species, rarity) + amount;
    }

    private void ApplyMoveFromSelection(CreatureData species, CreatureData.Rarity rarity, int amount)
    {
        var key = (species, rarity);
        int remaining = GetSelectedCount(species, rarity) - amount;

        if (remaining <= 0) selection.Remove(key);
        else selection[key] = remaining;
    }

    private void NotifyChanged()
    {
        OnSelectionChanged?.Invoke();
        RefreshGrids();
    }

    // ---------------- Transform ----------------

    /// <summary>Wire to the "Transform" button.</summary>
    public void BeginTransform()
    {
        if (selection.Count == 0) return;

        // Snapshot so the animation hook has stable data even though the
        // popup (and therefore this selection) may change before CompleteTransform runs.
        var snapshot = new Dictionary<(CreatureData, CreatureData.Rarity), int>(selection);

        CompleteTransform();

        // Close(); // hide the popup so a world-space animation is visible - drop this line if you want the popup to stay open
        // OnTransformInitiated?.Invoke(snapshot);
    }

    /// <summary>
    /// Call once your transformation animation finishes. Consumes the staged
    /// creatures and grants resources. Ingredient Power is NOT rolled here
    /// any more - it resolves at CAPTURE time now (CreatureAI.TryCapture),
    /// and each unit's result is already tagged on InventoryManager's
    /// sparkle count. This just reads how many of the units being consumed
    /// were flagged (sparkle-flagged units are removed FIRST - see
    /// InventoryManager.RemoveCreatures - so this is exact, not a guess).
    /// See decision log.
    /// </summary>
    public void CompleteTransform()
    {
        foreach (var kvp in selection)
        {
            var species = kvp.Key.species;
            var rarity = kvp.Key.rarity;
            int amount = kvp.Value;

            int sparkleAvailable = InventoryManager.Instance.GetSparkleCount(species, rarity);
            int sparkleUsed = Mathf.Min(sparkleAvailable, amount);

            InventoryManager.Instance.RemoveCreatures(species, rarity, amount);

            var resource = species.GetResource(rarity);
            if (resource == null) continue;

            int totalGranted = sparkleUsed * 2 + (amount - sparkleUsed);
            ResourceInventoryManager.Instance.AddResource(resource, totalGranted);
        }

        selection.Clear();
        OnTransformCompleted?.Invoke();
    }

    // ---------------- Grid building ----------------

    private void RefreshGrids()
    {
        ClearChildren(inventoryGridParent);
        ClearChildren(selectionGridParent);

        foreach (var kvp in InventoryManager.Instance.GetAll())
        {
            int available = GetAvailableCount(kvp.Key.species, kvp.Key.rarity);
            if (available <= 0) continue; // fully staged - nothing left to show on the source side

            var entry = Instantiate(entryPrefab, inventoryGridParent);
            entry.Setup(kvp.Key.species, kvp.Key.rarity, available, TransformEntrySide.Inventory, this);
        }

        foreach (var kvp in selection)
        {
            var entry = Instantiate(entryPrefab, selectionGridParent);
            entry.Setup(kvp.Key.species, kvp.Key.rarity, kvp.Value, TransformEntrySide.Selection, this);
        }
    }

    private void ClearChildren(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }
}