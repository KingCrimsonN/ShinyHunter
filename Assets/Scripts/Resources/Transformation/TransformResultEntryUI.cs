using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>One ingredient in the transform result pop-up's grid: icon, name, how many you got, and its rarity icon.</summary>
public class TransformResultEntryUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private GameObject selectedHighlight;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private IngredientRarityBadge rarityBadge;

    public void Set(ResourceData resource, int amount)
    {
        if (icon != null) icon.sprite = resource.icon;
        if (nameText != null) nameText.text = resource.resourceName;
        if (countText != null) countText.text = "x" + amount;
        if (rarityBadge != null) rarityBadge.Set(resource);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (selectedHighlight != null) selectedHighlight.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (selectedHighlight != null) selectedHighlight.SetActive(false);

    }
}
