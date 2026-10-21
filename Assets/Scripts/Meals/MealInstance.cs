using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>One modifier on a meal, as the UI sees it - see MealInstance.GetModifiers.</summary>
public readonly struct MealModifierInfo
{
    public readonly MealModifierType type;
    /// <summary>0-1 strength.</summary>
    public readonly float power;
    /// <summary>1-4, what the UI shows instead of the percentage - see MealCalculationConfig.GetModifierTier.</summary>
    public readonly int tier;
    /// <summary>The family its effect applies to.</summary>
    public readonly IngredientFamily family;

    public MealModifierInfo(MealModifierType type, float power, int tier, IngredientFamily family)
    {
        this.type = type;
        this.power = power;
        this.tier = tier;
        this.family = family;
    }
}

/// <summary>
/// A brewed meal ("bowl"). Runtime data, not a ScriptableObject - each meal
/// is a unique result of a specific brew, not an authored design-time asset.
/// </summary>
[Serializable]
public class MealInstance
{
    public string id = Guid.NewGuid().ToString();

    /// <summary>Optional name to show (e.g. "Auntie's Meal"). Brewed meals leave this empty - see MealDisplayUtil.FormatName.</summary>
    public string displayName;

    /// <summary>1-4, from the meal's time (MealCalculationConfig.GetMealTier). Picks its art.</summary>
    public int tier = 1;

    /// <summary>
    /// True only for the built-in default meal (ExpeditionMealManager.GetDefaultMeal).
    /// It is never stored in a bowl and never consumed when taken on an expedition.
    /// </summary>
    public bool isDefault;

    public List<ResourceData> ingredients = new List<ResourceData>();

    public float timeSeconds;

    /// <summary>Sweet, Fresh, Putrid, Metallic, Marine - each 0 to MealCalculationConfig.scentMaxValue.</summary>
    public float[] scents = new float[5];

    public MealModifierType modifierType = MealModifierType.None;
    /// <summary>0-1 - how strongly the recipe leaned into this modifier's family/rarity.</summary>
    public float modifierPower;
    /// <summary>1-4 from modifierPower (MealCalculationConfig.GetModifierTier); 0 without a modifier.</summary>
    public int modifierTier;

    /// <summary>Which family the recipe actually leaned into most - NOT the same as affectedFamily. See decision log.</summary>
    public IngredientFamily dominantFamily;

    /// <summary>The meal's strongest smell - picks its dish art and name (MealVisualConfig). See MealCalculator.GetDominantScent.</summary>
    public ScentType dominantScent;
    /// <summary>False = no clear dominant smell (a tie, or every scent below MealCalculationConfig.dominantScentMinValue): the meal is plain, and dominantScent is meaningless.</summary>
    public bool hasDominantScent;
    /// <summary>
    /// Which family modifierType's EFFECT applies to - rolled RANDOMLY once at
    /// brew time (MealCalculator.CalculateModifier), independent of
    /// dominantFamily and of whatever ratio actually triggered the modifier.
    /// Meaningless when modifierType is None. See decision log.
    /// </summary>
    public IngredientFamily affectedFamily;
    /// <summary>The meal's picture (MealVisualConfig.GetMealIcon, stamped at brew time by BrewingStationUI.ApplyVisuals).</summary>
    public Sprite icon;
    /// <summary>Drawn over the icon when the meal has a modifier; null otherwise.</summary>
    public Sprite topping;
    /// <summary>Drawn with the meal when it has a modifier, showing WHICH power it is at its tier (MealVisualConfig.GetFork); null otherwise.</summary>
    public Sprite fork;

    /// <summary>
    /// True for the live recipe preview in the brewing panel only (never
    /// stored). A preview can't show affectedFamily because that is rolled at
    /// the moment of brewing - see MealCalculator.Calculate / MealDisplayUtil.FormatModifier.
    /// </summary>
    [NonSerialized] public bool isPreview;

    /// <summary>
    /// Every modifier on this meal, for DISPLAY. Today a meal carries at most
    /// one (modifierType / modifierPower / affectedFamily), so this returns zero
    /// or one entries - but every screen (MealDetailsUI) loops over this list, so
    /// when meals gain several modifiers only this method and the data behind it
    /// change; the UIs already handle any number. (The gameplay queries in
    /// ExpeditionMealManager still read the single modifier directly.)
    /// </summary>
    public IReadOnlyList<MealModifierInfo> GetModifiers()
    {
        var list = new List<MealModifierInfo>();
        if (modifierType != MealModifierType.None)
            list.Add(new MealModifierInfo(modifierType, modifierPower, modifierTier, affectedFamily));
        return list;
    }
}