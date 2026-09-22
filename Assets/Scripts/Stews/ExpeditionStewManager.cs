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
    /// Call when the player picks a stew at the exit door. Applies Time
    /// (plus any bonus banked by a PREVIOUS stew's Chrono Power) to
    /// PlayerHealth immediately; everything else is queried lazily by other
    /// systems during the run via the getters below.
    /// </summary>
    public void SetActiveStew(StewInstance stew)
    {
        ActiveStew = stew;
        if (stew == null) return;

        float bankedBonus = PlayerHealth.Instance.ConsumeBankedChronoBonus();
        PlayerHealth.Instance.AddTemporaryMaxHealth(stew.timeSeconds + bankedBonus);

        if (stew.modifierType == StewModifierType.ChronoPower)
        {
            float bonusForNextRun = stew.modifierPower * config.chronoMaxBonusSeconds;
            PlayerHealth.Instance.AddBankedChronoBonus(bonusForNextRun);
        }
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
            scents = new float[] { 1f, 1f, 1f, 1f, 1f },
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
                stew.scents[i] = Mathf.Clamp(config.defaultStewScents[i], 1f, 5f);
        }

        return stew;
    }

    // ---------------- Modifier queries ----------------

    /// <summary>Multiplier for CreatureAI's rarity-roll chances.</summary>
    public float GetRarityChanceMultiplier()
    {
        if (ActiveStew == null || ActiveStew.modifierType != StewModifierType.ShinyPower) return 1f;
        return 1f + ActiveStew.modifierPower * config.shinyRarityMultiplierScale;
    }

    /// <summary>Roll once per creature being transformed, for its family - true if it should yield double resources.</summary>
    public bool TryRollDoubleIngredients(IngredientFamily family)
    {
        if (ActiveStew == null || ActiveStew.modifierType != StewModifierType.IngredientPower) return false;
        if (family != IngredientFamily.Animal) return false; // Ingredient Power is Animal/Bugs-only by design
        return Random.value <= ActiveStew.modifierPower * config.ingredientDoubleChanceScale;
    }

    /// <summary>Which family (if any) gets a spawn-weight boost, and by how much.</summary>
    public bool TryGetEncounterBoostFamily(out IngredientFamily family, out float weightMultiplier)
    {
        family = IngredientFamily.Stranger;
        weightMultiplier = 1f;

        if (ActiveStew == null || ActiveStew.modifierType != StewModifierType.EncounterPower) return false;

        weightMultiplier = 1f + ActiveStew.modifierPower * config.encounterFamilyWeightScale;
        return true;
    }

    /// <summary>Flat bonus (0-1) added to a capture attempt's final chance.</summary>
    public float GetCaptureChanceBonus()
    {
        if (ActiveStew == null || ActiveStew.modifierType != StewModifierType.CapturePower) return 0f;
        return ActiveStew.modifierPower * config.captureChanceBonusScale;
    }

    /// <summary>How many capture-wheel hit areas should start already marked as hit.</summary>
    public int GetCaptureAutoBreakCount()
    {
        if (ActiveStew == null || ActiveStew.modifierType != StewModifierType.CapturePower) return 0;
        return Mathf.RoundToInt(ActiveStew.modifierPower * config.captureMaxAutoBreakAreas);
    }

    /// <summary>Multiplier for creature flee speed / detection radius (less than 1 = slower, blinder).</summary>
    public float GetSoothingMultiplier()
    {
        if (ActiveStew == null || ActiveStew.modifierType != StewModifierType.SoothingPower) return 1f;
        return 1f - ActiveStew.modifierPower * config.soothingMaxSlowdown;
    }

    /// <summary>Player's current scent profile (Sweet, Fresh, Putrid, Metallic, Marine, each 1-5) - neutral (all 1) with no active stew.</summary>
    public float[] GetScents()
    {
        return ActiveStew != null ? ActiveStew.scents : new float[] { 1f, 1f, 1f, 1f, 1f };
    }
}