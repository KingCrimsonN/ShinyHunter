using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>One row in the inventory list: rarity-correct icon, name, rarity label, count.</summary>
public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private Image frame;
    [SerializeField] private TMP_Text nameText;
    // [SerializeField] private TMP_Text rarityText;
    [SerializeField] private TMP_Text countText;
    [SerializeField] public TMP_Text descriptionText;

    [SerializeField] private Sprite[] rarityFrames; // normal, uncommon, rare, legendary

    [Tooltip("Shown when any of this stack will yield double resources (Ingredient Power, flagged at capture time) - e.g. a sparkle graphic layered on top of the icon.")]
    [SerializeField] private GameObject sparkleOverlay;

    private string description;

    [Header("Rarity Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color uncommonColor = new Color(0.3f, 0.8f, 0.3f);
    [SerializeField] private Color rareColor = new Color(0.3f, 0.5f, 1f);
    [SerializeField] private Color legendaryColor = new Color(1f, 0.65f, 0f);

    public void Set(CreatureData species, CreatureData.Rarity rarity, int count)
    {
        if (icon != null) icon.sprite = species.GetIcon(rarity);
        if (nameText != null) nameText.text = species.creatureName;
        if (countText != null) countText.text = "x" + count;
        description = species.description;

        // if (rarityText != null)
        // {
        //     rarityText.text = rarity.ToString();
        nameText.color = GetRarityColor(rarity);
        if (frame != null) frame.sprite = rarityFrames[(int)rarity];
        // }

        if (sparkleOverlay != null)
        {
            bool hasSparkle = InventoryManager.Instance != null && InventoryManager.Instance.GetSparkleCount(species, rarity) > 0;
            sparkleOverlay.SetActive(hasSparkle);
        }
    }

    private Color GetRarityColor(CreatureData.Rarity rarity)
    {
        switch (rarity)
        {
            case CreatureData.Rarity.Uncommon: return uncommonColor;
            case CreatureData.Rarity.Rare: return rareColor;
            case CreatureData.Rarity.Legendary: return legendaryColor;
            default: return normalColor;
        }
    }

    private void OnMouseEnter()
    {

    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (descriptionText != null)
        {
            descriptionText.text = description;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (descriptionText != null)
        {
            descriptionText.text = "";
        }
    }

}