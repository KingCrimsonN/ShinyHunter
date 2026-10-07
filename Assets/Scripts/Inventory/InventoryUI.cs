using UnityEngine;

/// <summary>
/// The critter inventory grid. Rebuilds whenever the inventory or the view
/// mode changes. Individual view (default): one tile per caught critter, so a
/// double-yield critter shows its own sparkle. Grouped view: one tile per
/// species+rarity with a count (the old layout). The mode is the shared
/// CritterInventoryView setting - see CritterViewToggleButton.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Transform listParent;
    [SerializeField] private InventorySlotUI slotPrefab;
    [SerializeField] private TMPro.TMP_Text descriptionText;

    private void OnEnable()
    {
        InventoryManager.Instance.OnInventoryChanged += Refresh;
        CritterInventoryView.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= Refresh;
        CritterInventoryView.Changed -= Refresh;
    }

    private void Refresh()
    {
        foreach (Transform child in listParent)
            Destroy(child.gameObject);

        foreach (var stack in CritterStack.Build(InventoryManager.Instance.Critters, CritterInventoryView.Grouped))
        {
            var slot = Instantiate(slotPrefab, listParent);
            slot.Set(stack);
            slot.descriptionText = descriptionText;
        }
    }
}
