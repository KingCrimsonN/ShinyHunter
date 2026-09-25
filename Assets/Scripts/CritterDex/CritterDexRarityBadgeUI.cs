using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One rarity-select button in the detail panel (4 of these: Normal/Uncommon/
/// Rare/Legendary). Clicking one tells CritterDexDetailUI to show that
/// rarity's portrait/ingredient drop. Silhouetted (dark) if that SPECIFIC
/// rarity hasn't been caught yet, even when the species is unlocked at a
/// different rarity - same silhouette convention as the grid entries.
/// Non-clickable (species null) while nothing is selected at all.
/// </summary>
public class CritterDexRarityBadgeUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text label;
    [SerializeField] private GameObject selectedHighlight;

    [SerializeField] private Color ownedColor = Color.white;
    [SerializeField] private Color unownedColor = new Color(0.2f, 0.2f, 0.2f, 1f);

    private CreatureData.Rarity rarity;
    private Action<CreatureData.Rarity> onClicked;
    private bool clickable;

    public void Set(CreatureData species, CreatureData.Rarity rarity, bool owned, bool selected, Action<CreatureData.Rarity> onClicked)
    {
        this.rarity = rarity;
        this.onClicked = onClicked;
        clickable = species != null && onClicked != null;

        if (icon != null)
        {
            icon.sprite = species != null ? species.GetIcon(rarity) : null;
            icon.color = owned ? ownedColor : unownedColor;
            icon.enabled = icon.sprite != null;
        }

        if (label != null) label.text = rarity.ToString();

        if (selectedHighlight != null) selectedHighlight.SetActive(selected);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (clickable) onClicked(rarity);
    }
}
