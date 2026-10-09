using UnityEngine;

/// <summary>Icon per dominant family, used for a brewed stew's display icon.</summary>
[CreateAssetMenu(fileName = "StewVisualConfig", menuName = "ShinyHunt/Stew Visual Config")]
public class StewVisualConfig : ScriptableObject
{
    [Tooltip("Index matches IngredientFamily enum order: Bug, Plant, Freaky, Warm, Cold.")]
    public Sprite[] familyIcons = new Sprite[5];

    [Tooltip("One icon per modifier, index = StewModifierType: 0 None (unused), 1 ShinyPower, 2 IngredientPower, 3 EncounterPower, 4 ChronoPower, 5 CapturePower, 6 SoothingPower. Shown on every modifier row.")]
    public Sprite[] modifierIcons = new Sprite[7];

    /// <summary>The icon for a modifier type, or null if none is assigned (the row then just hides its icon).</summary>
    public Sprite GetModifierIcon(StewModifierType type)
    {
        int index = (int)type;
        return modifierIcons != null && index > 0 && index < modifierIcons.Length ? modifierIcons[index] : null;
    }

    [Header("Stew names (\"{power word} {dish}\", e.g. \"Shiny Cake\")")]
    [Tooltip("The first word of a stew's name, index = StewModifierType: 0 None (unused), 1 ShinyPower, 2 IngredientPower, 3 EncounterPower, 4 ChronoPower, 5 CapturePower, 6 SoothingPower.")]
    public string[] modifierNameWords = { "", "Shiny", "Hearty", "Wild", "Timeless", "Sticky", "Soothing" };
    [Tooltip("The dish, from the stew's DOMINANT family, index = IngredientFamily: Bug, Plant, Freaky, Warm, Cold.")]
    public string[] familyDishNames = { "Cake", "Salad", "Surprise", "Roast", "Chowder" };
    [Tooltip("Goes in front of the dish when the stew has no modifier (\"Plain Salad\").")]
    public string noModifierWord = "Plain";

    /// <summary>"{power word} {dish}", e.g. ShinyPower + Bug -> "Shiny Cake". Uses the DOMINANT family (known even in a preview), not the rolled affected family.</summary>
    public string GetStewName(StewModifierType modifier, IngredientFamily dominantFamily)
    {
        int f = (int)dominantFamily;
        string dish = familyDishNames != null && f >= 0 && f < familyDishNames.Length ? familyDishNames[f] : dominantFamily.ToString();

        int m = (int)modifier;
        string word = modifier == StewModifierType.None
            ? noModifierWord
            : (modifierNameWords != null && m > 0 && m < modifierNameWords.Length ? modifierNameWords[m] : modifier.ToString());

        return string.IsNullOrEmpty(word) ? dish : $"{word} {dish}";
    }

    public Sprite GetFamilyIcon(IngredientFamily family)
    {
        if (familyIcons == null || familyIcons.Length == 0) return null;
        int index = (int)family;
        return index < familyIcons.Length ? familyIcons[index] : familyIcons[0];
    }
}