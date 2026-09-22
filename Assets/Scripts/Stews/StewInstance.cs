using System;
using System.Collections.Generic;
using UnityEngine;

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

    /// <summary>Sweet, Fresh, Putrid, Metallic, Marine - each 1-5.</summary>
    public float[] scents = new float[5];

    public StewModifierType modifierType = StewModifierType.None;
    /// <summary>0-1 - how strongly the recipe leaned into this modifier's family/rarity.</summary>
    public float modifierPower;

    public IngredientFamily dominantFamily;
    public Sprite icon;
}