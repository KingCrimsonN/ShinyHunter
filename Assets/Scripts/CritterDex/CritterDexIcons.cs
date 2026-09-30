using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// How the dex draws a critter icon in each discovery state. One instance is
/// serialized on each dex component that shows critter icons (grid entry,
/// rarity button, detail portrait), so the look is tuned per prefab, but the
/// RULES live in one place - CritterDexIcons.Apply.
/// </summary>
[Serializable]
public class CritterDexIconStyle
{
    [Tooltip("Tint for a normal, caught icon. White = the sprite's own colors.")]
    public Color discoveredColor = Color.white;

    [Tooltip("Only used while a critter has NO locked sprite authored yet (icons[4]): the regular icon is drawn in this dark tint instead, like the old silhouette look.")]
    public Color fallbackLockedColor = new Color(0.12f, 0.12f, 0.12f, 1f);

    [Tooltip("Drawn (in Undiscovered Color) for a species that has NEVER been caught, so nothing about its shape is given away. Leave EMPTY to hide the icon entirely and let the frame behind it show through. Assign a plain white circle if the icon sits in a round frame and you want a solid gray disc.")]
    public Sprite undiscoveredSprite;

    [Tooltip("Tint applied to Undiscovered Sprite.")]
    public Color undiscoveredColor = new Color(0.35f, 0.35f, 0.35f, 1f);
}

/// <summary>
/// The three states a dex icon can be in, shared by the index grid, the rarity
/// buttons and the big portrait so they can never disagree:
///   1. This rarity has been caught      -> its real icon.
///   2. Species seen, this rarity NOT    -> the species' locked variant (icons[4]).
///   3. Species never caught at all      -> solid undiscovered look (no shape shown).
/// Reads discovery from InventoryManager.HasEverCaptured, which survives a
/// critter being transformed into ingredients.
/// </summary>
public static class CritterDexIcons
{
    public static bool IsSpeciesDiscovered(CreatureData species)
    {
        return species != null && InventoryManager.Instance != null && InventoryManager.Instance.HasEverCaptured(species);
    }

    public static bool IsRarityDiscovered(CreatureData species, CreatureData.Rarity rarity)
    {
        return species != null && InventoryManager.Instance != null && InventoryManager.Instance.HasEverCaptured(species, rarity);
    }

    public static void Apply(Image image, CreatureData species, CreatureData.Rarity rarity, CritterDexIconStyle style)
    {
        if (image == null) return;
        if (style == null) style = new CritterDexIconStyle();

        if (species == null)
        {
            image.enabled = false;
            return;
        }

        if (!IsSpeciesDiscovered(species))
        {
            image.sprite = style.undiscoveredSprite;
            image.color = style.undiscoveredColor;
            image.enabled = style.undiscoveredSprite != null;
            return;
        }

        if (IsRarityDiscovered(species, rarity))
        {
            image.sprite = species.GetIcon(rarity);
            image.color = style.discoveredColor;
        }
        else
        {
            var locked = species.GetLockedIcon();
            if (locked != null)
            {
                image.sprite = locked;
                image.color = style.discoveredColor; // the locked art is already a finished silhouette - don't darken it again
            }
            else
            {
                image.sprite = species.GetIcon(rarity);
                image.color = style.fallbackLockedColor;
            }
        }

        image.enabled = image.sprite != null;
    }
}
