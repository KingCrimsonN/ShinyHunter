using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Persistent storage for brewed stews ("bowls"). baseCapacity is this hub's
/// own local bowl count (default 3); sharedCapacity is an unlockable pool
/// usable from anywhere. Total capacity = base + shared.
///
/// NOTE: this project currently has one Hub scene, so "per hub" capacity is
/// represented as a single pool here. If you add multiple hub scenes later,
/// baseCapacity would need to become keyed by scene name - flagging this as
/// the natural extension point, not built now since only one hub exists.
/// </summary>
public class StewInventoryManager : MonoBehaviour
{
    public static StewInventoryManager Instance { get; private set; }

    [SerializeField] private int baseCapacity = 3;
    [SerializeField] private int sharedCapacity = 0;

    private readonly List<StewInstance> bowls = new List<StewInstance>();

    public event Action OnBowlsChanged;

    public int Capacity => baseCapacity + sharedCapacity;
    public int Count => bowls.Count;
    public bool IsFull => bowls.Count >= Capacity;
    public IReadOnlyList<StewInstance> Bowls => bowls;

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

    public bool TryAddStew(StewInstance stew)
    {
        if (IsFull || stew == null) return false;

        bowls.Add(stew);
        OnBowlsChanged?.Invoke();
        return true;
    }

    public void RemoveStew(StewInstance stew)
    {
        if (bowls.Remove(stew))
            OnBowlsChanged?.Invoke();
    }

    /// <summary>Progression hook - unlocks one more shared bowl slot.</summary>
    public void UnlockSharedBowlSlot()
    {
        sharedCapacity++;
        OnBowlsChanged?.Invoke();
    }
}