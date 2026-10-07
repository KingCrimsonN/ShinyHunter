using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>One tile in the critter inventory: a single critter (individual view - no count) or a species+rarity group (grouped view - shows "xN"). Rarity-correct icon, name, frame, and the double-yield sparkle.</summary>
public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private Image frame;
    [SerializeField] private TMP_Text nameText;
    // [SerializeField] private TMP_Text rarityText;
    [SerializeField] private TMP_Text countText;
    [SerializeField] public TMP_Text descriptionText;

    // [SerializeField] private Sprite[] rarityFrames; // normal, uncommon, rare, legendary
    [SerializeField] private CritterFrames critterFrames;

    [Tooltip("Shown when this critter yields double resources (or, in grouped view, when ANY in the group does) - flagged at capture time. E.g. a sparkle graphic layered on top of the icon.")]
    [SerializeField] private GameObject sparkleOverlay;
    private CreatureData.Rarity rarity;

    private string description;

    [Header("Rarity Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color uncommonColor = new Color(0.3f, 0.8f, 0.3f);
    [SerializeField] private Color rareColor = new Color(0.3f, 0.5f, 1f);
    [SerializeField] private Color legendaryColor = new Color(1f, 0.65f, 0f);

    public void Set(CritterStack stack)
    {
        var species = stack.Species;
        rarity = stack.Rarity;

        if (icon != null) icon.sprite = species.GetIcon(rarity);
        if (nameText != null)
        {
            nameText.text = species.creatureName;
            nameText.color = GetRarityColor(rarity);
        }
        // Individual tiles are always exactly one critter - no "x1" on every tile.
        if (countText != null) countText.text = stack.IsGroup ? "x" + stack.Count : string.Empty;
        description = species.description;

        if (frame != null && critterFrames != null) frame.sprite = critterFrames.rarityFrames[(int)rarity];

        if (sparkleOverlay != null) sparkleOverlay.SetActive(stack.DoubleYieldCount > 0);
    }

    private Color GetRarityColor(CreatureData.Rarity rarity)
    {
        switch (rarity)
        {
            case CreatureData.Rarity.Uncommon: return uncommonColor;
            case CreatureData.Rarity.Rare: return rareColor;
            case CreatureData.Rarity.Legendary: return legendaryColor;
            default: return normalColor;
        }
    }

    private void OnMouseEnter()
    {

    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        frame.sprite = critterFrames.selectedFrames[(int)rarity];
        if (descriptionText != null)
        {
            descriptionText.text = description;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        frame.sprite = critterFrames.rarityFrames[(int)rarity];
        if (descriptionText != null)
        {
            descriptionText.text = "";
        }
    }

}