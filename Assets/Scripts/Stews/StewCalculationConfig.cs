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
    [Tooltip("Raw scent points needed PER CAULDRON SLOT for an axis to reach its max (100) if every slot were filled with ingredients this strong - e.g. with 8 total cauldron slots and this at 5, hitting 100 on one axis needs 40 raw points there (~8 ingredients averaging 5 each). Using fewer ingredients than the cauldron's total capacity always gives proportionally less, however strong they are. See decision log.")]
    public float scentPerPoint = 5f;
    [Tooltip("Population-cap fraction (0-1) a Spawner uses when the active stew's scent is at its weakest (every axis at 0, e.g. no stew at all). Scales up to the full populationCap as the loudest single scent axis approaches 100 - a weak-smelling stew draws fewer creatures. 1 = scent strength doesn't affect population at all.")]
    [Range(0f, 1f)]
    public float scentPopulationMinFraction = 0.4f;

    [Header("Modifier Selection")]
    [Tooltip("Minimum dominance ratio (0-1) required for ANY modifier to activate. Below this, the stew gets StewModifierType.None.")]
    public float modifierActivationThreshold = 0.3f;

    [Header("Default Stew (always available at the exit door)")]
    [Tooltip("Shown as the stew's name. This stew is never stored in a bowl and never used up - it's the fallback when the player has nothing better to take.")]
    public string defaultStewName = "Auntie's Stew";
    [Tooltip("Expedition time this stew gives, in seconds. Keep it modest so brewing a real stew is worth it.")]
    public float defaultStewTimeSeconds = 120f;
    [Tooltip("Sweet, Fresh, Putrid, Metallic, Marine - each 0-100. All 0 = neutral (same as taking no stew).")]
    public float[] defaultStewScents = { 0f, 0f, 0f, 0f, 0f };
    [Tooltip("Icon shown in the carousel. If left empty, the carousel entry keeps whatever sprite its prefab has.")]
    public Sprite defaultStewIcon;

    // Every modifier below applies ONLY to creatures/captures of the active
    // stew's randomly-rolled affected family (StewInstance.affectedFamily) -
    // see decision log.
    [Header("Modifier Effect Scales")]
    [Tooltip("Rarity-roll chances are multiplied by (1 + power * this) when Shiny Power is active, for creatures of the affected family only.")]
    public float shinyRarityMultiplierScale = 1.5f;
    [Tooltip("Chance (0-1) of double resources on transform, at power = 1.0, when Ingredient Power is active and the CAPTURED creature was of the affected family. Rolled once AT CAPTURE, not at transform time - see decision log.")]
    public float ingredientDoubleChanceScale = 0.5f;
    [Tooltip("Spawn-weight multiplier for the affected family, at power = 1.0, when Encounter Power is active.")]
    public float encounterFamilyWeightScale = 1.0f;
    [Tooltip("Bonus expedition time (seconds), at power = 1.0, granted for EACH capture of the affected family while Chrono Power is active - up to a minute per capture at full power. Applied immediately to the CURRENT run, not banked for later - see decision log.")]
    public float chronoMaxBonusPerCapture = 60f;
    [Tooltip("Flat capture-chance bonus (0-1), at power = 1.0, when Capture Power is active and the targeted creature is of the affected family.")]
    public float captureChanceBonusScale = 0.3f;
    [Tooltip("Hit areas auto-marked as hit at the start of the wheel, at power = 1.0, when Capture Power is active and the targeted creature is of the affected family.")]
    public int captureMaxAutoBreakAreas = 2;
    [Tooltip("Flee speed / detection radius reduction (0-1), at power = 1.0, when Soothing Power is active, for creatures of the affected family only.")]
    public float soothingMaxSlowdown = 0.5f;
}