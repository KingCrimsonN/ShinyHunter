using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Sub-panel opened from the brewing UI to browse the stew (bowl) inventory.</summary>
public class StewInventoryPanelUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Transform gridParent;
    [SerializeField] private StewBowlEntryUI entryPrefab;
    [SerializeField] private Button closeButton;

    private readonly List<StewBowlEntryUI> spawned = new List<StewBowlEntryUI>();

    private void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
    }

    public void Show()
    {
        print("SHOWING");
        if (panelRoot != null) panelRoot.SetActive(true);
        Refresh();

    }

    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void Refresh()
    {
        foreach (Transform child in gridParent)
            Destroy(child.gameObject);

        foreach (var entry in spawned)
            if (entry != null) Destroy(entry.gameObject);
        spawned.Clear();

        foreach (var stew in StewInventoryManager.Instance.Bowls)
        {
            var entry = Instantiate(entryPrefab, gridParent);
            entry.Set(stew);
            spawned.Add(entry);
        }
    }
}