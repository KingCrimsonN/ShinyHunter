using UnityEngine;

/// <summary>
/// Every tunable number driven by a creature's RARITY, for both the capture
/// minigame itself (barrier count/size/movement, health) and rarity-scaled
/// combat behaviour outside it (the post-hit dash). Indexed by
/// CreatureData.Rarity (0=Normal/"Regular" .. 3=Legendary/"Radiant") -
/// exactly 4 entries per array. One shared asset, read via
/// CaptureMinigameController.Instance (see its Get* methods) so CreatureAI
/// doesn't need its own reference - see decision log.
///
/// Tool-driven minigame numbers (time limit, attempt count, barrier speed
/// multiplier, extra-ingredient/family-bonus chances) live on ToolData
/// instead, since those vary per equipped tool, not per creature rarity.
/// Per-SPECIES combat numbers (aggression, attack stats, flee/chase speed)
/// live on CreatureData instead, since those vary per species, not per rarity.
/// </summary>
[CreateAssetMenu(fileName = "CaptureMinigameConfig", menuName = "ShinyHunt/Capture Minigame Config")]
public class CaptureMinigameConfig : ScriptableObject
{
    [Header("Barriers per rarity")]
    [Tooltip("Index 0=Normal(Regular), 1=Uncommon(Sparkling), 2=Rare(Shiny), 3=Legendary(Radiant).")]
    public int[] barrierCountPerRarity = { 2, 3, 3, 4 };
    [Tooltip("Arc width in degrees, per rarity - smaller = harder to hit. Same index order as above.")]
    public float[] barrierWidthDegreesPerRarity = { 45f, 30f, 22f, 16f };
    [Tooltip("Whether barriers orbit the wheel at all, per rarity - per the design, only Shiny (Rare) and Radiant (Legendary) move.")]
    public bool[] barriersMovePerRarity = { false, false, true, true };
    [Tooltip("Degrees/second barriers orbit the wheel, for rarities where barriersMovePerRarity is true - Radiant faster than Shiny. Further multiplied by the equipped tool's barrierSpeedMultiplier (see ToolData). All barriers move together as one rigid rotating ring (a single random direction rolled per minigame), not independently - they can never drift into overlapping each other.")]
    public float[] barrierOrbitSpeedPerRarity = { 0f, 0f, 25f, 55f };

    [Header("Health per rarity")]
    [Tooltip("Max health, per rarity - Regular/Sparkling/Shiny/Radiant. The base weapon (PlayerCapture) deals 10 damage per hit, so Regular stuns in 1 hit, Sparkling in 2, Shiny in 3, Radiant in 5 (at base hit power - Capture Power can raise this).")]
    public float[] healthPerRarity = { 10f, 20f, 30f, 50f };
    [Tooltip("Fraction of BASE (max) health restored when a stun ends WITHOUT a successful capture (timed out, or a failed TryCapture roll) - not a full heal, so repeated attempts wear the creature down across multiple encounters rather than resetting to full each time.")]
    [Range(0f, 1f)] public float failedCaptureHealthRestoreFraction = 0.5f;

    [Header("Dash per rarity (a hit that damages but doesn't stun)")]
    [Tooltip("Instant \"blink\" distance in the flee direction, applied ONCE the moment a dash triggers, before the speed boost kicks in - the snappy, visually-obvious part of the dash. 0 = no blink, just the speed boost.")]
    public float[] dashBlinkDistancePerRarity = { 0f, 1.5f, 2f, 2.5f };
    [Tooltip("Flee-speed multiplier applied briefly after a hit that damages but doesn't stun, per rarity - 1 = no dash. Regular defaults to no dash since it always stuns in one hit at base damage anyway (dashDurationPerRarity's Regular entry is 0, which alone disables it regardless of this value). Only applies to NON-aggressive creatures - see CreatureData.isAggressive / CreatureAI.OnHit.")]
    public float[] dashSpeedMultiplierPerRarity = { 1f, 1.3f, 1.6f, 2f };
    [Tooltip("How long (seconds) the dash speed boost lasts after a qualifying hit, per rarity. 0 = no dash for that rarity, regardless of the multiplier/blink above.")]
    public float[] dashDurationPerRarity = { 0f, 0.6f, 0.8f, 1f };

    public int GetBarrierCount(CreatureData.Rarity rarity) => GetValue(barrierCountPerRarity, rarity, 2);
    public float GetBarrierWidthDegrees(CreatureData.Rarity rarity) => GetValue(barrierWidthDegreesPerRarity, rarity, 30f);
    public bool GetBarriersMove(CreatureData.Rarity rarity) => GetValue(barriersMovePerRarity, rarity, false);
    public float GetBarrierOrbitSpeed(CreatureData.Rarity rarity) => GetValue(barrierOrbitSpeedPerRarity, rarity, 0f);
    public float GetMaxHealth(CreatureData.Rarity rarity) => GetValue(healthPerRarity, rarity, 10f);
    public float GetDashBlinkDistance(CreatureData.Rarity rarity) => GetValue(dashBlinkDistancePerRarity, rarity, 0f);
    public float GetDashSpeedMultiplier(CreatureData.Rarity rarity) => GetValue(dashSpeedMultiplierPerRarity, rarity, 1f);
    public float GetDashDuration(CreatureData.Rarity rarity) => GetValue(dashDurationPerRarity, rarity, 0f);

    private static T GetValue<T>(T[] array, CreatureData.Rarity rarity, T fallback)
    {
        int index = (int)rarity;
        return array != null && index >= 0 && index < array.Length ? array[index] : fallback;
    }
}
