using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Cauldron brewing popup: left = ingredient inventory (drag from here),
/// right = 8 cauldron slots (first unlockedSlotCount usable) + Brew button.
/// Opened externally (call Instance.Open()) from the cauldron's IInteractable,
/// same convention as the shop and transform station.
/// </summary>
public class BrewingStationUI : MonoBehaviour
{
    public static BrewingStationUI Instance { get; private set; }

    [Header("Popup")]
    [SerializeField] private GameObject popupRoot;
    [Tooltip("Shared floating icon shown while dragging. UI Image under this popup's Canvas, Raycast Target OFF, inactive by default.")]
    [SerializeField] private Image dragIconTemplate;

    [Header("Ingredient Inventory (left)")]
    [SerializeField] private Transform ingredientGridParent;
    [SerializeField] private BrewIngredientEntryUI ingredientEntryPrefab;

    [Header("Cauldron Slots (right)")]
    [Tooltip("Exactly 8, in order.")]
    [SerializeField] private BrewCauldronSlotUI[] cauldronSlots = new BrewCauldronSlotUI[8];
    [SerializeField] private int unlockedSlotCount = 4;

    [Header("Brew")]
    [SerializeField] private Button brewButton;
    [SerializeField] private TMP_Text brewWarningText;

    [Header("Sub-panels")]
    [SerializeField] private StewResultPopupUI resultPopup;
    [SerializeField] private StewInventoryPanelUI stewInventoryPanel;
    [SerializeField] private Button viewStewInventoryButton;

    [Header("Config")]
    [SerializeField] private StewCalculationConfig calculationConfig;
    [SerializeField] private StewVisualConfig visualConfig;

    private readonly ResourceData[] slotContents = new ResourceData[8];

    private void Awake()
    {
        Instance = this;

        BrewIngredientEntryUI.DragIcon = dragIconTemplate;
        if (dragIconTemplate != null) dragIconTemplate.gameObject.SetActive(false);
        if (popupRoot != null) popupRoot.SetActive(false);

        if (brewButton != null) brewButton.onClick.AddListener(OnBrewPressed);
        if (viewStewInventoryButton != null) viewStewInventoryButton.onClick.AddListener(() => stewInventoryPanel?.Show());

        for (int i = 0; i < cauldronSlots.Length; i++)
        {
            if (cauldronSlots[i] != null)
                cauldronSlots[i].Setup(i, this);
        }
    }

    public void Open()
    {
        System.Array.Clear(slotContents, 0, slotContents.Length);

        if (popupRoot != null) popupRoot.SetActive(true);
        PlayerStateManager.Instance.Freeze();

        RefreshAll();
    }

    private void OnEnable()
    {
        System.Array.Clear(slotContents, 0, slotContents.Length);

        if (popupRoot != null) popupRoot.SetActive(true);
        PlayerStateManager.Instance.Freeze();

        RefreshAll();
    }

    public void Close()
    {

        if (popupRoot != null) popupRoot.SetActive(false);
        PlayerStateManager.Instance.Unfreeze();
    }

    /// <summary>Progression hook - unlocks one of the 4 extra cauldron slots.</summary>
    public void UnlockSlot()
    {
        unlockedSlotCount = Mathf.Min(unlockedSlotCount + 1, cauldronSlots.Length);
        RefreshCauldronSlots();
    }

    /// <summary>Called by StewResultPopupUI once the player accepts/dumps a brewed stew.</summary>
    public void RefreshAfterResult()
    {
        RefreshBrewButton();
    }

    private void RefreshAll()
    {
        RefreshIngredientGrid();
        RefreshCauldronSlots();
        RefreshBrewButton();
    }

    private void RefreshIngredientGrid()
    {
        print("REFRESHING");
        foreach (Transform child in ingredientGridParent)
            Destroy(child.gameObject);

        foreach (var resource in ResourceInventoryManager.Instance.GetOrderedEntries())
        {
            int available = GetAvailableCount(resource);
            if (available <= 0) continue;

            var entry = Instantiate(ingredientEntryPrefab, ingredientGridParent);
            entry.Setup(resource, available);
        }
    }

    private void RefreshCauldronSlots()
    {
        for (int i = 0; i < cauldronSlots.Length; i++)
        {
            if (cauldronSlots[i] == null) continue;
            cauldronSlots[i].Display(slotContents[i], i < unlockedSlotCount);
        }
    }

    private void RefreshBrewButton()
    {
        int ingredientCount = CountUsedIngredients();
        bool bowlsFull = StewInventoryManager.Instance.IsFull;
        bool hasIngredients = ingredientCount > 0;

        if (brewButton != null)
            brewButton.interactable = hasIngredients && !bowlsFull;

        if (brewWarningText != null)
        {
            if (bowlsFull) brewWarningText.text = "All bowls are full - free one up before brewing.";
            else if (!hasIngredients) brewWarningText.text = "Add at least one ingredient.";
            else brewWarningText.text = string.Empty;
        }
    }

    private int CountUsedIngredients()
    {
        int count = 0;
        foreach (var r in slotContents) if (r != null) count++;
        return count;
    }

    /// <summary>Owned amount minus however many are already placed in cauldron slots.</summary>
    public int GetAvailableCount(ResourceData resource)
    {
        int owned = ResourceInventoryManager.Instance.GetCount(resource);
        int placed = 0;
        foreach (var r in slotContents) if (r == resource) placed++;
        return owned - placed;
    }

    /// <summary>Called by BrewCauldronSlotUI.OnDrop.</summary>
    public void PlaceIngredient(ResourceData resource, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= cauldronSlots.Length || slotIndex >= unlockedSlotCount) return;
        if (slotContents[slotIndex] != null) return; // occupied - clear it first
        if (GetAvailableCount(resource) <= 0) return;

        slotContents[slotIndex] = resource;
        RefreshIngredientGrid();
        RefreshCauldronSlots();
        RefreshBrewButton();
    }

    /// <summary>Called by BrewCauldronSlotUI.OnPointerClick.</summary>
    public void ClearSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slotContents.Length) return;

        slotContents[slotIndex] = null;
        RefreshIngredientGrid();
        RefreshCauldronSlots();
        RefreshBrewButton();
    }

    private void OnBrewPressed()
    {
        var ingredients = new List<ResourceData>();
        foreach (var r in slotContents) if (r != null) ingredients.Add(r);
        if (ingredients.Count == 0) return;

        // cauldronSlots.Length (the array's real size, NOT unlockedSlotCount) is
        // the "total capacity" the scent calculation divides by - filling all
        // your CURRENTLY unlocked slots always means the same thing regardless
        // of how many you've unlocked, and unlocking more slots doesn't retroactively
        // change how strong an already-brewed recipe would have smelled.
        var stew = StewCalculator.Calculate(ingredients, cauldronSlots.Length, calculationConfig);
        stew.icon = visualConfig != null ? visualConfig.GetFamilyIcon(stew.dominantFamily) : null;

        // Ingredients are spent the moment you brew, win or lose - dumping
        // the result later loses the stew's contents, not the raw ingredients again.
        foreach (var r in ingredients)
            ResourceInventoryManager.Instance.RemoveResource(r, 1);

        System.Array.Clear(slotContents, 0, slotContents.Length);
        RefreshAll();

        resultPopup.Show(stew);
    }
}