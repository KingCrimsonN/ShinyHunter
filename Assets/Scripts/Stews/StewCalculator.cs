using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure calculation logic turning a list of ingredients into a StewInstance's
/// stats. Every number used here lives on StewCalculationConfig, not
/// hardcoded, so balance tuning never needs a code change.
/// </summary>
public static class StewCalculator
{
    /// <summary>
    /// Scent axes are reported on a 0-100 scale for now (was 1-5), so raw
    /// calculation output is easy to read while tuning - a later pass will
    /// remap this to whatever's actually shown to the player. See decision log.
    /// </summary>
    private const float ScentMaxValue = 100f;

    /// <param name="totalCapacity">The cauldron's TOTAL slot count (e.g. BrewingStationUI.cauldronSlots.Length) - NOT how many are currently unlocked, and NOT how many are actually filled. See CalculateScents.</param>
    public static StewInstance Calculate(List<ResourceData> ingredients, int totalCapacity, StewCalculationConfig config)
    {
        var stew = new StewInstance { ingredients = new List<ResourceData>(ingredients) };
        if (ingredients.Count == 0) return stew;

        stew.timeSeconds = CalculateTime(ingredients, config);
        stew.scents = CalculateScents(ingredients, totalCapacity, config);

        var (modifier, power, dominantFamily) = CalculateModifier(ingredients, totalCapacity, config);
        stew.modifierType = modifier;
        stew.modifierPower = power;
        stew.dominantFamily = dominantFamily;

        return stew;
    }

    private static float CalculateTime(List<ResourceData> ingredients, StewCalculationConfig config)
    {
        float total = 0f;
        foreach (var ingredient in ingredients)
        {
            int rarityIndex = (int)ingredient.rarity;
            total += rarityIndex < config.timePerRarity.Length ? config.timePerRarity[rarityIndex] : config.timePerRarity[0];
        }
        return Mathf.Min(total, config.maxTimeSeconds);
    }

    /// <summary>
    /// Raw scent points are summed across ingredients, then divided by the
    /// CAULDRON'S TOTAL capacity (not by how many ingredients were actually
    /// used, and not by a fixed constant) - so 1-2 strong ingredients out of
    /// many possible slots can only ever reach a small fraction of the scale;
    /// reaching a high value needs filling most/all of the cauldron. See
    /// decision log for why (this replaced a fixed-divisor formula that let a
    /// couple of ingredients reach near-max on their own).
    /// </summary>
    private static float[] CalculateScents(List<ResourceData> ingredients, int totalCapacity, StewCalculationConfig config)
    {
        float[] raw = new float[5];
        foreach (var ingredient in ingredients)
        {
            raw[0] += ingredient.sweetScent;
            raw[1] += ingredient.freshScent;
            raw[2] += ingredient.putridScent;
            raw[3] += ingredient.metallicScent;
            raw[4] += ingredient.marineScent;
        }

        float slots = Mathf.Max(1, totalCapacity);
        float divisor = Mathf.Max(0.01f, config.scentPerPoint) * slots;

        float[] scaled = new float[5];
        for (int i = 0; i < 5; i++)
            scaled[i] = Mathf.Clamp(raw[i] / divisor * ScentMaxValue, 0f, ScentMaxValue);

        return scaled;
    }

    /// <summary>
    /// Picks the stew's modifier (whichever family/rarity dominates the
    /// recipe) and its power (0-1). Each family-count / rarity-sum ratio is
    /// divided by the CAULDRON'S TOTAL capacity (not by how many ingredients
    /// were actually used) - same fix as CalculateScents, for the same
    /// reason: a single matching ingredient used to hit ratio=1.0 (100%
    /// power) outright, however small the cauldron. Reaching full power on a
    /// modifier now needs filling the ENTIRE cauldron with ingredients of one
    /// family / at max rarity - a deliberately rare, top-tier outcome. See
    /// decision log.
    /// </summary>
    private static (StewModifierType type, float power, IngredientFamily dominantFamily) CalculateModifier(
        List<ResourceData> ingredients, int totalCapacity, StewCalculationConfig config)
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

        var ratios = new Dictionary<StewModifierType, float>
        {
            { StewModifierType.ShinyPower, rarityRatio },
            { StewModifierType.IngredientPower, Mathf.Clamp01(familyCounts[IngredientFamily.Animal] / slots) },
            { StewModifierType.EncounterPower, Mathf.Clamp01(familyCounts[IngredientFamily.Stranger] / slots) },
            { StewModifierType.ChronoPower, Mathf.Clamp01(familyCounts[IngredientFamily.Warm] / slots) },
            { StewModifierType.CapturePower, Mathf.Clamp01(familyCounts[IngredientFamily.Cold] / slots) },
            { StewModifierType.SoothingPower, Mathf.Clamp01(familyCounts[IngredientFamily.Plant] / slots) },
        };

        StewModifierType best = StewModifierType.None;
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
            return (StewModifierType.None, 0f, dominantFamily);

        return (best, bestRatio, dominantFamily);
    }

    private static IngredientFamily GetDominantFamily(Dictionary<IngredientFamily, int> familyCounts)
    {
        IngredientFamily dominant = IngredientFamily.Animal;
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
}