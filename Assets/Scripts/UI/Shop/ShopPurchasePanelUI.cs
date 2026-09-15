using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quantity-selection sub-panel shown after clicking an item in the shop
/// grid. Purchase deducts money and adds to ToolInventoryManager. If the
/// inventory can't fit everything (e.g. slots full), the unfit portion is
/// automatically refunded - the player should never pay for items they
/// didn't actually receive.
/// </summary>
public class ShopPurchasePanelUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private TMP_Text totalCostText;

    [Header("Quantity buttons (hold-to-accelerate)")]
    [SerializeField] private HoldButtonUI increaseHold;
    [SerializeField] private HoldButtonUI decreaseHold;

    [SerializeField] private Button purchaseButton;
    [SerializeField] private Button cancelButton;
    [Tooltip("Shown briefly for purchase feedback (not enough money, partial refund, etc).")]
    [SerializeField] private TMP_Text feedbackText;

    [Header("Affordability")]
    [SerializeField] private Color affordableColor = Color.white;
    [SerializeField] private Color unaffordableColor = Color.red;

    [Header("Money display (shop screen total, shown top-right)")]
    [SerializeField] private ShopMoneyDisplayUI moneyDisplay;

    private ShopItemEntry currentEntry;
    private int quantity = 1;

    private void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);

        if (increaseHold != null) increaseHold.OnRepeat += () => ChangeQuantity(1);
        if (decreaseHold != null) decreaseHold.OnRepeat += () => ChangeQuantity(-1);
        if (purchaseButton != null) purchaseButton.onClick.AddListener(Purchase);
        if (cancelButton != null) cancelButton.onClick.AddListener(Hide);
    }

    public void Show(ShopItemEntry entry)
    {
        currentEntry = entry;
        quantity = 1;

        if (icon != null) icon.sprite = entry.item.icon;
        if (nameText != null) nameText.text = entry.item.toolName;
        if (feedbackText != null) feedbackText.text = string.Empty;

        RefreshDisplay();

        if (panelRoot != null) panelRoot.SetActive(true);
    }

    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        currentEntry = null;
    }

    private void ChangeQuantity(int delta)
    {
        if (currentEntry == null) return;

        int maxQuantity = Mathf.Max(1, currentEntry.item.maxStack);
        quantity = Mathf.Clamp(quantity + delta, 1, maxQuantity);

        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        if (currentEntry == null) return;

        int totalCost = quantity * currentEntry.pricePerUnit;

        if (quantityText != null) quantityText.text = quantity.ToString();

        if (totalCostText != null)
        {
            totalCostText.text = totalCost.ToString("N0");
            bool canAfford = MoneyManager.Instance.GetCurrentMoney() >= totalCost;
            totalCostText.color = canAfford ? affordableColor : unaffordableColor;
        }
    }

    private void Purchase()
    {
        if (currentEntry == null) return;

        int totalCost = quantity * currentEntry.pricePerUnit;
        int moneyBeforePurchase = MoneyManager.Instance.GetCurrentMoney();

        if (!MoneyManager.Instance.TrySpendMoney(totalCost))
        {
            if (feedbackText != null) feedbackText.text = "Not enough money!";
            return;
        }

        int leftover = ToolInventoryManager.Instance.AddTool(currentEntry.item, quantity);

        if (leftover > 0)
        {
            MoneyManager.Instance.AddMoney(leftover * currentEntry.pricePerUnit);
            if (feedbackText != null) feedbackText.text = $"Inventory full - refunded {leftover}.";
        }

        if (moneyDisplay != null)
            moneyDisplay.AnimateFrom(moneyBeforePurchase);

        Hide();
    }
}