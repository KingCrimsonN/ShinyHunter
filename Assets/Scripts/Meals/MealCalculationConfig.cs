using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Every tunable number MealCalculator and ExpeditionMealManager use. One
/// shared asset, so you can rebalance the whole system without touching code.
/// </summary>
[CreateAssetMenu(fileName = "MealCalculationConfig", menuName = "ShinyHunt/Meal Calculation Config")]
public class MealCalculationConfig : ScriptableObject
{
    [Header("Time")]
    [Tooltip("Every brewed meal starts from this many seconds, before ingredients add theirs - so even a single weak ingredient gives a usable meal. (The default meal has its own fixed time, see below.)")]
    public float baseTimeSeconds = 60f;
    [Tooltip("Seconds contributed per ingredient, ON TOP of Base Time Seconds, indexed by CreatureData.Rarity (Normal..Legendary).")]
    public float[] timePerRarity = { 60f, 90f, 150f, 240f };
    [Tooltip("Hard cap on total meal time (base + ingredients) - \"capped by the bowl\".")]
    public float maxTimeSeconds = 600f;

    [Header("Tiers")]
    [Tooltip("A meal's tier (1-4) comes from its time: MORE than the 1st number = tier 2, more than the 2nd = tier 3, more than the 3rd = tier 4, otherwise tier 1. Ascending, in seconds. The tier picks the meal's art (MealVisualConfig).")]
    public float[] mealTierTimeThresholds = { 120f, 180f, 210f };
    [Tooltip("A modifier's tier (1-4) comes from its power (0-1): BELOW the 1st number = tier 1, below the 2nd = tier 2, below the 3rd = tier 3, otherwise tier 4. Ascending. Display only - the effects still scale with the exact power.")]
    public float[] modifierTierThresholds = { 0.35f, 0.5f, 0.8f };

    /// <summary>1-4, from the meal's time - see mealTierTimeThresholds.</summary>
    public int GetMealTier(float timeSeconds)
    {
        int tier = 1;
        if (mealTierTimeThresholds != null)
            foreach (float threshold in mealTierTimeThresholds)
                if (timeSeconds > threshold) tier++;
        return tier;
    }

    /// <summary>1-4, from a modifier's 0-1 power - see modifierTierThresholds.</summary>
    public int GetModifierTier(float power)
    {
        int tier = 1;
        if (modifierTierThresholds != null)
            foreach (float threshold in modifierTierThresholds)
                if (power >= threshold) tier++;
        return tier;
    }

    [Header("Scents")]
    [Tooltip("The most any scent axis can reach. A meal's scents are the plain SUM of its ingredients' scent values, clamped to this. It's also the scale everything else normalizes against (creature spawn/detection bias, spawner population) via ExpeditionMealManager.ScentMax - so a meal only has full effect when an axis actually reaches this number; tune ingredient scent values (ResourceData) with that in mind.")]
    public float scentMaxValue = 500f;
    [Tooltip("Spawn-WEIGHT multiplier a species gets when the meal's scent on its FAVORITE axis is at the cap (Scent Max Value). The curve is exponential, so half the cap gives the square root of this: with 4, half = x2 and full = x4, and each extra 100 compounds (100 of 500 is about +32%). A HATED axis does the exact opposite - full = x1/4, half = x1/2 - and never reaches zero. This multiplies the species' WEIGHT, not its final probability: probability is weight / total weight of every species the spawner can pick.")]
    [Min(1f)] public float scentSpawnMultiplierAtMax = 4f;
    [Tooltip("Population-cap fraction (0-1) a Spawner uses when the active meal's scent is at its weakest (every axis at 0, e.g. no meal at all). Scales up to the full populationCap as the loudest single scent axis approaches Scent Max Value - a weak-smelling meal draws fewer creatures. 1 = scent strength doesn't affect population at all.")]
    [Range(0f, 1f)]
    public float scentPopulationMinFraction = 0.4f;

    [Header("Modifier Selection")]
    [Tooltip("Minimum dominance ratio (0-1) required for ANY modifier to activate. Below this, the meal gets MealModifierType.None.")]
    public float modifierActivationThreshold = 0.3f;

    [Header("Default Meal (always available at the exit door)")]
    [Tooltip("Shown as the meal's name. This meal is never stored in a bowl and never used up - it's the fallback when the player has nothing better to take.")]
    [FormerlySerializedAs("defaultStewName")]
    public string defaultMealName = "Auntie's Meal";
    [Tooltip("Expedition time this meal gives, in seconds. Keep it modest so brewing a real meal is worth it.")]
    [FormerlySerializedAs("defaultStewTimeSeconds")]
    public float defaultMealTimeSeconds = 120f;
    [Tooltip("Sweet, Fresh, Putrid, Metallic, Marine - each 0 to Scent Max Value. All 0 = neutral (same as taking no meal).")]
    [FormerlySerializedAs("defaultStewScents")]
    public float[] defaultMealScents = { 0f, 0f, 0f, 0f, 0f };
    [Tooltip("Its picture (it has no dominant family, so it does not use MealVisualConfig's art). If left empty, the carousel entry keeps whatever sprite its prefab has.")]
    [FormerlySerializedAs("defaultStewIcon")]
    public Sprite defaultMealIcon;

    // Every modifier below applies ONLY to creatures/captures of the active
    // meal's randomly-rolled affected family (MealInstance.affectedFamily) -
    // see decision log.
    [Header("Modifier Effect Scales")]
    [Tooltip("Rarity-roll chances are multiplied by (1 + power * this) when Shiny Power is active, for creatures of the affected family only.")]
    public float shinyRarityMultiplierScale = 1.5f;
    [Tooltip("Chance (0-1) of double resources on transform, at power = 1.0, when Ingredient Power is active and the CAPTURED creature was of the affected family. Rolled once AT CAPTURE, not at transform time - see decision log.")]
    public float ingredientDoubleChanceScale = 0.5f;
    [Tooltip("Spawn-weight multiplier for the affected family, at power = 1.0, when Encounter Power is active.")]
    public float encounterFamilyWeightScale = 1.0f;
    [Tooltip("Bonus expedition time (seconds), at power = 1.0, granted for EACH capture of the affected family while Chrono Power is active - up to a minute per capture at full power. Applied immediately to the CURRENT run, not banked for later - see decision log.")]
    public float chronoMaxBonusPerCapture = 60f;
    [Tooltip("Flat capture-chance bonus (0-1), at power = 1.0, when Capture Power is active and the targeted creature is of the affected family.")]
    public float captureChanceBonusScale = 0.3f;
    [Tooltip("Hit areas auto-marked as hit at the start of the wheel, at power = 1.0, when Capture Power is active and the targeted creature is of the affected family.")]
    public int captureMaxAutoBreakAreas = 2;
    [Tooltip("Weapon hit-power bonus (0-1), at power = 1.0, when Capture Power is active and the creature being hit is of the affected family - actual damage = baseDamage * (1 + modifierPower * this).")]
    public float captureHitPowerBonusScale = 0.5f;
    [Tooltip("Extra seconds added to the capture minigame's time limit, at power = 1.0, when Capture Power is active and the targeted creature is of the affected family.")]
    public float captureTimeBonusScale = 3f;
    [Tooltip("Flee speed / detection radius reduction (0-1), at power = 1.0, when Soothing Power is active, for creatures of the affected family only.")]
    public float soothingMaxSlowdown = 0.5f;
}