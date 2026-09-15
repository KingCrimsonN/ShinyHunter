using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>One grid cell in the shop: icon, name, price per unit. Click opens the purchase panel; hover shows a highlight.</summary>
public class ShopItemButtonUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private GameObject hoverHighlight;

    private ShopItemEntry entry;
    private Action<ShopItemEntry> onClicked;

    public void Set(ShopItemEntry entry, Action<ShopItemEntry> onClicked)
    {
        this.entry = entry;
        this.onClicked = onClicked;

        if (icon != null) icon.sprite = entry.item.icon;
        if (nameText != null) nameText.text = entry.item.toolName;
        if (priceText != null) priceText.text = entry.pricePerUnit.ToString("N0") + " ea.";

        if (hoverHighlight != null) hoverHighlight.SetActive(false);
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