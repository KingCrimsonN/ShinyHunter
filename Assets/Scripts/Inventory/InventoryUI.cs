using UnityEngine;

/// <summary>
/// Rebuilds a simple list of "icon / name / count" rows whenever the
/// inventory changes. Attach to a UI panel with a vertical layout group
/// as listParent, and assign an InventorySlotUI prefab.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Transform listParent;
    [SerializeField] private InventorySlotUI slotPrefab;

    private void OnEnable()
    {
        InventoryManager.Instance.OnInventoryChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= Refresh;
    }

    private void Refresh()
    {
        foreach (Transform child in listParent)
            Destroy(child.gameObject);

        foreach (var kvp in InventoryManager.Instance.GetAll())
        {
            var slot = Instantiate(slotPrefab, listParent);
            slot.Set(kvp.Key, kvp.Value);
        }
    }
}
