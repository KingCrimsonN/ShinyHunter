using UnityEngine;

/// <summary>
/// Every tunable number the capture minigame's RARITY-driven behaviour uses
/// (barrier count/size/movement, creature health). Indexed by
/// CreatureData.Rarity (0=Normal/"Regular" .. 3=Legendary/"Radiant") -
/// exactly 4 entries per array. One shared asset, read via
/// CaptureMinigameController.Instance (see its Get* methods) so CreatureAI
/// doesn't need its own reference - see decision log.
///
/// Tool-driven minigame numbers (time limit, attempt count, barrier speed
/// multiplier, extra-ingredient/family-bonus chances) live on ToolData
/// instead, since those vary per equipped tool, not per creature rarity.
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

    public int GetBarrierCount(CreatureData.Rarity rarity) => GetValue(barrierCountPerRarity, rarity, 2);
    public float GetBarrierWidthDegrees(CreatureData.Rarity rarity) => GetValue(barrierWidthDegreesPerRarity, rarity, 30f);
    public bool GetBarriersMove(CreatureData.Rarity rarity) => GetValue(barriersMovePerRarity, rarity, false);
    public float GetBarrierOrbitSpeed(CreatureData.Rarity rarity) => GetValue(barrierOrbitSpeedPerRarity, rarity, 0f);
    public float GetMaxHealth(CreatureData.Rarity rarity) => GetValue(healthPerRarity, rarity, 10f);

    private static T GetValue<T>(T[] array, CreatureData.Rarity rarity, T fallback)
    {
        int index = (int)rarity;
        return array != null && index >= 0 && index < array.Length ? array[index] : fallback;
    }
}
