using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One cell in the CritterDex grid. Shows the species' Normal-rarity icon
/// (tinted dark as a silhouette if not yet caught at all), the dex number,
/// and a purely decorative frame - one fixed sprite regardless of rarity
/// (an earlier version swapped the frame per the highest rarity caught;
/// dropped, no need to portray rarity in the grid - see decision log), tinted
/// the same as the icon. Click selects it.
/// </summary>
public class CritterDexEntryUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text numberText;
    [SerializeField] private GameObject selectedHighlight;
    [Tooltip("Purely decorative - always the same sprite, just tinted like the icon. Set its sprite once in the prefab; not swapped by code.")]
    [SerializeField] private Image frame;

    [Header("Silhouette")]
    [SerializeField] private Color unlockedColor = Color.white;
    [SerializeField] private Color lockedColor = new Color(0.12f, 0.12f, 0.12f, 1f);

    private CreatureData species;
    private Action<CreatureData> onClicked;

    public CreatureData Species => species;

    public void Set(CreatureData species, int dexNumber, bool unlocked, Action<CreatureData> onClicked)
    {
        this.species = species;
        this.onClicked = onClicked;

        if (numberText != null) numberText.text = dexNumber.ToString("000");

        var tint = unlocked ? unlockedColor : lockedColor;

        if (iconImage != null)
        {
            iconImage.sprite = species.GetIcon(CreatureData.Rarity.Normal);
            iconImage.color = tint;
        }

        if (frame != null) frame.color = tint;
    }

    public void SetSelected(bool selected)
    {
        if (selectedHighlight != null) selectedHighlight.SetActive(selected);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        onClicked?.Invoke(species);
    }
}
