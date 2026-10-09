using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>One modifier on a stew, as the UI sees it - see StewInstance.GetModifiers.</summary>
public readonly struct StewModifierInfo
{
    public readonly StewModifierType type;
    /// <summary>0-1 strength.</summary>
    public readonly float power;
    /// <summary>The family its effect applies to.</summary>
    public readonly IngredientFamily family;

    public StewModifierInfo(StewModifierType type, float power, IngredientFamily family)
    {
        this.type = type;
        this.power = power;
        this.family = family;
    }
}

/// <summary>
/// A brewed stew ("bowl"). Runtime data, not a ScriptableObject - each stew
/// is a unique result of a specific brew, not an authored design-time asset.
/// </summary>
[Serializable]
public class StewInstance
{
    public string id = Guid.NewGuid().ToString();

    /// <summary>Optional name to show (e.g. "Auntie's Stew"). Brewed stews leave this empty - see StewDisplayUtil.FormatName.</summary>
    public string displayName;

    /// <summary>
    /// True only for the built-in default stew (ExpeditionStewManager.GetDefaultStew).
    /// It is never stored in a bowl and never consumed when taken on an expedition.
    /// </summary>
    public bool isDefault;

    public List<ResourceData> ingredients = new List<ResourceData>();

    public float timeSeconds;

    /// <summary>Sweet, Fresh, Putrid, Metallic, Marine - each 0 to StewCalculationConfig.scentMaxValue.</summary>
    public float[] scents = new float[5];

    public StewModifierType modifierType = StewModifierType.None;
    /// <summary>0-1 - how strongly the recipe leaned into this modifier's family/rarity.</summary>
    public float modifierPower;

    /// <summary>Which family the recipe actually leaned into most (drives the stew's icon via StewVisualConfig) - NOT the same as affectedFamily. See decision log.</summary>
    public IngredientFamily dominantFamily;
    /// <summary>
    /// Which family modifierType's EFFECT applies to - rolled RANDOMLY once at
    /// brew time (StewCalculator.CalculateModifier), independent of
    /// dominantFamily and of whatever ratio actually triggered the modifier.
    /// Meaningless when modifierType is None. See decision log.
    /// </summary>
    public IngredientFamily affectedFamily;
    public Sprite icon;

    /// <summary>
    /// True for the live recipe preview in the brewing panel only (never
    /// stored). A preview can't show affectedFamily because that is rolled at
    /// the moment of brewing - see StewCalculator.Calculate / StewDisplayUtil.FormatModifier.
    /// </summary>
    [NonSerialized] public bool isPreview;

    /// <summary>
    /// Every modifier on this stew, for DISPLAY. Today a stew carries at most
    /// one (modifierType / modifierPower / affectedFamily), so this returns zero
    /// or one entries - but every screen (StewDetailsUI) loops over this list, so
    /// when stews gain several modifiers only this method and the data behind it
    /// change; the UIs already handle any number. (The gameplay queries in
    /// ExpeditionStewManager still read the single modifier directly.)
    /// </summary>
    public IReadOnlyList<StewModifierInfo> GetModifiers()
    {
        var list = new List<StewModifierInfo>();
        if (modifierType != StewModifierType.None)
            list.Add(new StewModifierInfo(modifierType, modifierPower, affectedFamily));
        return list;
    }
}