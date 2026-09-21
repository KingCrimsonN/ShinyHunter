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