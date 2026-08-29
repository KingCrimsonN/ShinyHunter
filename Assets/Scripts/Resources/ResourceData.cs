using UnityEngine;

/// <summary>
/// A single resource type - the output of turning a captured creature into
/// ingredients. Each rarity of a species yields a DIFFERENT ResourceData
/// (e.g. "Rabbit Fur (Rare)" is a separate asset from "Rabbit Fur (Legendary)"),
/// so rarity lives on the resource itself rather than needing a separate key.
/// </summary>
[CreateAssetMenu(fileName = "NewResource", menuName = "ShinyHunt/Resource Data")]
public class ResourceData : ScriptableObject
{
    [Header("Identity")]
    public string resourceName;
    public Sprite icon;
    [TextArea] public string description;

    [Tooltip("Which creature rarity this resource is extracted from. Should match the CreatureData.resources slot it's assigned to.")]
    public CreatureData.Rarity rarity = CreatureData.Rarity.Normal;

    [Header("Scents (stew ingredient values)")]
    [Tooltip("Resources have their own scent values, independent of the creature they came from.")]
    public float sweetScent;
    public float freshScent;
    public float putridScent;
    public float metallicScent;
    public float marineScent;

    [Header("Stacking")]
    [Min(1)] public int maxStack = 20;
}