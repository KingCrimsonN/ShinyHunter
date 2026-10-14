using UnityEngine;

/// <summary>
/// Everything a meal looks like: its art (one picture per dominant family and
/// tier), its topping when it has a modifier, its modifier icons and its name.
/// BrewingStationUI.ApplyVisuals stamps the picked sprites and name onto the
/// meal once, so every screen just shows meal.icon / meal.topping.
/// </summary>
[CreateAssetMenu(fileName = "MealVisualConfig", menuName = "ShinyHunt/Meal Visual Config")]
public class MealVisualConfig : ScriptableObject
{
    [Header("Meal art - one per tier (index 0 = tier 1 ... 3 = tier 4), per DOMINANT family")]
    public Sprite[] bugDish = new Sprite[4];
    public Sprite[] plantDish = new Sprite[4];
    public Sprite[] freakyDish = new Sprite[4];
    public Sprite[] warmDish = new Sprite[4];
    public Sprite[] coldDish = new Sprite[4];

    [Header("Toppings - drawn over the meal when it has a modifier; for now picked by the meal's tier")]
    public Sprite[] bugToppings = new Sprite[4];
    public Sprite[] plantToppings = new Sprite[4];
    public Sprite[] freakyToppings = new Sprite[4];
    public Sprite[] warmToppings = new Sprite[4];
    public Sprite[] coldToppings = new Sprite[4];

    [Header("Modifier icons")]
    [Tooltip("One icon per modifier, index = MealModifierType: 0 None (unused), 1 ShinyPower, 2 IngredientPower, 3 EncounterPower, 4 ChronoPower, 5 CapturePower, 6 SoothingPower. Shown on every modifier row.")]
    public Sprite[] modifierIcons = new Sprite[7];

    /// <summary>The icon for a modifier type, or null if none is assigned (the row then just hides its icon).</summary>
    public Sprite GetModifierIcon(MealModifierType type)
    {
        int index = (int)type;
        return modifierIcons != null && index > 0 && index < modifierIcons.Length ? modifierIcons[index] : null;
    }

    /// <summary>The meal's picture for its dominant family and tier (1-4).</summary>
    public Sprite GetMealIcon(IngredientFamily family, int tier) => PickTier(DishesFor(family), tier);

    /// <summary>The topping drawn over a meal WITH a modifier - null for none. For now it only depends on the meal's family and tier, not on which modifier it is.</summary>
    public Sprite GetTopping(MealModifierType modifier, IngredientFamily family, int tier) =>
        modifier == MealModifierType.None ? null : PickTier(ToppingsFor(family), tier);

    private Sprite[] DishesFor(IngredientFamily family)
    {
        switch (family)
        {
            case IngredientFamily.Bug: return bugDish;
            case IngredientFamily.Plant: return plantDish;
            case IngredientFamily.Freaky: return freakyDish;
            case IngredientFamily.Warm: return warmDish;
            case IngredientFamily.Cold: return coldDish;
            default: return null;
        }
    }

    private Sprite[] ToppingsFor(IngredientFamily family)
    {
        switch (family)
        {
            case IngredientFamily.Bug: return bugToppings;
            case IngredientFamily.Plant: return plantToppings;
            case IngredientFamily.Freaky: return freakyToppings;
            case IngredientFamily.Warm: return warmToppings;
            case IngredientFamily.Cold: return coldToppings;
            default: return null;
        }
    }

    /// <summary>tier is 1-based; a short array falls back to its last entry.</summary>
    private static Sprite PickTier(Sprite[] sprites, int tier)
    {
        if (sprites == null || sprites.Length == 0) return null;
        return sprites[Mathf.Clamp(tier - 1, 0, sprites.Length - 1)];
    }

    [Header("Meal names (\"{power word} {dish}\", e.g. \"Shiny Cake\")")]
    [Tooltip("The first word of a meal's name, index = MealModifierType: 0 None (unused), 1 ShinyPower, 2 IngredientPower, 3 EncounterPower, 4 ChronoPower, 5 CapturePower, 6 SoothingPower.")]
    public string[] modifierNameWords = { "", "Shiny", "Hearty", "Wild", "Timeless", "Sticky", "Soothing" };
    [Tooltip("The dish, from the meal's DOMINANT family, index = IngredientFamily: Bug, Plant, Freaky, Warm, Cold.")]
    public string[] familyDishNames = { "Cake", "Salad", "Surprise", "Roast", "Chowder" };
    [Tooltip("Goes in front of the dish when the meal has no modifier (\"Plain Salad\").")]
    public string noModifierWord = "Plain";

    /// <summary>"{power word} {dish}", e.g. ShinyPower + Bug -> "Shiny Cake". Uses the DOMINANT family (known even in a preview), not the rolled affected family.</summary>
    public string GetMealName(MealModifierType modifier, IngredientFamily dominantFamily)
    {
        int f = (int)dominantFamily;
        string dish = familyDishNames != null && f >= 0 && f < familyDishNames.Length ? familyDishNames[f] : dominantFamily.ToString();

        int m = (int)modifier;
        string word = modifier == MealModifierType.None
            ? noModifierWord
            : (modifierNameWords != null && m > 0 && m < modifierNameWords.Length ? modifierNameWords[m] : modifier.ToString());

        return string.IsNullOrEmpty(word) ? dish : $"{word} {dish}";
    }
}
