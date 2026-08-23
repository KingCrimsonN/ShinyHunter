using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>One row in the inventory list: rarity-correct icon, name, rarity label, count.</summary>
public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    // [SerializeField] private TMP_Text rarityText;
    [SerializeField] private TMP_Text countText;

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

        // if (rarityText != null)
        // {
        //     rarityText.text = rarity.ToString();
        nameText.color = GetRarityColor(rarity);
        // }
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
}