using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

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
    [SerializeField] private TMP_Text descriptionText;

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
    [SerializeField] private Animator shopKeeperPortrait;
    // [SerializeField] private Sprite shopKeeperHappy;

    private ShopItemEntry currentEntry;
    private int quantity = 1;

    /// <summary>Fires whenever this panel closes - ShopCatalogUI uses this to drop the corresponding item button's persistent selection highlight.</summary>
    public event System.Action OnClosed;

    private void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);

        if (increaseHold != null) increaseHold.OnRepeat += () => ChangeQuantity(1);
        if (decreaseHold != null) decreaseHold.OnRepeat += () => ChangeQuantity(-1);
        if (purchaseButton != null) purchaseButton.onClick.AddListener(Purchase);
        if (cancelButton != null) cancelButton.onClick.AddListener(Hide);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Purchase();
        }
    }

    public void Show(ShopItemEntry entry)
    {
        currentEntry = entry;
        quantity = 1;

        if (icon != null) icon.sprite = entry.item.icon;
        if (nameText != null) nameText.text = entry.item.toolName;
        if (descriptionText != null) descriptionText.text = entry.item.description;
        if (feedbackText != null) feedbackText.text = string.Empty;

        RefreshDisplay();

        if (panelRoot != null) panelRoot.SetActive(true);
    }

    /// <summary>Wire to a Cancel button's OnClick - also called internally by Purchase() once it completes.</summary>
    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        currentEntry = null;
        OnClosed?.Invoke();
    }

    /// <summary>
    /// Covers closing the WHOLE shop (Shop.CloseShop deactivating shopUI,
    /// which this object is a descendant of) rather than just this panel -
    /// Hide() only runs for the normal cancel/purchase path, where panelRoot
    /// toggles but this component's own object stays active, so that path
    /// alone wouldn't catch "the whole shop just closed while a purchase
    /// panel was open". Firing OnClosed again if Hide() already ran is
    /// harmless - currentEntry is already null and ShopCatalogUI's handler
    /// is idempotent. See decision log (same pattern as CritterDexDetailUI).
    /// </summary>
    private void OnDisable()
    {
        currentEntry = null;
        OnClosed?.Invoke();
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

        // Sprite temp = shopKeeperPortrait.sprite;
        // DOTween.Sequence().SetUpdate(true)
        //     .AppendCallback(() => shopKeeperPortrait.sprite = shopKeeperHappy)
        //     .AppendCallback(() => shopKeeperPortrait.
        //     .AppendInterval(0.5f)
        //     .AppendCallback(() => shopKeeperPortrait.sprite = temp);
        shopKeeperPortrait.SetTrigger("buy");

        if (moneyDisplay != null)
            moneyDisplay.AnimateFrom(moneyBeforePurchase);

        Hide();
    }
}