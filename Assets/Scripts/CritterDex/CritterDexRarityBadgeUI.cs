using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One "have I caught this rarity" chip shown in the detail panel's
/// rarities-found row (4 of these: Normal/Uncommon/Rare/Legendary).
/// </summary>
public class CritterDexRarityBadgeUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text label;
    [SerializeField] private TMP_Text countText;

    [SerializeField] private Color ownedColor = Color.white;
    [SerializeField] private Color unownedColor = new Color(0.2f, 0.2f, 0.2f, 1f);

    public void Set(CreatureData species, CreatureData.Rarity rarity, int count)
    {
        bool owned = count > 0;

        if (icon != null)
        {
            icon.sprite = species.GetIcon(rarity);
            icon.color = owned ? ownedColor : unownedColor;
        }

        if (label != null) label.text = rarity.ToString();
        if (countText != null) countText.text = owned ? "x" + count : "-";
    }
}