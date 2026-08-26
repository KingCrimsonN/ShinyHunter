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

    public event Action OnInventoryChanged;

    // public GameObject inventoryUI;

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
        if (!counts.ContainsKey(key))
            counts[key] = 0;

        counts[key] += amount;
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

    public int CalculateCaptureValue()
    {
        int totalValue = 0;
        foreach (var kvp in counts)
        {
            var species = kvp.Key.species;
            var rarity = kvp.Key.rarity;
            int count = kvp.Value;

            if (species != null && species.valuePerRarity != null)
            {
                int rarityIndex = (int)rarity;
                if (rarityIndex >= 0 && rarityIndex < species.valuePerRarity.Length)
                {
                    int valuePerCreature = species.valuePerRarity[rarityIndex];
                    totalValue += valuePerCreature * count;
                }
            }
        }
        return totalValue;
    }

    public IReadOnlyDictionary<(CreatureData species, CreatureData.Rarity rarity), int> GetAll() => counts;
}