using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The INDEX page of the book: a grid with one entry per species, in book
/// order. Clicking an entry asks the book (CritterDexUI) to flip to that
/// species' page - this component no longer tracks a "selected" entry at all,
/// since selecting something now just turns the page.
///
/// The species list is handed in by the book (SetSpecies) rather than read
/// from the registry here, so the book stays the single owner of the ordering.
/// Fully rebuilds on every refresh (CLAUDE.md #8) and listens to
/// InventoryManager.OnInventoryChanged only while enabled, so the discovered
/// state is always fresh the moment the index page is shown.
/// </summary>
public class CritterDexGridUI : MonoBehaviour
{
    [SerializeField] private CritterDexEntryUI entryPrefab;
    [Tooltip("REQUIRED - the ScrollView's Content transform (the one with the Grid Layout Group), NOT this component's own object. Refresh() destroys every child of this transform on each rebuild, so it must point at the innermost Content object, never at a panel that also holds the ScrollView itself or anything else you don't want wiped.")]
    [SerializeField] private Transform gridParent;

    private IReadOnlyList<CreatureData> species;

    /// <summary>Fires ONLY when the player clicks an entry.</summary>
    public event Action<CreatureData> OnSpeciesClicked;

    /// <summary>Called by the book with the ordered species list. Rebuilds immediately if the index page is currently showing.</summary>
    public void SetSpecies(IReadOnlyList<CreatureData> orderedSpecies)
    {
        species = orderedSpecies;
        if (isActiveAndEnabled) Refresh();
    }

    private void OnEnable()
    {
        if (InventoryManager.Instance != null) InventoryManager.Instance.OnInventoryChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null) InventoryManager.Instance.OnInventoryChanged -= Refresh;
    }

    private void Refresh()
    {
        if (gridParent == null)
        {
            // Refuse to rebuild rather than guess - this used to fall back to
            // destroying this component's OWN transform's children, which
            // silently wiped out the ScrollView/Content hierarchy itself
            // whenever this field was left unassigned. See decision log.
            Debug.LogError($"CritterDexGridUI on '{name}': Grid Parent is not assigned - assign it to the ScrollView's Content transform in the inspector.", this);
            return;
        }

        foreach (Transform child in gridParent)
            Destroy(child.gameObject);

        if (species == null || entryPrefab == null) return;

        for (int i = 0; i < species.Count; i++)
        {
            var entry = Instantiate(entryPrefab, gridParent);
            entry.Set(species[i], i + 1, HandleEntryClicked); // dex number = position in book order
        }
    }

    private void HandleEntryClicked(CreatureData clicked)
    {
        OnSpeciesClicked?.Invoke(clicked);
    }
}
