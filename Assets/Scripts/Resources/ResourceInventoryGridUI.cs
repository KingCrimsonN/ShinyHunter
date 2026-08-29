using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds/rebuilds the resource grid from ResourceInventoryManager. Refreshes
/// on OnEnable rather than Start, so it picks up fresh data every time its
/// tab becomes visible in the combined inventory UI - not just the first time.
/// Drop this in as the Content object of your resources tab's ScrollView.
/// </summary>
public class ResourceInventoryGridUI : MonoBehaviour
{
    [SerializeField] private ResourceInventoryEntryUI entryPrefab;

    private readonly List<ResourceInventoryEntryUI> spawnedEntries = new List<ResourceInventoryEntryUI>();

    private void OnEnable()
    {
        ResourceInventoryManager.Instance.OnInventoryChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (ResourceInventoryManager.Instance != null)
            ResourceInventoryManager.Instance.OnInventoryChanged -= Refresh;
    }

    private void Refresh()
    {
        foreach (var entry in spawnedEntries)
            Destroy(entry.gameObject);
        spawnedEntries.Clear();

        foreach (var resource in ResourceInventoryManager.Instance.GetOrderedEntries())
        {
            var entry = Instantiate(entryPrefab, transform);
            entry.Set(resource, ResourceInventoryManager.Instance.GetCount(resource));
            spawnedEntries.Add(entry);
        }
    }

    /// <summary>Wire this to the Sort button's OnClick.</summary>
    public void OnSortButtonPressed()
    {
        ResourceInventoryManager.Instance.SortByRarityThenName();
    }
}
