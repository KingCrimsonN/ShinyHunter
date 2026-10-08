using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Creature-to-resource transform station. Opened externally (call
/// Instance.Open() from your interactable object's script - no toggle key
/// of its own). Holds a local, transient selection of the SPECIFIC critters
/// staged for transformation; nothing is actually consumed until
/// CompleteTransform().
///
/// Works on individual critters (CapturedCritter), not counts: staging the
/// sparkly one stages THAT one. Both grids show CritterStacks - one tile per
/// critter in individual view (default), one per species+rarity in grouped
/// view (CritterInventoryView, shared with the inventory). Moving N out of a
/// group picks which critters go: double-yield ones first into the selection
/// (so transforming gets the most out of them), plain ones first back out.
///
/// Two-phase transform, to leave room for your animation:
///   1. BeginTransform() - validates selection, snapshots it, fires
///      OnTransformInitiated(snapshot). Nothing is consumed yet.
///   2. CompleteTransform() - call this once your animation finishes. THIS
///      is what actually removes the critters from InventoryManager and
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

    [Header("Pop-ups")]
    [Tooltip("Shown when a critter is clicked: the ingredient it would give. The same CritterDexIngredientPopupUI script the dex uses (this scene's own instance).")]
    [SerializeField] private CritterDexIngredientPopupUI ingredientPopup;
    [Tooltip("Shown after a transformation: the ingredients you received, with a close button that only closes the pop-up.")]
    [SerializeField] private TransformResultPopupUI resultPopup;

    /// <summary>The critters staged for transformation, in the order they were staged.</summary>
    private readonly List<CapturedCritter> staged = new List<CapturedCritter>();

    public event Action OnSelectionChanged;
    /// <summary>Fired when Transform is pressed, with a snapshot of the staged critters. Start your animation here.</summary>
    public event Action<IReadOnlyList<CapturedCritter>> OnTransformInitiated;
    /// <summary>Fired once CompleteTransform() has actually granted resources.</summary>
    public event Action OnTransformCompleted;

    private void Awake()
    {
        Instance = this;

        CreatureTransformEntryUI.DragIcon = dragIconTemplate;
        if (dragIconTemplate != null) dragIconTemplate.gameObject.SetActive(false);
        if (popupRoot != null) popupRoot.SetActive(false);

        // Here (not in the shared script's own defaults) so the dex's pop-up
        // keeps its old behaviour: this one also closes on any click outside it.
        if (ingredientPopup != null) ingredientPopup.SetCloseOnOutsideClick(true);
    }

    private void OnDestroy()
    {
        // Scene-scoped object: if the scene unloads while the popup is open,
        // don't leave the persistent inventory / static view setting calling
        // into a destroyed object.
        Unsubscribe();
    }

    // ---------------- Open / Close ----------------

    public void Open()
    {
        staged.Clear(); // starts fresh each time - remove this line if you'd rather staged critters persist between opens

        if (popupRoot != null) popupRoot.SetActive(true);
        // Freezes movement, Interactor, the stick and tool use together
        // (and frees the cursor) - see PlayerStateManager. Registers Close
        // as the Escape callback (see PlayerStateManager.TryCloseCurrentPopup
        // / UIManager's centralized Escape handling). See decision log.
        PlayerStateManager.Instance.Freeze(HandleEscape);
        HidePopups();

        // Unsubscribe first so an Open() without a matching Close() can't double-subscribe.
        Unsubscribe();
        InventoryManager.Instance.OnInventoryChanged += RefreshGrids;
        CritterInventoryView.Changed += RefreshGrids;
        RefreshGrids();
    }

    /// <summary>Escape closes the top-most layer first: the result pop-up, then the ingredient pop-up, and only then the table itself.</summary>
    private void HandleEscape()
    {
        if (resultPopup != null && resultPopup.IsOpen) { resultPopup.Hide(); return; }
        if (ingredientPopup != null && ingredientPopup.IsOpen) { ingredientPopup.Hide(); return; }
        Close();
    }

    private void HidePopups()
    {
        if (ingredientPopup != null) ingredientPopup.Hide();
        if (resultPopup != null) resultPopup.Hide();
    }

    /// <summary>Called by a critter tile when it is clicked: shows the ingredient that critter would give (what it is - not the x2).</summary>
    public void ShowIngredientPopup(CritterStack stack, RectTransform tile)
    {
        if (ingredientPopup == null || stack == null) return;

        ingredientPopup.ShowAt(stack.Species.GetResource(stack.Rarity), tile); // a null resource hides it
    }

    public void Close()
    {
        HidePopups();
        if (popupRoot != null) popupRoot.SetActive(false);
        PlayerStateManager.Instance.Unfreeze();
        Unsubscribe();
    }

    private void Unsubscribe()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= RefreshGrids;
        CritterInventoryView.Changed -= RefreshGrids;
    }

    // ---------------- Queries ----------------

    public IReadOnlyList<CapturedCritter> GetSelection() => staged;

    public bool IsStaged(CapturedCritter critter) => staged.Contains(critter);

    /// <summary>Held critters that aren't staged, in capture order - the source side of the table.</summary>
    private List<CapturedCritter> GetAvailable()
    {
        var available = new List<CapturedCritter>();
        foreach (var critter in InventoryManager.Instance.Critters)
            if (!staged.Contains(critter)) available.Add(critter);
        return available;
    }

    // ---------------- Move actions (called by CreatureTransformEntryUI / TransformDropZoneUI) ----------------

    /// <summary>Stages up to `amount` of the critters on this tile. From a group, double-yield critters go first - they're the ones worth transforming.</summary>
    public void MoveToSelection(CritterStack stack, int amount)
    {
        int moved = 0;
        foreach (var critter in Ordered(stack, doubleYieldFirst: true))
        {
            if (moved >= amount) break;
            if (staged.Contains(critter) || !IsHeld(critter)) continue;

            staged.Add(critter);
            moved++;
        }

        if (moved > 0) NotifyChanged();
    }

    /// <summary>Un-stages up to `amount` of the critters on this tile. From a group, plain critters go back first, so the double-yield ones stay selected.</summary>
    public void MoveFromSelection(CritterStack stack, int amount)
    {
        int moved = 0;
        foreach (var critter in Ordered(stack, doubleYieldFirst: false))
        {
            if (moved >= amount) break;
            if (staged.Remove(critter)) moved++;
        }

        if (moved > 0) NotifyChanged();
    }

    /// <summary>Wire to the "Put All" button.</summary>
    public void PutAllInSelection()
    {
        var available = GetAvailable();
        if (available.Count == 0) return;

        staged.AddRange(available);
        NotifyChanged();
    }

    /// <summary>Wire to the "Take All Back" button.</summary>
    public void TakeAllBackFromSelection()
    {
        if (staged.Count == 0) return;

        staged.Clear();
        NotifyChanged();
    }

    /// <summary>The stack's critters with double-yield ones first (or last), otherwise in their existing order.</summary>
    private static IEnumerable<CapturedCritter> Ordered(CritterStack stack, bool doubleYieldFirst)
    {
        foreach (var critter in stack.Critters)
            if (critter.doubleYield == doubleYieldFirst) yield return critter;
        foreach (var critter in stack.Critters)
            if (critter.doubleYield != doubleYieldFirst) yield return critter;
    }

    private static bool IsHeld(CapturedCritter critter)
    {
        foreach (var held in InventoryManager.Instance.Critters)
            if (held == critter) return true;
        return false;
    }

    private void NotifyChanged()
    {
        // The pop-up describes a critter that's about to move (or whose tile is
        // about to be rebuilt) - close it rather than leave it pointing at nothing.
        if (ingredientPopup != null) ingredientPopup.Hide();

        OnSelectionChanged?.Invoke();
        RefreshGrids();
    }

    // ---------------- Transform ----------------

    /// <summary>Wire to the "Transform" button.</summary>
    public void BeginTransform()
    {
        if (staged.Count == 0) return;

        // Snapshot so the animation hook has stable data even though the
        // popup (and therefore this selection) may change before CompleteTransform runs.
        var snapshot = new List<CapturedCritter>(staged);

        CompleteTransform();

        // Close(); // hide the popup so a world-space animation is visible - drop this line if you want the popup to stay open
        // OnTransformInitiated?.Invoke(snapshot);
    }

    /// <summary>
    /// Call once your transformation animation finishes. Consumes exactly the
    /// staged critters and grants each one's resource - two of it if that
    /// critter is flagged double-yield (decided at capture, see
    /// CreatureAI.TryCapture). No more "assume the flagged ones are consumed
    /// first": every critter carries its own flag. See decision log.
    /// </summary>
    public void CompleteTransform()
    {
        var grants = new Dictionary<ResourceData, int>();
        var consumed = new List<CapturedCritter>();

        foreach (var critter in staged)
        {
            if (!IsHeld(critter)) continue; // left the inventory some other way while staged

            consumed.Add(critter);

            var resource = critter.species.GetResource(critter.rarity);
            if (resource == null) continue;

            int amount = critter.doubleYield ? 2 : 1;
            grants[resource] = grants.TryGetValue(resource, out int n) ? n + amount : amount;
        }

        staged.Clear();
        InventoryManager.Instance.RemoveCritters(consumed);

        foreach (var grant in grants)
            ResourceInventoryManager.Instance.AddResource(grant.Key, grant.Value);

        if (ingredientPopup != null) ingredientPopup.Hide();
        if (resultPopup != null) resultPopup.Show(grants); // only this pop-up closes with its button - the table stays open

        OnTransformCompleted?.Invoke();
    }

    // ---------------- Grid building ----------------

    private void RefreshGrids()
    {
        // Drop anything staged that's no longer held (e.g. removed elsewhere while open).
        staged.RemoveAll(c => !IsHeld(c));

        ClearChildren(inventoryGridParent);
        ClearChildren(selectionGridParent);

        bool grouped = CritterInventoryView.Grouped;

        foreach (var stack in CritterStack.Build(GetAvailable(), grouped))
        {
            var entry = Instantiate(entryPrefab, inventoryGridParent);
            entry.Setup(stack, TransformEntrySide.Inventory, this);
        }

        foreach (var stack in CritterStack.Build(staged, grouped))
        {
            var entry = Instantiate(entryPrefab, selectionGridParent);
            entry.Setup(stack, TransformEntrySide.Selection, this);
        }
    }

    private void ClearChildren(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }
}
