using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One button in the index page's grid: the critter's Normal-slot icon and its
/// dex number. The icon follows CritterDexIcons' three states - caught: real
/// icon; species seen but Normal not caught yet: locked silhouette; species
/// never caught: solid undiscovered look. Click flips the book to its page.
/// </summary>
public class CritterDexEntryUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text numberText;
    [SerializeField] private CritterDexIconStyle iconStyle = new CritterDexIconStyle();

    private CreatureData species;
    private Action<CreatureData> onClicked;

    public CreatureData Species => species;

    public void Set(CreatureData species, int dexNumber, Action<CreatureData> onClicked)
    {
        this.species = species;
        this.onClicked = onClicked;

        if (numberText != null) numberText.text = dexNumber.ToString("000");

        CritterDexIcons.Apply(iconImage, species, CreatureData.Rarity.Normal, iconStyle);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        onClicked?.Invoke(species);
    }
}
