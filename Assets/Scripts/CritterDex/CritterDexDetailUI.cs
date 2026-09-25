using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Detail panel for whichever species is selected in the grid. Two levels:
/// SPECIES-level info (family, name, description, favorite/hated scent)
/// never changes with the rarity buttons; RARITY-level info (portrait,
/// ingredient drop) does, driven by the 4 CritterDexRarityBadgeUI buttons -
/// clicking one shows THAT rarity's icon/ingredient, silhouetted if that
/// specific rarity hasn't been caught yet (a species unlocked at Normal but
/// never seen as Legendary still silhouettes the Legendary tab). A totally
/// locked (never-caught-at-all) species shows "???" everywhere instead,
/// matching the real Pokedex's "not yet discovered" behaviour.
/// </summary>
public class CritterDexDetailUI : MonoBehaviour
{
    [Header("Portrait (selected rarity)")]
    [SerializeField] private Image portraitImage;

    [Header("Species Info")]
    [SerializeField] private TMP_Text familyText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text favoriteScentText;
    [SerializeField] private TMP_Text hatedScentText;

    [Header("Ingredient Drop (selected rarity)")]
    [SerializeField] private Image resourceIcon;
    [SerializeField] private TMP_Text resourceNameText;

    [Header("Rarity Select")]
    [Tooltip("Exactly 4, in CreatureData.Rarity enum order: Normal, Uncommon, Rare, Legendary.")]
    [SerializeField] private CritterDexRarityBadgeUI[] rarityBadges;

    [Header("Silhouette")]
    [SerializeField] private Color unlockedColor = Color.white;
    [SerializeField] private Color lockedColor = new Color(0.12f, 0.12f, 0.12f, 1f);

    private CreatureData currentSpecies;
    private CreatureData.Rarity selectedRarity;

    /// <summary>Species-selection entry point - wired to CritterDexGridUI.OnSpeciesSelected by CritterDexUI. null = nothing selectable (empty/filtered-out grid).</summary>
    public void Show(CreatureData species)
    {
        gameObject.SetActive(true);
        currentSpecies = species;
        selectedRarity = CreatureData.Rarity.Normal; // always land back on Normal for a freshly-selected species

        if (species == null)
        {
            ShowNothing();
            return;
        }

        bool speciesUnlocked = InventoryManager.Instance.HasEverCaptured(species); // survives the species later being transformed away entirely
        if (!speciesUnlocked)
        {
            ShowLockedPlaceholder(species);
            return;
        }

        if (familyText != null) familyText.text = StewDisplayUtil.FormatFamily(species.family);
        if (nameText != null) nameText.text = species.creatureName;
        if (descriptionText != null) descriptionText.text = species.description;
        if (favoriteScentText != null) favoriteScentText.text = species.favoriteScent.ToString();
        if (hatedScentText != null) hatedScentText.text = species.hatedScent.ToString();

        RefreshRarityBadges();
        ShowSelectedRarity();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        currentSpecies = null;
        ShowNothing();
    }

    /// <summary>Called when a rarity button is clicked - only the rarity-level half of the panel changes.</summary>
    private void SelectRarity(CreatureData.Rarity rarity)
    {
        selectedRarity = rarity;
        ShowSelectedRarity();
        RefreshRarityBadges(); // re-highlight which badge is now selected
    }

    private void ShowSelectedRarity()
    {
        if (currentSpecies == null) return;

        bool rarityOwned = InventoryManager.Instance.HasEverCaptured(currentSpecies, selectedRarity);

        if (portraitImage != null)
        {
            portraitImage.sprite = currentSpecies.GetIcon(selectedRarity);
            portraitImage.color = rarityOwned ? unlockedColor : lockedColor;
            portraitImage.enabled = portraitImage.sprite != null;
        }

        var resource = currentSpecies.GetResource(selectedRarity);
        if (resourceIcon != null)
        {
            resourceIcon.sprite = resource != null ? resource.icon : null;
            resourceIcon.color = rarityOwned ? unlockedColor : lockedColor;
            resourceIcon.enabled = resourceIcon.sprite != null;
        }
        if (resourceNameText != null) resourceNameText.text = resource != null ? (rarityOwned ? resource.resourceName : "???") : "-";
    }

    private void RefreshRarityBadges()
    {
        if (rarityBadges == null || currentSpecies == null) return;

        for (int i = 0; i < rarityBadges.Length; i++)
        {
            var rarity = (CreatureData.Rarity)i;
            bool owned = InventoryManager.Instance.HasEverCaptured(currentSpecies, rarity);
            rarityBadges[i].Set(currentSpecies, rarity, owned, rarity == selectedRarity, SelectRarity);
        }
    }

    public void ShowNothing()
    {
        if (portraitImage != null) portraitImage.enabled = false;
        if (familyText != null) familyText.text = "-";
        if (nameText != null) nameText.text = "-";
        if (descriptionText != null) descriptionText.text = "";
        if (favoriteScentText != null) favoriteScentText.text = "-";
        if (hatedScentText != null) hatedScentText.text = "-";
        if (resourceIcon != null) resourceIcon.enabled = false;
        if (resourceNameText != null) resourceNameText.text = "-";
        ClearRarityBadges();
    }

    private void ShowLockedPlaceholder(CreatureData species)
    {
        if (portraitImage != null) portraitImage.enabled = false;

        if (familyText != null) familyText.text = "???";
        if (nameText != null) nameText.text = "???";
        if (descriptionText != null) descriptionText.text = "Not yet discovered.";
        if (favoriteScentText != null) favoriteScentText.text = "???";
        if (hatedScentText != null) hatedScentText.text = "???";

        if (resourceIcon != null) resourceIcon.enabled = false;
        if (resourceNameText != null) resourceNameText.text = "???";

        ClearRarityBadges(); // fully locked - no rarity has been seen, so there's nothing to select yet
    }

    private void ClearRarityBadges()
    {
        if (rarityBadges == null) return;
        foreach (var badge in rarityBadges)
            badge.Set(null, CreatureData.Rarity.Normal, false, false, null);
    }
}
