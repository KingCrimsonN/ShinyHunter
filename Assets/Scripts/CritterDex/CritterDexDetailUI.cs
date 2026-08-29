using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Detail panel for whichever species is selected in the grid: name,
/// description, scent stats, resource, and which rarities have been caught.
/// Locked (never-caught) species show "???" placeholders instead of real data,
/// matching the real Pokedex's "seen but not caught" behaviour.
/// </summary>
public class CritterDexDetailUI : MonoBehaviour
{
    [Header("Portrait")]
    [SerializeField] private Image portraitImage;

    [Header("Text")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Scents")]
    [SerializeField] private TMP_Text sweetText;
    [SerializeField] private TMP_Text freshText;
    [SerializeField] private TMP_Text putridText;
    [SerializeField] private TMP_Text metallicText;
    [SerializeField] private TMP_Text marineText;

    [Header("Resource")]
    [SerializeField] private Image resourceIcon;
    [SerializeField] private TMP_Text resourceNameText;

    [Header("Rarities Found")]
    [Tooltip("Exactly 4, in CreatureData.Rarity enum order: Normal, Uncommon, Rare, Legendary.")]
    [SerializeField] private CritterDexRarityBadgeUI[] rarityBadges;

    private CreatureData currentSpecies;

    public void Show(CreatureData species)
    {
        currentSpecies = species;
        if (species == null) return;

        bool unlocked = InventoryManager.Instance.GetTotalCount(species) > 0;

        if (portraitImage != null)
        {
            portraitImage.sprite = species.GetIcon(CreatureData.Rarity.Normal);
            portraitImage.color = unlocked ? Color.white : Color.black;
        }

        if (!unlocked)
        {
            ShowLockedPlaceholder(species);
            return;
        }

        if (nameText != null) nameText.text = species.creatureName;
        if (descriptionText != null) descriptionText.text = species.description;

        SetScentTexts(species);

        var resource = species.GetResource(CreatureData.Rarity.Normal);
        if (resourceIcon != null)
        {
            resourceIcon.sprite = resource != null ? resource.icon : null;
            resourceIcon.enabled = resource != null && resource.icon != null;
        }
        if (resourceNameText != null) resourceNameText.text = resource != null ? resource.resourceName : "-";

        RefreshRarityBadges(species);
    }

    private void ShowLockedPlaceholder(CreatureData species)
    {
        if (nameText != null) nameText.text = "???";
        if (descriptionText != null) descriptionText.text = "Not yet discovered.";

        if (sweetText != null) sweetText.text = "-";
        if (freshText != null) freshText.text = "-";
        if (putridText != null) putridText.text = "-";
        if (metallicText != null) metallicText.text = "-";
        if (marineText != null) marineText.text = "-";

        if (resourceIcon != null) resourceIcon.enabled = false;
        if (resourceNameText != null) resourceNameText.text = "-";

        if (rarityBadges != null)
        {
            for (int i = 0; i < rarityBadges.Length; i++)
                rarityBadges[i].Set(species, (CreatureData.Rarity)i, 0);
        }
    }

    private void SetScentTexts(CreatureData species)
    {
        var resource = species.GetResource(CreatureData.Rarity.Normal);
        if (sweetText != null) sweetText.text = resource != null ? resource.sweetScent.ToString("0.#") : "-";
        if (freshText != null) freshText.text = resource != null ? resource.freshScent.ToString("0.#") : "-";
        if (putridText != null) putridText.text = resource != null ? resource.putridScent.ToString("0.#") : "-";
        if (metallicText != null) metallicText.text = resource != null ? resource.metallicScent.ToString("0.#") : "-";
        if (marineText != null) marineText.text = resource != null ? resource.marineScent.ToString("0.#") : "-";
    }

    private void RefreshRarityBadges(CreatureData species)
    {
        if (rarityBadges == null) return;

        for (int i = 0; i < rarityBadges.Length; i++)
        {
            var rarity = (CreatureData.Rarity)i;
            int count = InventoryManager.Instance.GetCount(species, rarity);
            rarityBadges[i].Set(species, rarity, count);
        }
    }
}
