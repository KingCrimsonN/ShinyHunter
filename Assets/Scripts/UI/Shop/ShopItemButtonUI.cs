using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>One grid cell in the shop: icon, name, price per unit, how many the player already owns. Click opens the purchase panel and stays highlighted while it's open; hover shows a separate highlight.</summary>
public class ShopItemButtonUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private GameObject hoverHighlight;
    [SerializeField] private TMP_Text onHandText;
    [Tooltip("Stays active while this item's purchase panel is open - separate from hoverHighlight, which is only active while the cursor is directly over the button.")]
    [SerializeField] private GameObject selectedHighlight;

    private ShopItemEntry entry;
    private Action<ShopItemEntry> onClicked;

    public ShopItemEntry Entry => entry;

    public void Set(ShopItemEntry entry, int ownedCount, Action<ShopItemEntry> onClicked)
    {
        this.entry = entry;
        this.onClicked = onClicked;

        if (icon != null) icon.sprite = entry.item.icon;
        if (nameText != null) nameText.text = entry.item.toolName;
        if (priceText != null) priceText.text = entry.pricePerUnit.ToString("N0");
        if (onHandText != null) onHandText.text = ownedCount > 0 ? $"x{ownedCount}" : string.Empty;

        if (hoverHighlight != null) hoverHighlight.SetActive(false);
        if (selectedHighlight != null) selectedHighlight.SetActive(false); // ShopCatalogUI re-applies this right after Set() if this entry is the one currently open
    }

    public void SetSelected(bool selected)
    {
        if (selectedHighlight != null) selectedHighlight.SetActive(selected);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        onClicked?.Invoke(entry);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverHighlight != null) hoverHighlight.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoverHighlight != null) hoverHighlight.SetActive(false);
    }
}
