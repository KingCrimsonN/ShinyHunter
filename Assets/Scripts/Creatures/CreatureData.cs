using UnityEngine;
using UnityEngine.Serialization;

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

    /// <summary>
    /// How the species treats the player. The numbers matter: this replaced the
    /// old bool isAggressive (false = 0, true = 1), so existing assets keep their meaning.
    /// </summary>
    public enum Temperament
    {
        /// <summary>Flees when it notices the player or is hit.</summary>
        Passive = 0,
        /// <summary>Chases and attacks when it notices the player.</summary>
        Aggressive = 1,
        /// <summary>Flees like Passive - until it is HIT, then turns aggressive (that one critter only) until it loses interest or is stunned.</summary>
        Neutral = 2,
    }

    [TextArea] public string description;
    [TextArea] public string note;

    [Tooltip("Biological family - drives which stew modifier this species' ingredient contributes toward, and Encounter Power's spawn-weight boost target.")]
    public IngredientFamily family;

    [Header("Scent Preference (spawn bias)")]
    [Tooltip("The one scent this species is drawn to. Boosts spawn weight when the active stew is strong on this axis - the stew's other four scent values are ignored entirely for this species. See GetScentAffinity.")]
    public ScentType favoriteScent = ScentType.Sweet;
    [Tooltip("The one scent this species is repelled by (\"hated\"). Reduces spawn weight AND increases detection radius (notices/flees the player from farther away, scaled by Detection Aversion Scale below) when the active stew is strong on THIS axis - again, only this one axis matters, not the other four. See GetScentAffinity.")]
    public ScentType hatedScent = ScentType.Putrid;

    [Header("Visuals per rarity")]
    [Tooltip("Index 0=Normal, 1=Uncommon, 2=Rare, 3=Legendary. Each entry holds that variant's full set of named animations (Idle, Move, Flee, etc). Fine to leave states unauthored while art is still coming in - they simply won't play.")]
    public CreatureVariantVisuals[] variants = new CreatureVariantVisuals[4];

    [Tooltip("Static thumbnails for UI (inventory/bestiary) - separate from the in-world animation frames. Index 0=Normal, 1=Uncommon, 2=Rare, 3=Legendary, and index 4 (optional, add it at the END) = the LOCKED variant the CritterDex shows for a rarity of an already-discovered species that hasn't been caught yet. Arrays that only have 4 entries keep working - the dex falls back to a dark tint of the regular icon until the locked art exists.")]
    public Sprite[] icons = new Sprite[5];

    /// <summary>Index in icons[] of the locked/silhouette variant - always AFTER the four rarities, so adding it never shifts an existing entry.</summary>
    public const int LockedIconIndex = 4;

    [Header("Physical")]
    [Tooltip("Applied to transform.localScale on spawn.")]
    public Vector3 size = Vector3.one;

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
    [Tooltip("Player distance at which the creature notices and reacts (flees, or moves in to attack if aggressive).")]
    public float detectionRadius = 6f;
    [Tooltip("Player distance the creature must reach before it feels safe/loses interest again (fleeing OR aggressive - see temperament).")]
    public float fleeDistance = 10f;
    [Tooltip("World units added to the detection radius at a full-strength FAVORITE scent, and subtracted at a full-strength HATED one (scaled by how strong the stew's scent is on that axis, 0 to 1). Negative flips both. The radius never drops below a small minimum. See CreatureAI.EffectiveDetectionRadius.")]
    public float scentDistanceAtMax = 3f;

    [Header("Aggression")]
    [Tooltip("Passive flees. Aggressive moves TOWARD the player and attacks once it notices them. Neutral flees until it is hit, then turns aggressive. When aggressive, everything else (detectionRadius, fleeDistance as the \"give up\" distance, fleeSpeed as the chase speed) is reused as-is - see CreatureAI.CheckPlayerProximity.")]
    [FormerlySerializedAs("isAggressive")]
    public Temperament temperament = Temperament.Passive;
    [Tooltip("Distance at which an aggressive creature can land an attack.")]
    public float attackRange = 1.5f;
    [Tooltip("Seconds between attacks - deliberately generous (\"reasonably big cooldown\" per design), not spammable.")]
    public float attackCooldown = 5f;
    [Tooltip("Expedition time (seconds) removed from the player per landed attack - see PlayerHealth.TakeDamage.")]
    public float attackDamage = 15f;
    [Tooltip("How long the attack animation holds the creature in place before it resumes chasing. Damage is applied immediately on landing the attack, not at the end of this window - this is purely how long the recovery/animation hold lasts.")]
    public float attackWindupDuration = 0.5f;
    [Tooltip("Seconds an aggressive creature freezes in place (no chasing, no attacking) after being hit without being stunned, before it resumes the chase. 0 = unaffected by hits.")]
    public float hitStaggerDuration = 0.6f;
    [Tooltip("Seconds an aggressive creature stays calm after a stun ends - it drops the chase and won't re-notice the player until this runs out. Without it, it would re-aggro the instant it recovers, since the player is standing right there.")]
    public float aggroLossDuration = 5f;

    [Header("Capture")]
    [Tooltip("NOT currently used - capture chance now comes from the capture minigame's hit ratio (see CaptureMinigameController). Left in place in case you want to fold it back in as a per-species multiplier later.")]
    [Range(0f, 1f)] public float baseCaptureChance = 0.5f;
    [Tooltip("Seconds the creature stays stunned/vulnerable after being hit with the stick.")]
    public float stunDuration = 3f;

    [Header("Sounds")]
    [Tooltip("Natural sounds it makes now and then while alive - one picked at random each time. Played in 3D from the critter (CreatureAudio).")]
    public AudioClip[] ambientSounds;
    [Tooltip("Seconds between natural sounds (random in range).")]
    public Vector2 ambientSoundInterval = new Vector2(6f, 15f);
    [Tooltip("Played when it is hit - also when a reckless critter runs into something.")]
    public AudioClip[] hitSounds;
    [Range(0f, 1f)] public float soundVolume = 1f;

    [Header("Resources")]
    [Tooltip("Index 0=Normal, 1=Uncommon, 2=Rare, 3=Legendary. The resource obtained when a creature of this species+rarity is turned into resources.")]
    public ResourceData[] resources = new ResourceData[4];

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

    /// <summary>The locked/silhouette thumbnail (icons[LockedIconIndex]), or null if this species doesn't have one authored yet.</summary>
    public Sprite GetLockedIcon()
    {
        return icons != null && icons.Length > LockedIconIndex ? icons[LockedIconIndex] : null;
    }

    /// <summary>The resource yielded by this species at a given rarity. Falls back to index 0 if the array is short.</summary>
    public ResourceData GetResource(Rarity rarity)
    {
        if (resources == null || resources.Length == 0) return null;
        int index = (int)rarity;
        return index < resources.Length ? resources[index] : resources[0];
    }

    /// <summary>
    /// How drawn to (lovedScore) vs. repelled by (hatedScore) this species is
    /// by the given scent profile - each axis on ExpeditionStewManager
    /// .GetScents()'s 0-scentMax scale (0 = that scent isn't present at all),
    /// normalized here to 0-1 by dividing by scentMax (pass ExpeditionStewManager.ScentMax). Only favoriteScent/hatedScent's own single axis
    /// is ever read - a stew that's maxed out on every OTHER axis still scores
    /// 0 here if none of them is this species' favorite or hated scent. Shared
    /// by Spawner (spawn-weight bias) and CreatureAI (detection-range bias) so
    /// both read the EXACT same match, just apply it differently.
    /// </summary>
    public void GetScentAffinity(float[] currentScents, float scentMax, out float lovedScore, out float hatedScore)
    {
        scentMax = Mathf.Max(1f, scentMax);
        lovedScore = GetScentValue(currentScents, favoriteScent) / scentMax;
        hatedScore = GetScentValue(currentScents, hatedScent) / scentMax;
    }

    private static float GetScentValue(float[] scents, ScentType scent)
    {
        if (scents == null) return 0f;
        int index = (int)scent;
        return index >= 0 && index < scents.Length ? scents[index] : 0f;
    }
}