using UnityEngine;

/// <summary>
/// Per-species configuration. Create one asset per creature type
/// (right click in Project window -> Create -> ShinyHunt -> Creature Data).
/// This is the "adjustable in editor" knob set for CreatureAI.
///
/// NOTE: this is a shared asset - every instance of this species in the
/// scene references the SAME CreatureData object. Never write to fields
/// on this object at runtime (e.g. rarity) - that would leak between
/// instances. Runtime-rolled values like rolled rarity live on CreatureAI
/// instead. See CreatureAI.Rarity.
/// </summary>
[CreateAssetMenu(fileName = "NewCreature", menuName = "ShinyHunt/Creature Data")]
public class CreatureData : ScriptableObject
{
    [Header("Identity")]
    public string creatureName;

    public enum Rarity { Normal, Uncommon, Rare, Legendary }

    [TextArea] public string description;

    [Header("Visuals per rarity")]
    [Tooltip("Index 0=Normal, 1=Uncommon, 2=Rare, 3=Legendary. Each entry holds that variant's full set of named animations (Idle, Move, Flee, etc). Fine to leave states unauthored while art is still coming in - they simply won't play.")]
    public CreatureVariantVisuals[] variants = new CreatureVariantVisuals[4];

    [Tooltip("Static thumbnails per rarity, for UI (inventory/bestiary) - separate from the in-world animation frames.")]
    public Sprite[] icons = new Sprite[4];

    public Vector3 size;

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

    /// <summary>Animation set for a given rolled rarity. Falls back to index 0 if the array is short.</summary>
    public CreatureVariantVisuals GetVariant(Rarity rarity)
    {
        if (variants == null || variants.Length == 0) return null;
        int index = (int)rarity;
        return index < variants.Length ? variants[index] : variants[0];
    }

    /// <summary>UI thumbnail for a given rolled rarity. Falls back to index 0 if the array is short.</summary>
    public Sprite GetIcon(Rarity rarity)
    {
        if (icons == null || icons.Length == 0) return null;
        int index = (int)rarity;
        return index < icons.Length ? icons[index] : icons[0];
    }
}