using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central store of how many of each (species, rarity) combo the player has
/// captured. Fires OnInventoryChanged so UI (and later, the bestiary/stew
/// system) can react without polling.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    /// <summary>Key = species + rarity. A captured Rare rabbit and a Normal rabbit are separate entries.</summary>
    private readonly Dictionary<(CreatureData species, CreatureData.Rarity rarity), int> counts =
        new Dictionary<(CreatureData, CreatureData.Rarity), int>();

    /// <summary>Same shape as counts, but scoped to the current run only - cleared by ResetRunTracking().</summary>
    private readonly Dictionary<(CreatureData species, CreatureData.Rarity rarity), int> runCounts =
        new Dictionary<(CreatureData, CreatureData.Rarity), int>();

    /// <summary>Every species ever captured, any rarity, persists for the whole play session (never cleared).</summary>
    private readonly HashSet<CreatureData> everCapturedSpecies = new HashSet<CreatureData>();

    /// <summary>Species that became "ever captured" for the first time during the CURRENT run - cleared by ResetRunTracking().</summary>
    private readonly HashSet<CreatureData> newSpeciesThisRun = new HashSet<CreatureData>();

    public event Action OnInventoryChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddCreature(CreatureData species, CreatureData.Rarity rarity, int amount = 1)
    {
        if (species == null) return;

        var key = (species, rarity);

        if (!counts.ContainsKey(key)) counts[key] = 0;
        counts[key] += amount;

        if (!runCounts.ContainsKey(key)) runCounts[key] = 0;
        runCounts[key] += amount;

        if (!everCapturedSpecies.Contains(species))
        {
            everCapturedSpecies.Add(species);
            newSpeciesThisRun.Add(species);
        }

        OnInventoryChanged?.Invoke();
    }

    /// <summary>Call when a new expedition/run begins, so this-run stats (grid, "new species", etc.) start fresh.</summary>
    public void ResetRunTracking()
    {
        runCounts.Clear();
        newSpeciesThisRun.Clear();
    }

    /// <summary>How many of a species+rarity were captured THIS run.</summary>
    public int GetRunCount(CreatureData species, CreatureData.Rarity rarity)
    {
        return runCounts.TryGetValue((species, rarity), out int c) ? c : 0;
    }

    /// <summary>All species+rarity combos captured THIS run - what the run-end grid/stats read from.</summary>
    public IReadOnlyDictionary<(CreatureData species, CreatureData.Rarity rarity), int> GetRunCaptures() => runCounts;

    /// <summary>How many species were captured for the first time ever, during THIS run.</summary>
    public int GetNewSpeciesThisRunCount() => newSpeciesThisRun.Count;

    /// <summary>Removes captured creatures of a species+rarity (e.g. consumed by transforming them into resources).</summary>
    public void RemoveCreatures(CreatureData species, CreatureData.Rarity rarity, int amount)
    {
        var key = (species, rarity);
        if (!counts.ContainsKey(key)) return;

        counts[key] -= amount;
        if (counts[key] <= 0) counts.Remove(key);

        OnInventoryChanged?.Invoke();
    }

    /// <summary>Count of one specific species+rarity combo.</summary>
    public int GetCount(CreatureData species, CreatureData.Rarity rarity)
    {
        return counts.TryGetValue((species, rarity), out int c) ? c : 0;
    }

    /// <summary>Total captured of a species across all rarities - handy for bestiary "seen" checks.</summary>
    public int GetTotalCount(CreatureData species)
    {
        int total = 0;
        foreach (var kvp in counts)
        {
            if (kvp.Key.species == species)
                total += kvp.Value;
        }
        return total;
    }

    public IReadOnlyDictionary<(CreatureData species, CreatureData.Rarity rarity), int> GetAll() => counts;
}