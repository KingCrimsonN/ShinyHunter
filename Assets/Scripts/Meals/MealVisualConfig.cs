using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Everything a meal looks like: its art (one picture per dominant SMELL and
/// tier - or the plain art when it has no dominant smell), its topping and its
/// fork when it has a modifier, its modifier icons and its name.
/// BrewingStationUI.ApplyVisuals stamps the picked sprites and name onto the
/// meal once, so every screen just shows meal.icon / meal.topping / meal.fork.
/// </summary>
[CreateAssetMenu(fileName = "MealVisualConfig", menuName = "ShinyHunt/Meal Visual Config")]
public class MealVisualConfig : ScriptableObject
{
    [Header("Meal art - one per tier (index 0 = tier 1 ... 3 = tier 4), per DOMINANT SMELL")]
    [FormerlySerializedAs("bugDish")] public Sprite[] sweetDish = new Sprite[4];
    [FormerlySerializedAs("plantDish")] public Sprite[] freshDish = new Sprite[4];
    [FormerlySerializedAs("freakyDish")] public Sprite[] putridDish = new Sprite[4];
    [FormerlySerializedAs("warmDish")] public Sprite[] metallicDish = new Sprite[4];
    [FormerlySerializedAs("coldDish")] public Sprite[] marineDish = new Sprite[4];
    [Tooltip("For a meal with NO dominant smell (a tie, or every scent too weak), per tier.")]
    public Sprite[] plainDish = new Sprite[4];

    [Header("Toppings - drawn over the meal when it has a modifier; picked by the modifier's affected FAMILY and the meal's tier")]
    public Sprite[] bugToppings = new Sprite[4];
    public Sprite[] plantToppings = new Sprite[4];
    public Sprite[] freakyToppings = new Sprite[4];
    public Sprite[] warmToppings = new Sprite[4];
    public Sprite[] coldToppings = new Sprite[4];

    [Header("Forks - drawn with a meal that has a modifier: which power it is, one per MEAL tier (index 0 = tier 1 ... 3 = tier 4)")]
    public Sprite[] shinyForks = new Sprite[4];
    public Sprite[] ingredientForks = new Sprite[4];
    public Sprite[] encounterForks = new Sprite[4];
    public Sprite[] chronoForks = new Sprite[4];
    public Sprite[] captureForks = new Sprite[4];
    public Sprite[] soothingForks = new Sprite[4];

    [Header("Modifier icons")]
    [Tooltip("One icon per modifier, index = MealModifierType: 0 None (unused), 1 ShinyPower, 2 IngredientPower, 3 EncounterPower, 4 ChronoPower, 5 CapturePower, 6 SoothingPower. Shown on every modifier row.")]
    public Sprite[] modifierIcons = new Sprite[7];

    /// <summary>The icon for a modifier type, or null if none is assigned (the row then just hides its icon).</summary>
    public Sprite GetModifierIcon(MealModifierType type)
    {
        int index = (int)type;
        return modifierIcons != null && index > 0 && index < modifierIcons.Length ? modifierIcons[index] : null;
    }

    /// <summary>The meal's picture for its dominant smell and tier (1-4) - the plain art when it has no dominant smell.</summary>
    public Sprite GetMealIcon(bool hasDominantScent, ScentType scent, int tier) =>
        PickTier(hasDominantScent ? DishesFor(scent) : plainDish, tier);

    /// <summary>The fork for a modifier, at the MEAL's tier (1-4, from its time - MealInstance.tier), like the toppings - null for no modifier.</summary>
    public Sprite GetFork(MealModifierType modifier, int mealTier) => PickTier(ForksFor(modifier), mealTier);

    private Sprite[] ForksFor(MealModifierType modifier)
    {
        switch (modifier)
        {
            case MealModifierType.ShinyPower: return shinyForks;
            case MealModifierType.IngredientPower: return ingredientForks;
            case MealModifierType.EncounterPower: return encounterForks;
            case MealModifierType.ChronoPower: return chronoForks;
            case MealModifierType.CapturePower: return captureForks;
            case MealModifierType.SoothingPower: return soothingForks;
            default: return null; // None - no fork
        }
    }

    /// <summary>The topping drawn over a meal WITH a modifier - null for none. For now it only depends on the meal's family and tier, not on which modifier it is.</summary>
    public Sprite GetTopping(MealModifierType modifier, IngredientFamily family, int tier) =>
        modifier == MealModifierType.None ? null : PickTier(ToppingsFor(family), tier);

    private Sprite[] DishesFor(ScentType scent)
    {
        switch (scent)
        {
            case ScentType.Sweet: return sweetDish;
            case ScentType.Fresh: return freshDish;
            case ScentType.Putrid: return putridDish;
            case ScentType.Metallic: return metallicDish;
            case ScentType.Marine: return marineDish;
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
    [Tooltip("The dish, from the meal's DOMINANT SMELL, index = ScentType: Sweet, Fresh, Putrid, Metallic, Marine.")]
    [FormerlySerializedAs("familyDishNames")]
    public string[] scentDishNames = { "Cake", "Salad", "Surprise", "Roast", "Chowder" };
    [Tooltip("The dish when the meal has NO dominant smell (\"Shiny Meal\").")]
    public string plainDishName = "Meal";
    [Tooltip("Goes in front of the dish when the meal has no modifier (\"Plain Salad\").")]
    public string noModifierWord = "Plain";

    /// <summary>"{power word} {dish}", e.g. ShinyPower + Sweet -> "Shiny Cake". The dish comes from the DOMINANT SMELL; none = Plain Dish Name.</summary>
    public string GetMealName(MealModifierType modifier, bool hasDominantScent, ScentType dominantScent)
    {
        int s = (int)dominantScent;
        string dish = !hasDominantScent
            ? plainDishName
            : (scentDishNames != null && s >= 0 && s < scentDishNames.Length ? scentDishNames[s] : dominantScent.ToString());

        int m = (int)modifier;
        string word = modifier == MealModifierType.None
            ? noModifierWord
            : (modifierNameWords != null && m > 0 && m < modifierNameWords.Length ? modifierNameWords[m] : modifier.ToString());

        return string.IsNullOrEmpty(word) ? dish : $"{word} {dish}";
    }
}
