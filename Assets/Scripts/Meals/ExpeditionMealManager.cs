using UnityEngine;

/// <summary>
/// Holds the meal selected for the CURRENT/upcoming expedition and exposes
/// its effects as simple queries for other systems (CreatureAI, Spawner,
/// CaptureMinigameController, CreatureTransformStationUI) - none of them
/// need to know about MealInstance/MealModifierType directly, just call
/// these getters. Persistent, since the meal is picked in the hub but
/// consumed throughout the following expedition scene.
/// </summary>
public class ExpeditionMealManager : MonoBehaviour
{
    public static ExpeditionMealManager Instance { get; private set; }

    [SerializeField] private MealCalculationConfig config;

    public MealInstance ActiveMeal { get; private set; }

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
    /// Call when the player picks a meal at the exit door. Applies Time to
    /// PlayerHealth immediately; everything else is queried lazily by other
    /// systems during the run via the getters below - including Chrono
    /// Power, which no longer does anything here (see ApplyChronoBonusIfMatching).
    /// </summary>
    public void SetActiveMeal(MealInstance meal)
    {
        ActiveMeal = meal;
        if (meal == null) return;

        PlayerHealth.Instance.AddTemporaryMaxHealth(meal.timeSeconds);
    }

    public void ClearActiveMeal()
    {
        ActiveMeal = null;
    }

    /// <summary>
    /// The built-in meal that's always on offer at the exit door (e.g.
    /// "Auntie's Meal") so the player can always start an expedition. It's a
    /// fresh runtime MealInstance built from MealCalculationConfig - the config
    /// asset itself is shared and is never written to - flagged isDefault so
    /// it's never stored in a bowl or consumed. No modifier: just time and
    /// neutral scents by default (all tunable on the config).
    /// </summary>
    public MealInstance GetDefaultMeal()
    {
        var meal = new MealInstance
        {
            displayName = config != null ? config.defaultMealName : "Auntie's Meal",
            isDefault = true,
            modifierType = MealModifierType.None,
            modifierPower = 0f,
            scents = new float[] { 0f, 0f, 0f, 0f, 0f },
        };

        if (config == null)
        {
            Debug.LogWarning("ExpeditionMealManager: no MealCalculationConfig assigned - using fallback values for the default meal.");
            meal.timeSeconds = 120f;
            return meal;
        }

        meal.timeSeconds = Mathf.Min(config.defaultMealTimeSeconds, config.maxTimeSeconds);
        meal.tier = config.GetMealTier(meal.timeSeconds);
        meal.icon = config.defaultMealIcon;

        if (config.defaultMealScents != null)
        {
            for (int i = 0; i < meal.scents.Length && i < config.defaultMealScents.Length; i++)
                meal.scents[i] = Mathf.Clamp(config.defaultMealScents[i], 0f, config.scentMaxValue);
        }

        return meal;
    }

    // ---------------- Modifier queries ----------------

    /// <summary>
    /// Multiplier for CreatureAI's rarity-roll chances - ONLY for creatures
    /// whose family matches the active meal's RANDOMLY-ROLLED affected
    /// family (ActiveMeal.affectedFamily, NOT dominantFamily); every other
    /// family rolls at its normal odds. Shiny Power's TRIGGER stays
    /// rarity-based, not family-based - only its EFFECT is family-scoped,
    /// same as every other modifier below. See decision log.
    /// </summary>
    public float GetRarityChanceMultiplier(IngredientFamily creatureFamily)
    {
        if (ActiveMeal == null || ActiveMeal.modifierType != MealModifierType.ShinyPower) return 1f;
        if (creatureFamily != ActiveMeal.affectedFamily) return 1f;
        return 1f + ActiveMeal.modifierPower * config.shinyRarityMultiplierScale;
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
        if (ActiveMeal == null || ActiveMeal.modifierType != MealModifierType.IngredientPower) return false;
        if (family != ActiveMeal.affectedFamily) return false;
        return Random.value <= ActiveMeal.modifierPower * config.ingredientDoubleChanceScale;
    }

    /// <summary>Which family (if any) gets a spawn-weight boost, and by how much.</summary>
    public bool TryGetEncounterBoostFamily(out IngredientFamily family, out float weightMultiplier)
    {
        family = IngredientFamily.Freaky;
        weightMultiplier = 1f;

        if (ActiveMeal == null || ActiveMeal.modifierType != MealModifierType.EncounterPower) return false;

        family = ActiveMeal.affectedFamily;
        weightMultiplier = 1f + ActiveMeal.modifierPower * config.encounterFamilyWeightScale;
        return true;
    }

