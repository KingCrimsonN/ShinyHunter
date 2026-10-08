using UnityEngine;

/// <summary>
/// The small "this ingredient is rarer than common" icons, one per rarity -
/// ONE shared asset so the sprites are chosen once and every ingredient view
/// (inventory, brewing, cauldron slots, dex, pop-ups, transform results) shows
/// the same ones. Common ingredients deliberately have no icon.
/// </summary>
[CreateAssetMenu(fileName = "IngredientRarityIcons", menuName = "ShinyHunt/Ingredient Rarity Icons")]
public class IngredientRarityIcons : ScriptableObject
{
    [Tooltip("Index 0 = Normal (never shown - common ingredients get no icon, whatever is assigned here), 1 = Uncommon, 2 = Rare, 3 = Legendary.")]
    public Sprite[] icons = new Sprite[4];

    /// <summary>The icon for this rarity, or null for Normal / an unassigned slot.</summary>
    public Sprite GetIcon(CreatureData.Rarity rarity)
    {
        if (rarity == CreatureData.Rarity.Normal) return null;

        int index = (int)rarity;
        return icons != null && index < icons.Length ? icons[index] : null;
    }
}
