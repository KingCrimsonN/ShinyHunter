using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One rarity-select button on a critter page (4 of these: Normal/Uncommon/
/// Rare/Legendary). Clicking one shows that rarity's portrait/ingredient. The
/// icon follows CritterDexIcons' three states, so a rarity you haven't caught
/// yet shows the species' locked silhouette, and a species you've never caught
/// shows the solid undiscovered look. Only clickable once the species itself
/// has been caught (an undiscovered page has nothing to switch between).
/// </summary>
public class CritterDexRarityBadgeUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text label;
    [SerializeField] private GameObject selectedHighlight;
    [SerializeField] private CritterDexIconStyle iconStyle = new CritterDexIconStyle();

    private CreatureData.Rarity rarity;
    private Action<CreatureData.Rarity> onClicked;
    private bool clickable;

    public void Set(CreatureData species, CreatureData.Rarity rarity, bool selected, Action<CreatureData.Rarity> onClicked)
    {
        this.rarity = rarity;
        this.onClicked = onClicked;
        clickable = species != null && onClicked != null && CritterDexIcons.IsSpeciesDiscovered(species);

        CritterDexIcons.Apply(icon, species, rarity, iconStyle);

        if (label != null) label.text = rarity.ToString();
        if (selectedHighlight != null) selectedHighlight.SetActive(selected && clickable);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (clickable) onClicked(rarity);
    }
}
