using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One cell in the CritterDex grid. Shows the species' Normal-rarity icon,
/// tinted dark as a silhouette if not yet caught. Click selects it.
/// </summary>
public class CritterDexEntryUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text numberText;
    [SerializeField] private GameObject selectedHighlight;

    [Header("Silhouette")]
    [SerializeField] private Color unlockedColor = Color.white;
    [SerializeField] private Color lockedColor = new Color(0.12f, 0.12f, 0.12f, 1f);

    private CreatureData species;
    private Action<CreatureData> onClicked;

    public void Set(CreatureData species, int dexNumber, bool unlocked, Action<CreatureData> onClicked)
    {
        this.species = species;
        this.onClicked = onClicked;

        if (numberText != null) numberText.text = dexNumber.ToString("000");

        if (iconImage != null)
        {
            iconImage.sprite = species.GetIcon(CreatureData.Rarity.Normal);
            iconImage.color = unlocked ? unlockedColor : lockedColor;
        }
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