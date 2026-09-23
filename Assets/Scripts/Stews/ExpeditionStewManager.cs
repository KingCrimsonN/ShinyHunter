using UnityEngine;

/// <summary>
/// Holds the stew selected for the CURRENT/upcoming expedition and exposes
/// its effects as simple queries for other systems (CreatureAI, Spawner,
/// CaptureMinigameController, CreatureTransformStationUI) - none of them
/// need to know about StewInstance/StewModifierType directly, just call
/// these getters. Persistent, since the stew is picked in the hub but
/// consumed throughout the following expedition scene.
/// </summary>
public class ExpeditionStewManager : MonoBehaviour
{
    public static ExpeditionStewManager Instance { get; private set; }

    [SerializeField] private StewCalculationConfig config;

    public StewInstance ActiveStew { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Call when the player picks a stew at the exit door. Applies Time to
    /// PlayerHealth immediately; everything else is queried lazily by other
    /// systems during the run via the getters below - including Chrono
    /// Power, which no longer does anything here (see ApplyChronoBonusIfMatching).
    /// </summary>
    public void SetActiveStew(StewInstance stew)
    {
        ActiveStew = stew;
        if (stew == null) return;

        PlayerHealth.Instance.AddTemporaryMaxHealth(stew.timeSeconds);
    }

    public void ClearActiveStew()
    {
        ActiveStew = null;
    }

    /// <summary>
    /// The built-in stew that's always on offer at the exit door (e.g.
    /// "Auntie's Stew") so the player can always start an expedition. It's a
    /// fresh runtime StewInstance built from StewCalculationConfig - the config
    /// asset itself is shared and is never written to - flagged isDefault so
    /// it's never stored in a bowl or consumed. No modifier: just time and
    /// neutral scents by default (all tunable on the config).
    /// </summary>
    public StewInstance GetDefaultStew()
    {
        var stew = new StewInstance
        {
            displayName = config != null ? config.defaultStewName : "Auntie's Stew",
            isDefault = true,
            modifierType = StewModifierType.None,
            modifierPower = 0f,
            scents = new float[] { 0f, 0f, 0f, 0f, 0f },
        };

        if (config == null)
        {
            Debug.LogWarning("ExpeditionStewManager: no StewCalculationConfig assigned - using fallback values for the default stew.");
            stew.timeSeconds = 120f;
            return stew;
        }

        stew.timeSeconds = Mathf.Min(config.defaultStewTimeSeconds, config.maxTimeSeconds);
        stew.icon = config.defaultStewIcon;

        if (config.defaultStewScents != null)
        {
            for (int i = 0; i < stew.scents.Length && i < config.defaultStewScents.Length; i++)
                stew.scents[i] = Mathf.Clamp(config.defaultStewScents[i], 0f, 100f);
        }

        return stew;
    }

    // ---------------- Modifier queries ----------------

    /// <summary>
    /// Multiplier for CreatureAI's rarity-roll chances - ONLY for creatures
    /// whose family matches the active stew's RANDOMLY-ROLLED affected
    /// family (ActiveStew.affectedFamily, NOT dominantFamily); every other
    /// family rolls at its normal odds. Shiny Power's TRIGGER stays
    /// rarity-based, not family-based - only its EFFECT is family-scoped,
    /// same as every other modifier below. See decision log.
    /// </summary>
    public float GetRarityChanceMultiplier(IngredientFamily creatureFamily)
    {
        if (ActiveStew == null || ActiveStew.modifierType != StewModifierType.ShinyPower) return 1f;
        if (creatureFamily != ActiveStew.affectedFamily) return 1f;
        return 1f + ActiveStew.modifierPower * config.shinyRarityMultiplierScale;
    }

    /// <summary>
    /// Call once per CAPTURED creature (not per transformed one - see
    /// decision log), for its family - true if it should be flagged to yield
    /// double resources once eventually transformed. The flag itself is
    /// stored on InventoryManager (AddCreature's doubleYield parameter), not
    /// here - this only answers "should THIS capture roll get one".
    /// </summary>
    public bool TryRollDoubleIngredients(IngredientFamily family)
    {
        if (ActiveStew == null || ActiveStew.modifierType != StewModifierType.IngredientPower) return false;
        if (family != ActiveStew.affectedFamily) return false;
        return Random.value <= ActiveStew.modifierPower * config.ingredientDoubleChanceScale;
    }

    /// <summary>Which family (if any) gets a spawn-weight boost, and by how much.</summary>
    public bool TryGetEncounterBoostFamily(out IngredientFamily family, out float weightMultiplier)
    {
        family = IngredientFamily.Freaky;
        weightMultiplier = 1f;

        if (ActiveStew == null || ActiveStew.modifierType != StewModifierType.EncounterPower) return false;

        family = ActiveStew.affectedFamily;
        weightMultiplier = 1f + ActiveStew.modifierPower * config.encounterFamilyWeightScale;
        return true;
    }

    /// <summary>
    /// Flat bonus (0-1) added to a capture attempt's final chance, for
    /// creatures of the affected family only - 0 for every other family.
    /// </summary>
    public float GetCaptureChanceBonus(IngredientFamily creatureFamily)
    {
        if (ActiveStew == null || ActiveStew.modifierType != StewModifierType.CapturePower) return 0f;
        if (creatureFamily != ActiveStew.affectedFamily) return 0f;
        return ActiveStew.modifierPower * config.captureChanceBonusScale;
    }

    /// <summary>How many capture-wheel hit areas should start already marked as hit, for creatures of the affected family only.</summary>
    public int GetCaptureAutoBreakCount(IngredientFamily creatureFamily)
    {
        if (ActiveStew == null || ActiveStew.modifierType != StewModifierType.CapturePower) return 0;
        if (creatureFamily != ActiveStew.affectedFamily) return 0;
        return Mathf.RoundToInt(ActiveStew.modifierPower * config.captureMaxAutoBreakAreas);
    }

    /// <summary>Multiplier for creature flee speed / detection radius (less than 1 = slower, blinder) - only for creatures of the affected family; every other family gets 1 (no effect).</summary>
    public float GetSoothingMultiplier(IngredientFamily creatureFamily)
    {
        if (ActiveStew == null || ActiveStew.modifierType != StewModifierType.SoothingPower) return 1f;
        if (creatureFamily != ActiveStew.affectedFamily) return 1f;
        return 1f - ActiveStew.modifierPower * config.soothingMaxSlowdown;
    }

    /// <summary>Weapon hit-power bonus (0 = no bonus) for a hit against a creature of the given family - Capture Power, family-scoped like everything else. Actual damage = baseDamage * (1 + this).</summary>
    public float GetHitPowerBonus(IngredientFamily creatureFamily)
    {
        if (ActiveStew == null || ActiveStew.modifierType != StewModifierType.CapturePower) return 0f;
        if (creatureFamily != ActiveStew.affectedFamily) return 0f;
        return ActiveStew.modifierPower * config.captureHitPowerBonusScale;
    }

    /// <summary>Extra seconds added to the capture minigame's time limit for a creature of the given family - Capture Power, family-scoped.</summary>
    public float GetCaptureTimeBonus(IngredientFamily creatureFamily)
    {
        if (ActiveStew == null || ActiveStew.modifierType != StewModifierType.CapturePower) return 0f;
        if (creatureFamily != ActiveStew.affectedFamily) return 0f;
        return ActiveStew.modifierPower * config.captureTimeBonusScale;
    }

    /// <summary>
    /// Call once per successful capture. Chrono Power's ENTIRE mechanic now
    /// lives here: if it's active and the captured creature's family matches
    /// the affected family, grants bonus CURRENT-run expedition time right
    /// away (0 to chronoMaxBonusPerCapture seconds, scaling with
    /// modifierPower - up to a minute per capture at full power). No-op for
    /// every other family, and a no-op entirely when Chrono Power isn't
    /// active. Completely replaces the old "bank a bonus for the NEXT
    /// expedition" design - see decision log.
    /// </summary>
    public void ApplyChronoBonusIfMatching(IngredientFamily capturedFamily)
    {
        if (ActiveStew == null || ActiveStew.modifierType != StewModifierType.ChronoPower) return;
        if (capturedFamily != ActiveStew.affectedFamily) return;
        if (config == null || PlayerHealth.Instance == null) return;

        float bonus = ActiveStew.modifierPower * config.chronoMaxBonusPerCapture;
        PlayerHealth.Instance.AddTemporaryMaxHealth(bonus);
    }

    /// <summary>Player's current scent profile (Sweet, Fresh, Putrid, Metallic, Marine, each 0-100) - neutral (all 0, i.e. "not present") with no active stew.</summary>
    public float[] GetScents()
    {
        return ActiveStew != null ? ActiveStew.scents : new float[] { 0f, 0f, 0f, 0f, 0f };
    }

    /// <summary>Population-cap fraction (0-1) Spawner uses when the active stew's scent is at its weakest - see StewCalculationConfig.scentPopulationMinFraction.</summary>
    public float GetScentPopulationMinFraction()
    {
        return config != null ? config.scentPopulationMinFraction : 1f;
    }
}