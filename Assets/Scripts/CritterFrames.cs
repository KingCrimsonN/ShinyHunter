using UnityEngine;

/// <summary>
/// The CritterDex's art and colors: rarity frames, the per-family frame and
/// badge of a critter page (with the badge's hover variant and pop-up text),
/// and the loved/hated scent icons and colors. Shared asset - read only.
/// </summary>
[CreateAssetMenu(fileName = "CritterFrames", menuName = "ShinyHunt/Critter Frames")]
public class CritterFrames : ScriptableObject
{
    public Sprite[] rarityFrames = new Sprite[4];
    public Sprite[] selectedFrames = new Sprite[4];

    [Header("Family - index = IngredientFamily: Bug, Plant, Freaky, Warm, Cold")]
    public Sprite[] familyFrames = new Sprite[5];
    public Sprite[] familyBadges = new Sprite[5];
    [Tooltip("The focus variant of each family badge, shown while the mouse is over it.")]
    public Sprite[] familyBadgesHover = new Sprite[5];
    [Tooltip("The text of the family pop-up (clicking the badge).")]
    [TextArea(2, 5)]
    public string[] familyDescriptions =
    {
        "Bugs - arthropods: insects, spiders and their crawling kin.",
        "Plants - living vegetables, fungi and carnivorous plants.",
        "Freakies - unnatural beings and the living dead.",
        "Warm-blooded - mammals and birds.",
        "Cold-blooded - fish, reptiles and amphibians.",
    };

    [Header("Scents - index = ScentType: Sweet, Fresh, Putrid, Metallic, Marine")]
    public Sprite[] favoriteScentIcons = new Sprite[5];
    public Sprite[] hatedScentIcons = new Sprite[5];
    [Tooltip("Color of the scent's name on a critter page (loved and hated alike).")]
    public Color[] scentColors =
    {
        new Color32(0xB2, 0x4E, 0x75, 0xFF), // Sweet
        new Color32(0x02, 0xB1, 0xA0, 0xFF), // Fresh
        new Color32(0x61, 0x5D, 0x4D, 0xFF), // Putrid
        new Color32(0x93, 0x1E, 0x34, 0xFF), // Metallic
        new Color32(0x41, 0x5F, 0xBA, 0xFF), // Marine
    };

    /// <summary>array[index], or the fallback when the array is missing or too short.</summary>
    public static T Pick<T>(T[] array, int index, T fallback = default)
    {
        return array != null && index >= 0 && index < array.Length ? array[index] : fallback;
    }
}
