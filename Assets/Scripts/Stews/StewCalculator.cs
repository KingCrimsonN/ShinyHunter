using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure calculation logic turning a list of ingredients into a StewInstance's
/// stats. Every number used here lives on StewCalculationConfig, not
/// hardcoded, so balance tuning never needs a code change.
/// </summary>
public static class StewCalculator
{
    public static StewInstance Calculate(List<ResourceData> ingredients, StewCalculationConfig config)
    {
        var stew = new StewInstance { ingredients = new List<ResourceData>(ingredients) };
        if (ingredients.Count == 0) return stew;

        stew.timeSeconds = CalculateTime(ingredients, config);
        stew.scents = CalculateScents(ingredients, config);

        var (modifier, power, dominantFamily) = CalculateModifier(ingredients, config);
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

    private static float[] CalculateScents(List<ResourceData> ingredients, StewCalculationConfig config)
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

        float divisor = Mathf.Max(0.01f, config.scentPerPoint);
        float[] scaled = new float[5];
        for (int i = 0; i < 5; i++)
            scaled[i] = Mathf.Clamp(1f + raw[i] / divisor, 1f, 5f);

        return scaled;
    }

    private static (StewModifierType type, float power, IngredientFamily dominantFamily) CalculateModifier(
        List<ResourceData> ingredients, StewCalculationConfig config)
    {
        int count = ingredients.Count;

        var familyCounts = new Dictionary<IngredientFamily, int>();
        foreach (IngredientFamily family in System.Enum.GetValues(typeof(IngredientFamily)))
            familyCounts[family] = 0;

        float raritySum = 0f;
        foreach (var ingredient in ingredients)
        {
            familyCounts[ingredient.family]++;
            raritySum += (int)ingredient.rarity + 1; // Normal=1 .. Legendary=4
        }

        float rarityRatio = raritySum / (count * 4f);

        var ratios = new Dictionary<StewModifierType, float>
        {
            { StewModifierType.ShinyPower, rarityRatio },
            { StewModifierType.IngredientPower, familyCounts[IngredientFamily.Animal] / (float)count },
            { StewModifierType.EncounterPower, familyCounts[IngredientFamily.Stranger] / (float)count },
            { StewModifierType.ChronoPower, familyCounts[IngredientFamily.Warm] / (float)count },
            { StewModifierType.CapturePower, familyCounts[IngredientFamily.Cold] / (float)count },
            { StewModifierType.SoothingPower, familyCounts[IngredientFamily.Plant] / (float)count },
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