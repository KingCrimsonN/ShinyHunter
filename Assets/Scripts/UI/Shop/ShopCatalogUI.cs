using System.Collections.Generic;
using UnityEngine;

/// <summary>One purchasable line in the shop's catalog: which tool, and its per-unit price.</summary>
[System.Serializable]
public class ShopItemEntry
{
    public ToolData item;
    public int pricePerUnit;
}

/// <summary>
/// Populates the shop's item grid from a fixed catalog and opens the
/// purchase panel when an item is clicked. Rebuilds on every OnEnable,
/// since Shop.cs just toggles this panel's GameObject rather than
/// re-instantiating it - matches the "refresh on enable" pattern used
/// elsewhere in this project's popups.
/// </summary>
public class ShopCatalogUI : MonoBehaviour
{
    [SerializeField] private List<ShopItemEntry> catalog = new List<ShopItemEntry>();
    [SerializeField] private Transform gridParent;
    [SerializeField] private ShopItemButtonUI itemButtonPrefab;
    [SerializeField] private ShopPurchasePanelUI purchasePanel;

    private readonly List<ShopItemButtonUI> spawned = new List<ShopItemButtonUI>();

    private void OnEnable()
    {
        BuildGrid();
    }

    private void BuildGrid()
    {
        foreach (var button in spawned)
            if (button != null) Destroy(button.gameObject);
        spawned.Clear();

        foreach (var entry in catalog)
        {
            if (entry.item == null) continue;

            var button = Instantiate(itemButtonPrefab, gridParent);
            button.Set(entry, OnItemClicked);
            spawned.Add(button);
        }
    }

    private void OnItemClicked(ShopItemEntry entry)
    {
        if (purchasePanel != null)
            purchasePanel.Show(entry);
    }
}