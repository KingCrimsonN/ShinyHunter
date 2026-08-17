using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central store of how many of each creature the player has captured.
/// Fires OnInventoryChanged so UI (and later, the bestiary/stew system)
/// can react without polling.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    private readonly Dictionary<CreatureData, int> counts = new Dictionary<CreatureData, int>();

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

    public void AddCreature(CreatureData data, int amount = 1)
    {
        if (data == null) return;

        if (!counts.ContainsKey(data))
            counts[data] = 0;

        counts[data] += amount;
        OnInventoryChanged?.Invoke();
    }

    public int GetCount(CreatureData data)
    {
        return counts.TryGetValue(data, out int c) ? c : 0;
    }

    public IReadOnlyDictionary<CreatureData, int> GetAll() => counts;
}
