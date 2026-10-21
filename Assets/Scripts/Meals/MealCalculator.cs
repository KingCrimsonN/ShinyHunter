using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure calculation logic turning a list of ingredients into a MealInstance's
/// stats. Every number used here lives on MealCalculationConfig, not
/// hardcoded, so balance tuning never needs a code change.
/// </summary>
public static class MealCalculator
{
    /// <param name="totalCapacity">The cauldron's TOTAL slot count (e.g. BrewingStationUI.cauldronSlots.Length) - NOT how many are currently unlocked, and NOT how many are actually filled. Used by the modifier ratios (CalculateModifier); scents are a plain sum and ignore it.</param>
    /// <param name="preview">True for the brewing panel's live preview: identical numbers, but the modifier's random affected family is NOT rolled (it's decided at brew time, so a preview can't know it) and the result is flagged isPreview.</param>
    public static MealInstance Calculate(List<ResourceData> ingredients, int totalCapacity, MealCalculationConfig config, bool preview = false)
    {
        var meal = new MealInstance { ingredients = new List<ResourceData>(ingredients), isPreview = preview };
        if (ingredients.Count == 0) return meal;

        meal.timeSeconds = CalculateTime(ingredients, config);
        meal.scents = CalculateScents(ingredients, config);

        (meal.dominantScent, meal.hasDominantScent) = GetDominantScent(meal.scents, config);

        var (modifier, power, dominantFamily, affectedFamily) = CalculateModifier(ingredients, totalCapacity, config, preview);
        meal.modifierType = modifier;
        meal.modifierPower = power;
        meal.dominantFamily = dominantFamily;
        meal.affectedFamily = affectedFamily;

        meal.tier = config.GetMealTier(meal.timeSeconds);
        meal.modifierTier = modifier == MealModifierType.None ? 0 : config.GetModifierTier(power);

        return meal;
    }

    private static float CalculateTime(List<ResourceData> ingredients, MealCalculationConfig config)
    {
        float total = config.baseTimeSeconds; // every meal starts here, ingredients add on top
        foreach (var ingredient in ingredients)
        {
            int rarityIndex = (int)ingredient.rarity;
            total += rarityIndex < config.timePerRarity.Length ? config.timePerRarity[rarityIndex] : config.timePerRarity[0];
        }
        return Mathf.Min(total, config.maxTimeSeconds);
    }

    /// <summary>
    /// A meal's scents are simply the SUM of its ingredients' scent values,
    /// per axis, clamped to MealCalculationConfig.scentMaxValue. No division
    /// by the cauldron's capacity and no scaling - an ingredient with 15
    /// Sweet adds exactly 15 Sweet. (This replaced "sum / (scentPerPoint x
    /// slots) x max"; modifiers still use the capacity-based ratios, see
    /// CalculateModifier.) See decision log.
    /// </summary>
    private static float[] CalculateScents(List<ResourceData> ingredients, MealCalculationConfig config)
    {
        float[] sum = new float[5];
        foreach (var ingredient in ingredients)
        {
            sum[0] += ingredient.sweetScent;
            sum[1] += ingredient.freshScent;
            sum[2] += ingredient.putridScent;
            sum[3] += ingredient.metallicScent;
            sum[4] += ingredient.marineScent;
        }

        float max = Mathf.Max(1f, config.scentMaxValue);
        for (int i = 0; i < 5; i++)
            sum[i] = Mathf.Clamp(sum[i], 0f, max);

        return sum;
    }

