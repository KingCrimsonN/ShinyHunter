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
/// actually caught. Clicking the family badge opens the family pop-up (the same
/// component, text only - CritterFrames.familyDescriptions); the badge shows its
/// family's focus sprite on hover (Sprite Swap). The loved/hated scents show
/// their own icon and their name in the scent's color (CritterFrames).
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
    [Tooltip("Optional - the badge's Button. Empty = the Button on Family Badge's object. Its Transition must be Sprite Swap for the hover sprite.")]
    [SerializeField] private Button familyButton;
    [Tooltip("The family pop-up - a CritterDexIngredientPopupUI with only its Description Text assigned.")]
    [SerializeField] private CritterDexIngredientPopupUI familyPopup;

    [Header("Scent icons (sprite per scent from CritterFrames)")]
    [SerializeField] private Image favoriteScentIcon;
    [SerializeField] private Image hatedScentIcon;

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

    // What the scent icons/texts look like in the prefab - used when there is no scent to show.
    private Sprite defaultFavoriteIcon, defaultHatedIcon;
    private Color defaultFavoriteColor = Color.black, defaultHatedColor = Color.black;

    private void Awake()
    {
        if (ingredientButton != null) ingredientButton.onClick.AddListener(OnIngredientClicked);

        if (familyButton == null && familyBadge != null) familyButton = familyBadge.GetComponent<Button>();
        if (familyButton != null) familyButton.onClick.AddListener(OnFamilyClicked);

        if (favoriteScentIcon != null) defaultFavoriteIcon = favoriteScentIcon.sprite;
        if (hatedScentIcon != null) defaultHatedIcon = hatedScentIcon.sprite;
        if (favoriteScentText != null) defaultFavoriteColor = favoriteScentText.color;
        if (hatedScentText != null) defaultHatedColor = hatedScentText.color;
    }

    /// <summary>Called by the book when it turns to this species' page.</summary>
    public void Show(CreatureData species)
    {
        currentSpecies = species;
        selectedRarity = CreatureData.Rarity.Normal; // every freshly-opened page starts on Normal
        CloseIngredientPopup();
        CloseFamilyPopup();

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
        ShowScent(favoriteScentText, favoriteScentIcon, species.favoriteScent, critterFrames != null ? critterFrames.favoriteScentIcons : null, defaultFavoriteIcon);
        ShowScent(hatedScentText, hatedScentIcon, species.hatedScent, critterFrames != null ? critterFrames.hatedScentIcons : null, defaultHatedIcon);

        RefreshRarityBadges();
        ShowSelectedRarity();
    }

    /// <summary>Called by the book when it leaves for the index page, so nothing stale (or an open pop-up) is left on the hidden page.</summary>
    public void Clear()
    {
        currentSpecies = null;
        CloseIngredientPopup();
        CloseFamilyPopup();
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

    /// <summary>Family badge clicked: opens the family pop-up (or closes it if it's already open).</summary>
    private void OnFamilyClicked()
    {
        if (familyPopup == null || currentSpecies == null) return;

        if (familyPopup.IsOpen)
        {
            familyPopup.Hide();
            return;
        }

        string text = critterFrames != null
            ? CritterFrames.Pick(critterFrames.familyDescriptions, (int)currentSpecies.family, currentSpecies.family.ToString())
            : currentSpecies.family.ToString();
        familyPopup.ShowText(text);
    }

    private void CloseFamilyPopup()
    {
        if (familyPopup != null) familyPopup.Hide();
    }

    private void SetFamilyFrame(CreatureData species)
    {
        int index = (int)species.family;
        if (familyFrame != null) familyFrame.sprite = critterFrames != null ? CritterFrames.Pick(critterFrames.familyFrames, index) : null;
        if (familyBadge != null) familyBadge.sprite = critterFrames != null ? CritterFrames.Pick(critterFrames.familyBadges, index) : null;

        // Hover/press show this family's focus variant (the button's Transition must be Sprite Swap).
        // Selected stays on the normal sprite, so the badge doesn't stay "focused" after a click.
        if (familyButton != null)
        {
            var hover = critterFrames != null ? CritterFrames.Pick(critterFrames.familyBadgesHover, index) : null;
            var states = familyButton.spriteState;
            states.highlightedSprite = hover;
            states.pressedSprite = hover;
            states.selectedSprite = null;
            familyButton.spriteState = states;
        }
    }

    /// <summary>A loved/hated scent: its name in its color, and its icon (CritterFrames).</summary>
    private void ShowScent(TMP_Text text, Image icon, ScentType scent, Sprite[] icons, Sprite fallbackIcon)
    {
        int index = (int)scent;
        if (text != null)
        {
            text.text = scent.ToString();
            if (critterFrames != null) text.color = CritterFrames.Pick(critterFrames.scentColors, index, text.color);
        }
        if (icon != null) icon.sprite = CritterFrames.Pick(icons, index) ?? fallbackIcon;
    }

    /// <summary>No scent to show (empty page, undiscovered species): the label's own text, color and the generic icons.</summary>
    private void ResetScents(string label)
    {
        if (favoriteScentText != null) { favoriteScentText.text = label; favoriteScentText.color = defaultFavoriteColor; }
        if (hatedScentText != null) { hatedScentText.text = label; hatedScentText.color = defaultHatedColor; }
        if (favoriteScentIcon != null) favoriteScentIcon.sprite = defaultFavoriteIcon;
        if (hatedScentIcon != null) hatedScentIcon.sprite = defaultHatedIcon;
    }

    private void ShowNothing()
    {
        if (portraitImage != null) portraitImage.enabled = false;
        if (familyText != null) familyText.text = "-";
        if (nameText != null) nameText.text = "-";
        if (descriptionText != null) descriptionText.text = "";
        if (noteText != null) noteText.text = "";
        ResetScents("-");
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
        ResetScents("???");

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
