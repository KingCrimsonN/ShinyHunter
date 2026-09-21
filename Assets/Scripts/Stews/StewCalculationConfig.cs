using UnityEngine;

/// <summary>
/// Every tunable number StewCalculator and ExpeditionStewManager use. One
/// shared asset, so you can rebalance the whole system without touching code.
/// </summary>
[CreateAssetMenu(fileName = "StewCalculationConfig", menuName = "ShinyHunt/Stew Calculation Config")]
public class StewCalculationConfig : ScriptableObject
{
    [Header("Time")]
    [Tooltip("Seconds contributed per ingredient, indexed by CreatureData.Rarity (Normal..Legendary).")]
    public float[] timePerRarity = { 60f, 90f, 150f, 240f };
    [Tooltip("Hard cap on total stew time - \"capped by the bowl\".")]
    public float maxTimeSeconds = 600f;

    [Header("Scents")]
    [Tooltip("How much raw scent total (summed across ingredients) is needed per +1 on the displayed 1-5 scale.")]
    public float scentPerPoint = 3f;

    [Header("Modifier Selection")]
    [Tooltip("Minimum dominance ratio (0-1) required for ANY modifier to activate. Below this, the stew gets StewModifierType.None.")]
    public float modifierActivationThreshold = 0.3f;

    [Header("Modifier Effect Scales")]
    [Tooltip("Rarity-roll chances are multiplied by (1 + power * this) when Shiny Power is active.")]
    public float shinyRarityMultiplierScale = 1.5f;
    [Tooltip("Chance (0-1) of double resources on transform, at power = 1.0, when Ingredient Power is active.")]
    public float ingredientDoubleChanceScale = 0.5f;
    [Tooltip("Spawn-weight multiplier for the boosted family, at power = 1.0, when Encounter Power is active.")]
    public float encounterFamilyWeightScale = 1.0f;
    [Tooltip("Seconds banked for the NEXT expedition, at power = 1.0, when Chrono Power is active.")]
    public float chronoMaxBonusSeconds = 60f;
    [Tooltip("Flat capture-chance bonus (0-1), at power = 1.0, when Capture Power is active.")]
    public float captureChanceBonusScale = 0.3f;
    [Tooltip("Hit areas auto-marked as hit at the start of the wheel, at power = 1.0, when Capture Power is active.")]
    public int captureMaxAutoBreakAreas = 2;
    [Tooltip("Flee speed / detection radius reduction (0-1), at power = 1.0, when Soothing Power is active.")]
    public float soothingMaxSlowdown = 0.5f;
}