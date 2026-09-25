using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central store of how many of each (species, rarity) combo the player has
/// captured, plus how many of each stack are flagged double-yield (Ingredient
/// Power - see sparkleCounts). Fires OnInventoryChanged so UI (and later, the
/// bestiary/stew system) can react without polling.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    /// <summary>Key = species + rarity. A captured Rare rabbit and a Normal rabbit are separate entries.</summary>
    private readonly Dictionary<(CreatureData species, CreatureData.Rarity rarity), int> counts =
        new Dictionary<(CreatureData, CreatureData.Rarity), int>();

    /// <summary>
    /// How many of each stack's CURRENT stock are flagged to yield double
    /// resources when eventually transformed (Ingredient Power, rolled once
    /// PER CAPTURED UNIT - see CreatureAI.TryCapture). Always &lt;= the
    /// matching entry in counts. Not truly per-instance (individual captured
    /// creatures aren't distinguishable, only counted) - flagged units within
    /// a stack are interchangeable, and RemoveCreatures always consumes
    /// flagged ones FIRST, so "which n are sparkly" is well-defined without
    /// needing real per-instance identity. See decision log.
    /// </summary>
    private readonly Dictionary<(CreatureData species, CreatureData.Rarity rarity), int> sparkleCounts =
        new Dictionary<(CreatureData, CreatureData.Rarity), int>();

    /// <summary>Same shape as counts, but scoped to the current run only - cleared by ResetRunTracking().</summary>
    private readonly Dictionary<(CreatureData species, CreatureData.Rarity rarity), int> runCounts =
        new Dictionary<(CreatureData, CreatureData.Rarity), int>();

    /// <summary>Every species ever captured, any rarity, persists for the whole play session (never cleared).</summary>
    private readonly HashSet<CreatureData> everCapturedSpecies = new HashSet<CreatureData>();

    /// <summary>
    /// Every (species, rarity) combo ever captured, persists for the whole
    /// play session (never cleared) - unlike `counts`, which drops back out
    /// once every unit of that combo is later transformed/consumed. The
    /// CritterDex reads THIS, not GetCount, for "has this rarity been seen" -
    /// a species/rarity that's been caught once must never disappear from
    /// the dex again just because none are currently held. See decision log.
    /// </summary>
    private readonly HashSet<(CreatureData species, CreatureData.Rarity rarity)> everCapturedRarities =
        new HashSet<(CreatureData, CreatureData.Rarity)>();

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
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
    }

    /// <summary>
    /// Every expedition starts with fresh per-run tracking. This lives HERE
    /// (rather than in some player component's Start) so it can't be skipped:
    /// it used to be tied to PlayerCapture.Start in the hub, but that component
    /// is disabled in the hub scene, so the reset never ran and each run
    /// summary showed the previous runs' creatures too.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!SceneNames.IsHub(scene.name))
            ResetRunTracking();
    }

    /// <param name="doubleYield">True if this capture was flagged by Ingredient Power to yield double resources on transform (see CreatureAI.TryCapture). Applies to the WHOLE amount being added in this one call.</param>
    public void AddCreature(CreatureData species, CreatureData.Rarity rarity, int amount = 1, bool doubleYield = false)
    {
        if (species == null) return;

        var key = (species, rarity);

        if (!counts.ContainsKey(key)) counts[key] = 0;
        counts[key] += amount;

        if (!runCounts.ContainsKey(key)) runCounts[key] = 0;
        runCounts[key] += amount;

        if (doubleYield)
        {
            if (!sparkleCounts.ContainsKey(key)) sparkleCounts[key] = 0;
            sparkleCounts[key] += amount;
        }

        if (!everCapturedSpecies.Contains(species))
        {
            everCapturedSpecies.Add(species);
            newSpeciesThisRun.Add(species);
        }
        everCapturedRarities.Add(key);

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

    /// <summary>
    /// Removes captured creatures of a species+rarity (e.g. consumed by
    /// transforming them into resources). Sparkle-flagged (double-yield)
    /// units are always consumed FIRST - see the sparkleCounts field comment
    /// - so a caller that wants to know how many of the units it's about to
    /// remove were flagged should read GetSparkleCount(species, rarity)
    /// (clamped to `amount`) BEFORE calling this.
    /// </summary>
    public void RemoveCreatures(CreatureData species, CreatureData.Rarity rarity, int amount)
    {
        var key = (species, rarity);
        if (!counts.ContainsKey(key)) return;

        counts[key] -= amount;
        if (counts[key] <= 0) counts.Remove(key);

        if (sparkleCounts.TryGetValue(key, out int sparkle))
        {
            int remainingSparkle = Mathf.Max(0, sparkle - amount);
            if (remainingSparkle <= 0) sparkleCounts.Remove(key);
            else sparkleCounts[key] = remainingSparkle;
        }

        OnInventoryChanged?.Invoke();
    }

    /// <summary>Count of one specific species+rarity combo.</summary>
    public int GetCount(CreatureData species, CreatureData.Rarity rarity)
    {
        return counts.TryGetValue((species, rarity), out int c) ? c : 0;
    }

    /// <summary>How many of this species+rarity's current stock are flagged double-yield (Ingredient Power) - always &lt;= GetCount(species, rarity). 0 = no sparkle badge.</summary>
    public int GetSparkleCount(CreatureData species, CreatureData.Rarity rarity)
    {
        return sparkleCounts.TryGetValue((species, rarity), out int c) ? c : 0;
    }

    /// <summary>Total captured of a species across all rarities, CURRENTLY HELD. Drops back to 0 once every unit is transformed/consumed - NOT what the CritterDex should use for "seen" (see HasEverCaptured), just for inventory/UI stock displays.</summary>
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

    /// <summary>True if this species has EVER been captured at any rarity - survives every unit later being transformed away. What the CritterDex should check for "is this species unlocked" instead of GetTotalCount.</summary>
    public bool HasEverCaptured(CreatureData species)
    {
        return everCapturedSpecies.Contains(species);
    }

    /// <summary>True if this exact species+rarity has EVER been captured - survives every unit later being transformed away. What the CritterDex should check for "has this rarity been seen" instead of GetCount.</summary>
    public bool HasEverCaptured(CreatureData species, CreatureData.Rarity rarity)
    {
        return everCapturedRarities.Contains((species, rarity));
    }

    public IReadOnlyDictionary<(CreatureData species, CreatureData.Rarity rarity), int> GetAll() => counts;
}