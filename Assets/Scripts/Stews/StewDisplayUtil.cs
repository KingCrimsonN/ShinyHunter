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

    public static string FormatModifier(StewInstance stew)
    {
        return stew.modifierType == StewModifierType.None
            ? "No modifier"
            : $"{stew.modifierType} ({stew.modifierPower * 100f:0}%)";
    }
}