using UnityEngine;
using TMPro;

/// <summary>Shared formatting for stew stats, used by StewDetailsUI and its modifier rows.</summary>
public static class StewDisplayUtil
{
    private static readonly string[] ScentLabels = { "Sweet", "Fresh", "Putrid", "Metallic", "Marine" };

    public static void SetScentNumbers(TMP_Text[] scentTexts, float[] scents)
    {
        if (scentTexts == null || scents == null) return;

        for (int i = 0; i < scentTexts.Length && i < scents.Length; i++)
            if (scentTexts[i] != null) scentTexts[i].text = $"{scents[i]:0}";
    }

    /// <summary>Seconds as minutes:seconds, e.g. 150 -> "2:30".</summary>
    public static string FormatTime(float seconds)
    {
        int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
        return $"{total / 60}:{total % 60:00}";
    }

    public static string FormatName(StewInstance stew)
    {
        return string.IsNullOrEmpty(stew.displayName) ? "Stew" : stew.displayName;
    }

    /// <summary>Spaced-out display name for a modifier type, e.g. "ShinyPower" -> "Shiny Power".</summary>
    public static string FormatModifierName(StewModifierType type)
    {
        switch (type)
        {
            case StewModifierType.ShinyPower: return "Shiny Power";
            case StewModifierType.IngredientPower: return "Ingredient Power";
            case StewModifierType.EncounterPower: return "Encounter Power";
            case StewModifierType.ChronoPower: return "Chrono Power";
            case StewModifierType.CapturePower: return "Capture Power";
            case StewModifierType.SoothingPower: return "Soothing Power";
            default: return type.ToString();
        }
    }

    /// <summary>The family as one word, used in front of a power: "Bug", "Plant", "Freaky", "Warm", "Cold".</summary>
    public static string FormatFamily(IngredientFamily family) => family.ToString();

    /// <summary>
    /// The modifier line's label: the family first, then the power - "Bug Encounter Power".
    /// A preview shows "???" for the family: it is rolled when the stew is actually brewed.
    /// </summary>
    public static string FormatModifierLabel(StewModifierInfo modifier, bool isPreview)
    {
        string family = isPreview ? "???" : FormatFamily(modifier.family);
        return $"{family} {FormatModifierName(modifier.type)}";
    }
}
