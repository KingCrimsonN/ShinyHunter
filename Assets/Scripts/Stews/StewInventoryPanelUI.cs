using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Sub-panel opened from the brewing UI to browse the stew (bowl) inventory.
/// Always shows StewInventoryManager.MaxSlots slots in the grid: the first
/// Count are filled with a stew, the rest of the usable ones are vacant, and the
/// ones beyond the unlocked capacity are locked.
///
/// Lives on the persistent Overlay (CLAUDE.md #3) so it survives scene loads and
/// every host reaches it the same way: the brewing screen through
/// StewInventoryPanelUI.Instance, the tablet's inventory button by wiring its
/// OnClick straight to Show(). It is NOT a popup of its own - the host (tablet /
/// brewing) owns the player freeze and Escape - so it simply closes itself the
/// moment the player is no longer frozen (host closed) or a scene loads.
/// Put this component on an object that stays ACTIVE and give it a separate
/// Panel Root to show/hide.
/// </summary>
public class StewInventoryPanelUI : MonoBehaviour
{
    public static StewInventoryPanelUI Instance { get; private set; }

    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Transform gridParent;
    [Tooltip("The slot prefab - one is spawned per slot (6), each with filled / vacant / locked states.")]
    [SerializeField] private StewBowlEntryUI entryPrefab;
    [SerializeField] private Button closeButton;

    private readonly List<StewBowlEntryUI> spawned = new List<StewBowlEntryUI>();

    private void Awake()
    {
        // Only the first scene's Overlay survives; a duplicate's Awake still runs - leave the survivor's Instance alone.
        if (Instance != null && Instance != this) return;

        Instance = this;
        if (panelRoot != null) panelRoot.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Instance == this) Hide();
    }

    private void Update()
    {
        // Whoever opened this (tablet / brewing) froze the player; once they close and unfreeze, so do we.
        if (Instance == this && panelRoot != null && panelRoot.activeSelf
            && PlayerStateManager.Instance != null && !PlayerStateManager.Instance.IsFrozen)
            Hide();
    }

    public void Show()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void Refresh()
    {
        // Anything left in the grid at design time is only a preview of the look.
        foreach (Transform child in gridParent)
            Destroy(child.gameObject);
        spawned.Clear();

        var manager = StewInventoryManager.Instance;
        var bowls = manager.Bowls;

        for (int i = 0; i < manager.MaxSlots; i++)
        {
            var slot = Instantiate(entryPrefab, gridParent);
            slot.gameObject.SetActive(true);

            if (i < bowls.Count) slot.SetFilled(bowls[i]);
            else if (i < manager.Capacity) slot.SetVacant();
            else slot.SetLocked();

            spawned.Add(slot);
        }
    }
}