    /// <summary>
    /// Picks the meal's modifier (whichever family/rarity dominates the
    /// recipe), its power (0-1), the recipe's dominant family (for the
    /// meal's icon), and a RANDOMLY rolled affected family (which family the
    /// modifier's effect actually applies to - decoupled from whichever
    /// family/rarity ratio triggered the modifier in the first place, and
    /// re-rolled independent of dominantFamily). Each family-count /
    /// rarity-sum ratio is divided by the CAULDRON'S TOTAL capacity (not by
    /// how many ingredients were actually used) - same fix as
    /// the earlier scent formula, for the same reason: a single matching ingredient
    /// used to hit ratio=1.0 (100% power) outright, however small the
    /// cauldron. Reaching full power on a modifier now needs filling the
    /// ENTIRE cauldron with ingredients of one family / at max rarity - a
    /// deliberately rare, top-tier outcome. See decision log.
    /// </summary>
    private static (MealModifierType type, float power, IngredientFamily dominantFamily, IngredientFamily affectedFamily) CalculateModifier(
        List<ResourceData> ingredients, int totalCapacity, MealCalculationConfig config, bool preview)
    {
        var familyCounts = new Dictionary<IngredientFamily, int>();
        foreach (IngredientFamily family in System.Enum.GetValues(typeof(IngredientFamily)))
            familyCounts[family] = 0;

        float raritySum = 0f;
        foreach (var ingredient in ingredients)
        {
            familyCounts[ingredient.family]++;
            raritySum += (int)ingredient.rarity + 1; // Normal=1 .. Legendary=4
        }

        float slots = Mathf.Max(1, totalCapacity);
        float rarityRatio = Mathf.Clamp01(raritySum / (slots * 4f));

        var ratios = new Dictionary<MealModifierType, float>
        {
            { MealModifierType.ShinyPower, rarityRatio },
            { MealModifierType.IngredientPower, Mathf.Clamp01(familyCounts[IngredientFamily.Bug] / slots) },
            { MealModifierType.EncounterPower, Mathf.Clamp01(familyCounts[IngredientFamily.Freaky] / slots) },
            { MealModifierType.ChronoPower, Mathf.Clamp01(familyCounts[IngredientFamily.Warm] / slots) },
            { MealModifierType.CapturePower, Mathf.Clamp01(familyCounts[IngredientFamily.Cold] / slots) },
            { MealModifierType.SoothingPower, Mathf.Clamp01(familyCounts[IngredientFamily.Plant] / slots) },
        };

        MealModifierType best = MealModifierType.None;
        float bestRatio = 0f;
        foreach (var kvp in ratios)
        {
            if (kvp.Value > bestRatio)
            {
                bestRatio = kvp.Value;
                best = kvp.Key;
            }
        }

        var dominantFamily = GetDominantFamily(familyCounts);

        if (bestRatio < config.modifierActivationThreshold)
            return (MealModifierType.None, 0f, dominantFamily, IngredientFamily.Bug); // no modifier - affectedFamily is meaningless

        return (best, bestRatio, dominantFamily, preview ? IngredientFamily.Bug : RollRandomFamily()); // a preview must not "use up" a roll the real brew would then re-roll differently
    }

    private static IngredientFamily GetDominantFamily(Dictionary<IngredientFamily, int> familyCounts)
    {
        IngredientFamily dominant = IngredientFamily.Bug;
        int best = -1;
        foreach (var kvp in familyCounts)
        {
            if (kvp.Value > best)
            {
                best = kvp.Value;
                dominant = kvp.Key;
            }
        }
        return dominant;
    }

    /// <summary>
    /// The strongest scent - but only if it is a clear winner: at least
    /// dominantScentMinValue, and no other scent as strong. Otherwise has =
    /// false: the meal is plain. scents is indexed like ScentType.
    /// </summary>
    public static (ScentType scent, bool has) GetDominantScent(float[] scents, MealCalculationConfig config)
    {
        if (scents == null || scents.Length == 0) return (ScentType.Sweet, false);

        int best = 0;
        bool tied = false;
        for (int i = 1; i < scents.Length; i++)
        {
            if (scents[i] > scents[best] && !Mathf.Approximately(scents[i], scents[best]))
            {
                best = i;
                tied = false;
            }
            else if (Mathf.Approximately(scents[i], scents[best]))
            {
                tied = true;
            }
        }

        float minimum = config != null ? config.dominantScentMinValue : 25f;
        bool has = !tied && scents[best] >= minimum;
        return ((ScentType)best, has);
    }

    /// <summary>Uniformly random family, for a newly-active modifier's affectedFamily. See CalculateModifier.</summary>
    private static IngredientFamily RollRandomFamily()
    {
        var values = (IngredientFamily[])System.Enum.GetValues(typeof(IngredientFamily));
        return values[Random.Range(0, values.Length)];
    }
}