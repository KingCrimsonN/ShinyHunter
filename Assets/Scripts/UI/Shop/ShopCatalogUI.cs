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
/// purchase panel when an item is clicked. Fully rebuilds on OnEnable AND
/// whenever ToolInventoryManager.OnInventoryChanged fires while the shop is
/// open (same "grid rebuilds fully on refresh" convention as every other
/// grid UI, see CLAUDE.md #8) - this is what keeps each button's "on hand"
/// count live as purchases happen, not just correct the next time the shop
/// is reopened.
///
/// A rebuild destroys/recreates every button, so the currently-selected
/// item's highlight is re-applied to whichever NEW button instance matches
/// it afterward - same pattern as CritterDexGridUI.
/// </summary>
public class ShopCatalogUI : MonoBehaviour
{
    [SerializeField] private List<ShopItemEntry> catalog = new List<ShopItemEntry>();
    [SerializeField] private Transform gridParent;
    [SerializeField] private ShopItemButtonUI itemButtonPrefab;
    [SerializeField] private ShopPurchasePanelUI purchasePanel;

    private readonly List<ShopItemButtonUI> spawned = new List<ShopItemButtonUI>();
    private ShopItemButtonUI selectedButton;
    private ShopItemEntry selectedEntry;

    private void OnEnable()
    {
        BuildGrid();

        if (purchasePanel != null) purchasePanel.OnClosed += HandlePurchasePanelClosed;
        if (ToolInventoryManager.Instance != null) ToolInventoryManager.Instance.OnInventoryChanged += BuildGrid;
    }

    private void OnDisable()
    {
        if (purchasePanel != null) purchasePanel.OnClosed -= HandlePurchasePanelClosed;
        if (ToolInventoryManager.Instance != null) ToolInventoryManager.Instance.OnInventoryChanged -= BuildGrid;
    }

    private void BuildGrid()
    {
        foreach (var button in spawned)
            if (button != null) Destroy(button.gameObject);
        spawned.Clear();

        ShopItemButtonUI matchedButton = null;

        foreach (var entry in catalog)
        {
            if (entry.item == null) continue;

            int owned = ToolInventoryManager.Instance != null ? ToolInventoryManager.Instance.GetTotalCount(entry.item) : 0;

            var button = Instantiate(itemButtonPrefab, gridParent);
            button.Set(entry, owned, OnItemClicked);
            spawned.Add(button);

            if (entry == selectedEntry) matchedButton = button;
        }

        selectedButton = matchedButton;
        if (selectedButton != null) selectedButton.SetSelected(true);
    }

    private void OnItemClicked(ShopItemEntry entry)
    {
        if (selectedButton != null) selectedButton.SetSelected(false);

        selectedEntry = entry;
        selectedButton = spawned.Find(b => b.Entry == entry);
        if (selectedButton != null) selectedButton.SetSelected(true);

        if (purchasePanel != null)
            purchasePanel.Show(entry);
    }

    private void HandlePurchasePanelClosed()
    {
        if (selectedButton != null) selectedButton.SetSelected(false);
        selectedButton = null;
        selectedEntry = null;
    }
}
