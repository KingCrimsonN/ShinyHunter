using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds the scrollable grid from CritterDexRegistry, keeps lock states in
/// sync with InventoryManager, and reports which species is selected.
/// Attach to the Content object of a ScrollView (with a Grid Layout Group).
/// </summary>
public class CritterDexGridUI : MonoBehaviour
{
    [SerializeField] private CritterDexRegistry registry;
    [SerializeField] private CritterDexEntryUI entryPrefab;

    private readonly List<CritterDexEntryUI> entries = new List<CritterDexEntryUI>();
    private CritterDexEntryUI selectedEntry;
    private bool built;

    /// <summary>Fires with the species the player clicked in the grid.</summary>
    public event Action<CreatureData> OnSpeciesSelected;

    private void OnEnable()
    {
        if (!built) BuildGrid();
        InventoryManager.Instance.OnInventoryChanged += RefreshLockStates;
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= RefreshLockStates;
    }

    private void BuildGrid()
    {
        built = true;
        if (registry == null || registry.species == null) return;

        for (int i = 0; i < registry.species.Count; i++)
        {
            var species = registry.species[i];
            var entry = Instantiate(entryPrefab, transform);
            bool unlocked = InventoryManager.Instance.GetTotalCount(species) > 0;
            entry.Set(species, i + 1, unlocked, HandleEntryClicked);
            entries.Add(entry);
        }

        // Auto-select the first entry so the detail panel isn't empty on open.
        if (registry.species.Count > 0)
            HandleEntryClicked(registry.species[0]);
    }

    private void RefreshLockStates()
    {
        for (int i = 0; i < entries.Count; i++)
        {
            var species = registry.species[i];
            bool unlocked = InventoryManager.Instance.GetTotalCount(species) > 0;
            entries[i].Set(species, i + 1, unlocked, HandleEntryClicked);
        }
    }

    private void HandleEntryClicked(CreatureData species)
    {
        int index = registry.species.IndexOf(species);

        if (selectedEntry != null) selectedEntry.SetSelected(false);
        if (index >= 0 && index < entries.Count)
        {
            selectedEntry = entries[index];
            selectedEntry.SetSelected(true);
        }

        OnSpeciesSelected?.Invoke(species);
    }
}