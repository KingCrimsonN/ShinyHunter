using UnityEngine;

/// <summary>
/// Per-species configuration. Create one asset per creature type
/// (right click in Project window -> Create -> ShinyHunt -> Creature Data).
/// This is the "adjustable in editor" knob set for CreatureAI.
/// </summary>
[CreateAssetMenu(fileName = "NewCreature", menuName = "ShinyHunt/Creature Data")]
public class CreatureData : ScriptableObject
{
    [Header("Identity")]
    public string creatureName;

    public enum Rarity { Normal, Uncommon, Rare, Legendary }

    public Rarity rarity = Rarity.Normal;
    /* Sprites based on rarity:
        0 = Normal
        1 = Uncommon
        2 = Rare
        3 = Legendary
    */
    public Sprite[] sprites;

    public Sprite icon;
    [TextArea] public string description;

    [Header("Movement")]
    public CreatureMovementMode movementMode = CreatureMovementMode.Ground;
    public float wanderSpeed = 1.5f;
    public float fleeSpeed = 4f;
    [Tooltip("Max distance from spawn point the creature will wander.")]
    public float wanderRadius = 8f;
    [Tooltip("Random range (min,max) seconds spent idling between wander legs.")]
    public Vector2 idleTimeRange = new Vector2(2f, 5f);
    [Tooltip("Random range (min,max) seconds before picking a new wander target even if not reached.")]
    public Vector2 wanderIntervalRange = new Vector2(3f, 8f);

    [Header("Flying (only used if movementMode = Flying)")]
    public float flightHeightMin = 2f;
    public float flightHeightMax = 5f;

    [Header("Fear / Detection")]
    [Tooltip("Player distance at which the creature notices and flees.")]
    public float detectionRadius = 6f;
    [Tooltip("Player distance the creature must reach before it feels safe again.")]
    public float fleeDistance = 10f;

    [Header("Capture")]
    [Range(0f, 1f)] public float baseCaptureChance = 0.5f;
    [Tooltip("Seconds the creature stays stunned/vulnerable after being hit with the stick.")]
    public float stunDuration = 3f;
}
