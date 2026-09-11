using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Page 1 of the run summary: grid of everything captured THIS run, rarest
/// first. Rebuilds fresh each time BuildGrid() is called rather than staying
/// live-updated, since the run is over by the time this shows.
/// </summary>
public class RunCreatureGridUI : MonoBehaviour
{
    [SerializeField] private RunCreatureEntryUI entryPrefab;
    [Tooltip("Index 0=Normal, 1=Uncommon, 2=Rare, 3=Legendary.")]
    [SerializeField] private Sprite[] rarityFrames = new Sprite[4];

    private readonly List<RunCreatureEntryUI> spawned = new List<RunCreatureEntryUI>();

    public void BuildGrid()
    {
        Clear();

        var entries = new List<KeyValuePair<(CreatureData species, CreatureData.Rarity rarity), int>>(
            InventoryManager.Instance.GetRunCaptures());

        entries.Sort((a, b) =>
        {
            int rarityCompare = b.Key.rarity.CompareTo(a.Key.rarity); // rarest first
            return rarityCompare != 0
                ? rarityCompare
                : string.Compare(a.Key.species.creatureName, b.Key.species.creatureName, StringComparison.OrdinalIgnoreCase);
        });

        foreach (var kvp in entries)
        {
            var entry = Instantiate(entryPrefab, transform);
            entry.Set(kvp.Key.species, kvp.Key.rarity, kvp.Value, GetFrame(kvp.Key.rarity));
            spawned.Add(entry);
        }
    }

    private Sprite GetFrame(CreatureData.Rarity rarity)
    {
        if (rarityFrames == null || rarityFrames.Length == 0) return null;
        int index = (int)rarity;
        return index < rarityFrames.Length ? rarityFrames[index] : rarityFrames[0];
    }

    private void Clear()
    {
        foreach (var entry in spawned)
            if (entry != null) Destroy(entry.gameObject);

        spawned.Clear();
    }
}