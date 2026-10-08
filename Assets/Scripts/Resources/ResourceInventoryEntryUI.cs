using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>One read-only grid cell in the resource inventory: icon, name, count.</summary>
public class ResourceInventoryEntryUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text countText;
    public TMP_Text descriptionText;
    [SerializeField] private GameObject selectedHighlight;
    [Tooltip("Optional - small rarity icon on the ingredient (hidden for common ones).")]
    [SerializeField] private IngredientRarityBadge rarityBadge;

    private string description;

    public void Set(ResourceData data, int count)
    {
        if (icon != null) icon.sprite = data.icon;
        if (nameText != null) nameText.text = data.resourceName;
        if (countText != null) countText.text = "x" + count;
        description = data.description;
        if (rarityBadge != null) rarityBadge.Set(data);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (descriptionText != null)
        {
            descriptionText.text = description;
            selectedHighlight.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (descriptionText != null)
        {
            descriptionText.text = "";
            selectedHighlight.SetActive(false);
        }
    }
}