    /// <summary>
    /// Flat bonus (0-1) added to a capture attempt's final chance, for
    /// creatures of the affected family only - 0 for every other family.
    /// </summary>
    public float GetCaptureChanceBonus(IngredientFamily creatureFamily)
    {
        if (ActiveMeal == null || ActiveMeal.modifierType != MealModifierType.CapturePower) return 0f;
        if (creatureFamily != ActiveMeal.affectedFamily) return 0f;
        return ActiveMeal.modifierPower * config.captureChanceBonusScale;
    }

    /// <summary>How many capture-wheel hit areas should start already marked as hit, for creatures of the affected family only.</summary>
    public int GetCaptureAutoBreakCount(IngredientFamily creatureFamily)
    {
        if (ActiveMeal == null || ActiveMeal.modifierType != MealModifierType.CapturePower) return 0;
        if (creatureFamily != ActiveMeal.affectedFamily) return 0;
        return Mathf.RoundToInt(ActiveMeal.modifierPower * config.captureMaxAutoBreakAreas);
    }

    /// <summary>Multiplier for creature flee speed / detection radius (less than 1 = slower, blinder) - only for creatures of the affected family; every other family gets 1 (no effect).</summary>
    public float GetSoothingMultiplier(IngredientFamily creatureFamily)
    {
        if (ActiveMeal == null || ActiveMeal.modifierType != MealModifierType.SoothingPower) return 1f;
        if (creatureFamily != ActiveMeal.affectedFamily) return 1f;
        return 1f - ActiveMeal.modifierPower * config.soothingMaxSlowdown;
    }

    /// <summary>Weapon hit-power bonus (0 = no bonus) for a hit against a creature of the given family - Capture Power, family-scoped like everything else. Actual damage = baseDamage * (1 + this).</summary>
    public float GetHitPowerBonus(IngredientFamily creatureFamily)
    {
        if (ActiveMeal == null || ActiveMeal.modifierType != MealModifierType.CapturePower) return 0f;
        if (creatureFamily != ActiveMeal.affectedFamily) return 0f;
        return ActiveMeal.modifierPower * config.captureHitPowerBonusScale;
    }

    /// <summary>Extra seconds added to the capture minigame's time limit for a creature of the given family - Capture Power, family-scoped.</summary>
    public float GetCaptureTimeBonus(IngredientFamily creatureFamily)
    {
        if (ActiveMeal == null || ActiveMeal.modifierType != MealModifierType.CapturePower) return 0f;
        if (creatureFamily != ActiveMeal.affectedFamily) return 0f;
        return ActiveMeal.modifierPower * config.captureTimeBonusScale;
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
        if (ActiveMeal == null || ActiveMeal.modifierType != MealModifierType.ChronoPower) return;
        if (capturedFamily != ActiveMeal.affectedFamily) return;
        if (config == null || PlayerHealth.Instance == null) return;

        // AddTime, not AddTemporaryMaxHealth: raising only the maximum mid-run gave
        // no time at all and made the dial (time left / maximum) jump DOWN.
        float bonus = ActiveMeal.modifierPower * config.chronoMaxBonusPerCapture;
        PlayerHealth.Instance.AddTime(bonus);
    }

    /// <summary>The scent scale's ceiling (MealCalculationConfig.scentMaxValue) - divide a scent value by this to get 0-1. Everything that normalizes a scent goes through here instead of hardcoding a number.</summary>
    public float ScentMax => config != null ? Mathf.Max(1f, config.scentMaxValue) : 500f;

    /// <summary>Player's current scent profile (Sweet, Fresh, Putrid, Metallic, Marine, each 0 to ScentMax) - neutral (all 0, i.e. "not present") with no active meal.</summary>
    public float[] GetScents()
    {
        return ActiveMeal != null ? ActiveMeal.scents : new float[] { 0f, 0f, 0f, 0f, 0f };
    }

    /// <summary>
    /// Spawn-weight multiplier for one species given how strongly the meal hits
    /// its favorite (lovedScore) and hated (hatedScore) scent axes, both 0-1:
    /// atMax ^ (loved - hated). So with atMax = 4: half the cap on the favorite
    /// axis = x2, the full cap = x4, nothing = x1; the hated axis is the exact
    /// mirror (full = x0.25). Exponential on purpose - "every half of the max
    /// doubles it".
    /// </summary>
    public float GetScentSpawnMultiplier(float lovedScore, float hatedScore)
    {
        float atMax = config != null ? Mathf.Max(1f, config.scentSpawnMultiplierAtMax) : 4f;
        return Mathf.Pow(atMax, Mathf.Clamp01(lovedScore) - Mathf.Clamp01(hatedScore));
    }

    /// <summary>Population-cap fraction (0-1) Spawner uses when the active meal's scent is at its weakest - see MealCalculationConfig.scentPopulationMinFraction.</summary>
    public float GetScentPopulationMinFraction()
    {
        return config != null ? config.scentPopulationMinFraction : 1f;
    }
}