using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central store of every critter the player currently holds - ONE
/// CapturedCritter record per critter caught, each with its own state
/// (double-yield flag). Grouping by species+rarity is purely a display choice
/// made by the UI (CritterStack / CritterInventoryView); the count-style
/// queries below (GetCount, GetSparkleCount, GetAll...) are computed from the
/// list for the systems that only care about totals. Also tracks per-run
/// captures and "ever caught" for the run summary and CritterDex. Fires
/// OnInventoryChanged so UI can react without polling.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    /// <summary>
    /// Every critter currently held, in the order they were caught. Replaces the
    /// old count-per-(species, rarity) + separate double-yield sub-count: with
    /// those, critters were interchangeable numbers, so the transform table had
    /// to assume "double-yield ones are consumed first" - it could never honour
    /// the player picking one particular critter. See decision log.
    /// </summary>
    private readonly List<CapturedCritter> critters = new List<CapturedCritter>();
    private int nextCritterId = 1;

    /// <summary>Captures THIS run, counted per species+rarity (the run summary only needs totals) - cleared by ResetRunTracking(). Unlike the held critters, never reduced by transforming.</summary>
    private readonly Dictionary<(CreatureData species, CreatureData.Rarity rarity), int> runCounts =
        new Dictionary<(CreatureData, CreatureData.Rarity), int>();

    /// <summary>Every species ever captured, any rarity, persists for the whole play session (never cleared).</summary>
    private readonly HashSet<CreatureData> everCapturedSpecies = new HashSet<CreatureData>();

    /// <summary>
    /// Every (species, rarity) combo ever captured, persists for the whole
    /// play session (never cleared) - unlike the held critters, which disappear
    /// once they are transformed/consumed. The
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

    /// <summary>Adds `amount` new critter records (normally 1 - one capture).</summary>
    /// <param name="doubleYield">True if this capture was flagged to yield double resources on transform (see CreatureAI.TryCapture). Applies to every critter added in this one call.</param>
    public void AddCreature(CreatureData species, CreatureData.Rarity rarity, int amount = 1, bool doubleYield = false)
    {
        if (species == null || amount <= 0) return;

        var key = (species, rarity);

        for (int i = 0; i < amount; i++)
        {
            critters.Add(new CapturedCritter
            {
                id = nextCritterId++,
                species = species,
                rarity = rarity,
                doubleYield = doubleYield,
            });
        }

        if (!runCounts.ContainsKey(key)) runCounts[key] = 0;
        runCounts[key] += amount;

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

    /// <summary>Every critter currently held, in capture order - what the critter grids build their tiles from.</summary>
    public IReadOnlyList<CapturedCritter> Critters => critters;

    /// <summary>
    /// Removes these specific critters (e.g. the ones staged on the transform
    /// table). Critters no longer in the inventory are ignored. Fires
    /// OnInventoryChanged once, however many were removed.
    /// </summary>
    public void RemoveCritters(IEnumerable<CapturedCritter> toRemove)
    {
        if (toRemove == null) return;

        var set = new HashSet<CapturedCritter>(toRemove);
        int removed = critters.RemoveAll(c => set.Contains(c));

        if (removed > 0) OnInventoryChanged?.Invoke();
    }

    /// <summary>How many critters of this species+rarity are currently held.</summary>
    public int GetCount(CreatureData species, CreatureData.Rarity rarity)
    {
        int n = 0;
        foreach (var c in critters)
            if (c.species == species && c.rarity == rarity) n++;
        return n;
    }

    /// <summary>How many of the held critters of this species+rarity yield double resources - always &lt;= GetCount. For grouped displays; individual tiles read CapturedCritter.doubleYield directly.</summary>
    public int GetSparkleCount(CreatureData species, CreatureData.Rarity rarity)
    {
        int n = 0;
        foreach (var c in critters)
            if (c.species == species && c.rarity == rarity && c.doubleYield) n++;
        return n;
    }

    /// <summary>Total captured of a species across all rarities, CURRENTLY HELD. Drops back to 0 once every unit is transformed/consumed - NOT what the CritterDex should use for "seen" (see HasEverCaptured), just for inventory/UI stock displays.</summary>
    public int GetTotalCount(CreatureData species)
    {
        int total = 0;
        foreach (var c in critters)
            if (c.species == species) total++;
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

    /// <summary>Held critters counted per species+rarity, in first-caught order. Built fresh each call (the list is small) - for code that only needs totals.</summary>
    public IReadOnlyDictionary<(CreatureData species, CreatureData.Rarity rarity), int> GetAll()
    {
        var totals = new Dictionary<(CreatureData, CreatureData.Rarity), int>();
        foreach (var c in critters)
        {
            var key = (c.species, c.rarity);
            totals[key] = totals.TryGetValue(key, out int n) ? n + 1 : 1;
        }
        return totals;
    }
}