using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds the scrollable grid from CritterDexRegistry. Fully rebuilds on
/// every refresh (registry, entries all destroyed/reinstantiated) rather than
/// diffing in place - same convention as every other grid UI in the project
/// (see CLAUDE.md #8) - and subscribes to InventoryManager.OnInventoryChanged
/// only while enabled, so it stays live while the dex is open AND is
/// guaranteed fresh the next time it's opened, instead of showing whatever
/// was true when it was last closed. Attach to the panel that HOLDS the grid
/// (gridParent is the actual ScrollView Content object, so this component
/// itself is free to also hold the family-filter row above the grid).
///
/// IMPORTANT: a refresh (inventory change, filter change, this panel being
/// (re)enabled) never fires OnSpeciesSelected on its own - only an actual
/// click does. The detail panel is a popup a player opens on purpose; it
/// must not pop open just because a family filter was clicked or the dex was
/// reopened. See decision log.
/// </summary>
public class CritterDexGridUI : MonoBehaviour
{
    [SerializeField] private CritterDexRegistry registry;
    [SerializeField] private CritterDexEntryUI entryPrefab;
    [Tooltip("REQUIRED - the ScrollView's Content transform (the one with the Grid Layout Group), NOT this component's own object. Refresh() destroys every child of this transform on each rebuild, so it must point at the innermost Content object, never at a panel that also holds the ScrollView itself or anything else you don't want wiped.")]
    [SerializeField] private Transform gridParent;

    private readonly List<CritterDexEntryUI> entries = new List<CritterDexEntryUI>();
    private CritterDexEntryUI selectedEntry;
    private CreatureData selectedSpecies;
    private IngredientFamily? familyFilter;

    /// <summary>Fires ONLY when the player clicks an entry - see class docs.</summary>
    public event Action<CreatureData> OnSpeciesSelected;

    private void OnEnable()
    {
        InventoryManager.Instance.OnInventoryChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= Refresh;
    }

    /// <summary>Null = show every species. Rebuilds immediately.</summary>
    public void SetFamilyFilter(IngredientFamily? family)
    {
        familyFilter = family;
        Refresh();
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
        entries.Clear();

        CritterDexEntryUI matchedEntry = null;

        if (registry != null && registry.species != null)
        {
            for (int i = 0; i < registry.species.Count; i++)
            {
                var species = registry.species[i];
                if (species == null) continue;
                if (familyFilter.HasValue && species.family != familyFilter.Value) continue;

                bool unlocked = InventoryManager.Instance.HasEverCaptured(species); // survives the species later being transformed away entirely - see decision log

                var entry = Instantiate(entryPrefab, gridParent);
                entry.Set(species, i + 1, unlocked, HandleEntryClicked); // dex number is the species' fixed registry position, unaffected by filtering
                entries.Add(entry);

                if (species == selectedSpecies) matchedEntry = entry;
            }
        }

        // Re-apply the existing highlight if that species is still visible
        // under the current filter (entries are destroyed/recreated every
        // refresh, so the OLD CritterDexEntryUI instance is gone even when
        // the species itself is still "selected"). Deliberately does NOT
        // fire OnSpeciesSelected and does NOT fall back to auto-selecting
        // the first entry if the previous selection is gone - a refresh must
        // never pop the detail panel open (or closed) on its own.
        selectedEntry = matchedEntry;
        if (selectedEntry != null) selectedEntry.SetSelected(true);
    }

    private void HandleEntryClicked(CreatureData species)
    {
        if (selectedEntry != null) selectedEntry.SetSelected(false);

        selectedEntry = entries.Find(e => e.Species == species);
        selectedSpecies = species;
        if (selectedEntry != null) selectedEntry.SetSelected(true);

        OnSpeciesSelected?.Invoke(species);
    }
}
