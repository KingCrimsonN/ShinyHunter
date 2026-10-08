using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// The content of one CRITTER PAGE in the book. It is no longer a pop-up that
/// activates/deactivates itself - CritterDexUI (the book) shows and hides the
/// whole page object and calls Show(species) when it turns to it.
///
/// Two levels: SPECIES-level info (family sticker, name, description,
/// favorite/hated scent) is fixed for the page; RARITY-level info (portrait,
/// ingredient drop) follows the 4 CritterDexRarityBadgeUI buttons. Icons use
/// CritterDexIcons' three states (caught / locked silhouette / undiscovered).
/// A never-caught species shows "???" for all text, matching the real
/// Pokedex's "not yet discovered" behaviour.
///
/// Clicking the ingredient box opens a small pop-up (CritterDexIngredientPopupUI)
/// with its name, description and scent stats - only for a rarity you've
/// actually caught.
/// </summary>
public class CritterDexDetailUI : MonoBehaviour
{
    [Header("Portrait (selected rarity)")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private CritterDexIconStyle portraitStyle = new CritterDexIconStyle();

    [Header("Species Info")]
    [SerializeField] private TMP_Text familyText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text noteText;
    [SerializeField] private TMP_Text favoriteScentText;
    [SerializeField] private TMP_Text hatedScentText;
    [SerializeField] private CritterFrames critterFrames;
    [SerializeField] private Image familyFrame;
    [SerializeField] private Image familyBadge;

    [Header("Ingredient Drop (selected rarity)")]
    [SerializeField] private Image resourceIcon;
    [SerializeField] private TMP_Text resourceNameText;
    [Tooltip("Optional - small rarity icon on the ingredient (hidden for common ones, and while that rarity is undiscovered).")]
    [SerializeField] private IngredientRarityBadge resourceRarityBadge;
    [Tooltip("The clickable ingredient box (usually the object holding Resource Icon). Clicking it opens the pop-up below. Optional - leave empty to disable the pop-up.")]
    [SerializeField] private Button ingredientButton;
    [SerializeField] private CritterDexIngredientPopupUI ingredientPopup;
    [Tooltip("Tint for the ingredient icon of a rarity that hasn't been caught yet. (There's no locked ingredient art - only critters have a locked variant.)")]
    [SerializeField] private Color ingredientLockedColor = new Color(0.12f, 0.12f, 0.12f, 1f);

    [Header("Rarity Select")]
    [Tooltip("Exactly 4, in CreatureData.Rarity enum order: Normal, Uncommon, Rare, Legendary.")]
    [SerializeField] private CritterDexRarityBadgeUI[] rarityBadges;

    private CreatureData currentSpecies;
    private CreatureData.Rarity selectedRarity;
    private ResourceData currentResource;

    private void Awake()
    {
        if (ingredientButton != null) ingredientButton.onClick.AddListener(OnIngredientClicked);
    }

    /// <summary>Called by the book when it turns to this species' page.</summary>
    public void Show(CreatureData species)
    {
        currentSpecies = species;
        selectedRarity = CreatureData.Rarity.Normal; // every freshly-opened page starts on Normal
        CloseIngredientPopup();

        if (species == null)
        {
            ShowNothing();
            return;
        }

        SetFamilyFrame(species);

        if (!CritterDexIcons.IsSpeciesDiscovered(species))
        {
            ShowUndiscovered(species);
            return;
        }

        if (nameText != null) nameText.text = species.creatureName;
        if (descriptionText != null) descriptionText.text = species.description;
        if (noteText != null) noteText.text = species.note;
        if (favoriteScentText != null) favoriteScentText.text = species.favoriteScent.ToString();
        if (hatedScentText != null) hatedScentText.text = species.hatedScent.ToString();

        RefreshRarityBadges();
        ShowSelectedRarity();
    }

    /// <summary>Called by the book when it leaves for the index page, so nothing stale (or an open pop-up) is left on the hidden page.</summary>
    public void Clear()
    {
        currentSpecies = null;
        CloseIngredientPopup();
        ShowNothing();
    }

    /// <summary>Rarity button clicked - only the rarity-level half of the page changes.</summary>
    private void SelectRarity(CreatureData.Rarity rarity)
    {
        selectedRarity = rarity;
        CloseIngredientPopup(); // the pop-up belongs to one specific ingredient
        ShowSelectedRarity();
        RefreshRarityBadges(); // re-highlight which badge is now selected
    }

    private void ShowSelectedRarity()
    {
        if (currentSpecies == null) return;

        bool rarityOwned = CritterDexIcons.IsRarityDiscovered(currentSpecies, selectedRarity);
        CritterDexIcons.Apply(portraitImage, currentSpecies, selectedRarity, portraitStyle);

        currentResource = currentSpecies.GetResource(selectedRarity);
        if (resourceIcon != null)
        {
            resourceIcon.sprite = currentResource != null ? currentResource.icon : null;
            resourceIcon.color = rarityOwned ? Color.white : ingredientLockedColor;
            resourceIcon.enabled = resourceIcon.sprite != null;
        }
        if (resourceNameText != null) resourceNameText.text = currentResource != null ? (rarityOwned ? currentResource.resourceName : "???") : "-";
        if (resourceRarityBadge != null) resourceRarityBadge.Set(rarityOwned ? currentResource : null); // like the name: an uncaught rarity's ingredient stays hidden

        // Only a caught rarity has an ingredient worth inspecting - an uncaught
        // one would otherwise leak its name/description/stats.
        if (ingredientButton != null) ingredientButton.interactable = rarityOwned && currentResource != null;
    }

    private void RefreshRarityBadges()
    {
        if (rarityBadges == null) return;

        for (int i = 0; i < rarityBadges.Length; i++)
        {
            var rarity = (CreatureData.Rarity)i;
            rarityBadges[i].Set(currentSpecies, rarity, rarity == selectedRarity, SelectRarity);
        }
    }

    private void OnIngredientClicked()
    {
        if (ingredientPopup == null || currentSpecies == null) return;
        if (!CritterDexIcons.IsRarityDiscovered(currentSpecies, selectedRarity)) return;

        ingredientPopup.Show(currentResource);
    }

    private void CloseIngredientPopup()
    {
        if (ingredientPopup != null) ingredientPopup.Hide();
    }

    private void SetFamilyFrame(CreatureData species)
    {
        if (familyFrame == null) return;

        int index = (int)species.family;
        bool valid = critterFrames != null && critterFrames.familyFrames != null && index >= 0 && index < critterFrames.familyFrames.Length;
        familyFrame.sprite = valid ? critterFrames.familyFrames[index] : null;
        familyBadge.sprite = valid ? critterFrames.familyBadges[index] : null;
    }

    private void ShowNothing()
    {
        if (portraitImage != null) portraitImage.enabled = false;
        if (familyText != null) familyText.text = "-";
        if (nameText != null) nameText.text = "-";
        if (descriptionText != null) descriptionText.text = "";
        if (noteText != null) noteText.text = "";
        if (favoriteScentText != null) favoriteScentText.text = "-";
        if (hatedScentText != null) hatedScentText.text = "-";
        if (resourceIcon != null) resourceIcon.enabled = false;
        if (resourceNameText != null) resourceNameText.text = "-";
        if (resourceRarityBadge != null) resourceRarityBadge.Clear();
        if (ingredientButton != null) ingredientButton.interactable = false;
        ClearRarityBadges();
    }

    private void ShowUndiscovered(CreatureData species)
    {
        // Portrait + rarity buttons go through the same helper as everything
        // else, which draws the solid "undiscovered" look for a never-caught species.
        CritterDexIcons.Apply(portraitImage, species, selectedRarity, portraitStyle);

        if (nameText != null) nameText.text = "???";
        if (descriptionText != null) descriptionText.text = "???";
        if (favoriteScentText != null) favoriteScentText.text = "???";
        if (hatedScentText != null) hatedScentText.text = "???";

        if (resourceIcon != null) resourceIcon.enabled = false;
        if (resourceNameText != null) resourceNameText.text = "???";
        if (resourceRarityBadge != null) resourceRarityBadge.Clear();
        if (ingredientButton != null) ingredientButton.interactable = false;

        RefreshRarityBadges(); // not clickable while undiscovered - the badge checks that itself
    }

    private void ClearRarityBadges()
    {
        if (rarityBadges == null) return;
        foreach (var badge in rarityBadges)
            badge.Set(null, CreatureData.Rarity.Normal, false, null);
    }
}
