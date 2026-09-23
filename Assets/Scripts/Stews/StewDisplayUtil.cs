using UnityEngine;
using TMPro;

/// <summary>Shared formatting for stew stats, reused across the result popup, bowl inventory, and exit-door carousel.</summary>
public static class StewDisplayUtil
{
    private static readonly string[] ScentLabels = { "Sweet", "Fresh", "Putrid", "Metallic", "Marine" };

    public static void SetScentTexts(TMP_Text[] scentTexts, float[] scents)
    {
        if (scentTexts == null || scents == null) return;

        for (int i = 0; i < scentTexts.Length && i < scents.Length; i++)
            if (scentTexts[i] != null) scentTexts[i].text = $"{ScentLabels[i]}: {scents[i]:0}";
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

    /// <summary>
    /// Informal family nickname, matching the names already used in
    /// IngredientFamily's own comments ("Bugs", "Freakies") - invented for
    /// the other three (Plants/Warm-Blooded/Cold-Blooded), retune freely.
    /// </summary>
    public static string FormatFamily(IngredientFamily family)
    {
        switch (family)
        {
            case IngredientFamily.Animal: return "Bugs";
            case IngredientFamily.Plant: return "Plants";
            case IngredientFamily.Stranger: return "Freakies";
            case IngredientFamily.Warm: return "Warm-Blooded";
            case IngredientFamily.Cold: return "Cold-Blooded";
            default: return family.ToString();
        }
    }

    /// <summary>e.g. "Shiny Power: Bugs (73%)". Every active modifier is now family-scoped - see StewInstance.affectedFamily.</summary>
    public static string FormatModifier(StewInstance stew)
    {
        return stew.modifierType == StewModifierType.None
            ? "No modifier"
            : $"{FormatModifierName(stew.modifierType)}: {FormatFamily(stew.affectedFamily)} ({stew.modifierPower * 100f:0}%)";
    }
}