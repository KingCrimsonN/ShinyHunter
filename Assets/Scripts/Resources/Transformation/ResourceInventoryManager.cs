using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stash of collected resources. Unlike ToolInventoryManager this has no
/// fixed slot count or hotbar keybinds - it's a dictionary of counts, keyed
/// directly by ResourceData (which already encodes its own rarity, so no
/// separate rarity key is needed here the way creature captures need one).
///
/// Display order is tracked separately from the dictionary via orderedKeys:
/// new resources append to the end as they're picked up, and SortByRarityThenName()
/// re-sorts that list on demand (the "Sort" button's action). This is the
/// general pattern for sorting any dictionary-backed inventory - see the
/// class comment on ToolInventoryManager.SortSlots() for how the same idea
/// applies to a fixed-slot inventory instead.
/// </summary>
public class ResourceInventoryManager : MonoBehaviour
{
    public static ResourceInventoryManager Instance { get; private set; }

    private readonly Dictionary<ResourceData, int> counts = new Dictionary<ResourceData, int>();
    private readonly List<ResourceData> orderedKeys = new List<ResourceData>();

    public event Action OnInventoryChanged;

    [SerializeField] private TMPro.TMP_Text descriptionText;

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

    public void AddResource(ResourceData data, int amount = 1)
    {
        if (data == null || amount <= 0) return;

        if (!counts.ContainsKey(data))
        {
            counts[data] = 0;
            orderedKeys.Add(data); // new resource type appends at the current end of the display order
        }

        counts[data] += amount;
        OnInventoryChanged?.Invoke();
    }

    public void RemoveResource(ResourceData data, int amount = 1)
    {
        if (data == null || !counts.ContainsKey(data)) return;

        counts[data] -= amount;
        if (counts[data] <= 0)
        {
            counts.Remove(data);
            orderedKeys.Remove(data);
        }

        OnInventoryChanged?.Invoke();
    }

    /// <summary>True if the player holds at least one ingredient of any kind.</summary>
    public bool HasAnyResources => counts.Count > 0;

    public int GetCount(ResourceData data) => data != null && counts.TryGetValue(data, out int c) ? c : 0;

    /// <summary>Current display order - stable until a pickup appends a new type or SortByRarityThenName() runs.</summary>
    public IReadOnlyList<ResourceData> GetOrderedEntries() => orderedKeys;

    /// <summary>
    /// The "Sort" button's action: reorders the display list by rarity
    /// (rarest first) then alphabetically. This mutates orderedKeys once -
    /// it's a one-off reorder, not a persistent live sort, so a resource
    /// picked up afterward still appends to the end until Sort runs again.
    /// </summary>
    /// <summary>
    /// Family order (Bug, Plant, Freaky, Warm, Cold), rarest first inside a
    /// family, then by name. One-off reorder, like SortByRarityThenName.
    /// </summary>
    public void SortByFamily()
    {
        orderedKeys.Sort((a, b) =>
        {
            int c = a.family.CompareTo(b.family);
            if (c == 0) c = b.rarity.CompareTo(a.rarity);
            return c != 0 ? c : string.Compare(a.resourceName, b.resourceName, StringComparison.OrdinalIgnoreCase);
        });

        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// By scent stats: grouped by each ingredient's STRONGEST scent (ScentType
    /// order: Sweet, Fresh, Putrid, Metallic, Marine), strongest value first
    /// inside a group, then by name - so everything that pushes a meal toward
    /// the same smell sits together. One-off reorder.
    /// </summary>
    public void SortByStats()
    {
        orderedKeys.Sort((a, b) =>
        {
            var sa = StrongestScent(a);
            var sb = StrongestScent(b);
            int c = sa.CompareTo(sb);
            if (c == 0) c = b.GetScent(sb).CompareTo(a.GetScent(sa));
            return c != 0 ? c : string.Compare(a.resourceName, b.resourceName, StringComparison.OrdinalIgnoreCase);
        });

        OnInventoryChanged?.Invoke();
    }

    private static ScentType StrongestScent(ResourceData r)
    {
        var best = ScentType.Sweet;
        foreach (ScentType s in Enum.GetValues(typeof(ScentType)))
            if (r.GetScent(s) > r.GetScent(best)) best = s;
        return best;
    }

    public void SortByRarityThenName()
    {
        orderedKeys.Sort((a, b) =>
        {
            int rarityCompare = b.rarity.CompareTo(a.rarity); // higher rarity first
            return rarityCompare != 0
                ? rarityCompare
                : string.Compare(a.resourceName, b.resourceName, StringComparison.OrdinalIgnoreCase);
        });

        OnInventoryChanged?.Invoke();
    }
}