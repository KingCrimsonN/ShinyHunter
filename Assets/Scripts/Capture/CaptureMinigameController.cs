using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Drives the capture minigame: spins a needle around a wheel with randomly
/// placed hit arcs ("barriers"). Player gets a number of attempts and a time
/// limit to hit as many as possible; final capture chance = hits/total
/// (plus Capture Power's stew bonus and the tool's own family bonus, if
/// either applies - see EndMinigame).
///
/// Every run is configured from three independent sources (see BeginCapture
/// and decision log):
///   - The EQUIPPED TOOL (ToolData): time limit, attempt count, barrier
///     orbit-speed multiplier, extra-ingredient chance, family capture bonus.
///   - The CREATURE'S RARITY (CaptureMinigameConfig): barrier count/width,
///     and whether/how fast barriers orbit the wheel (Shiny/Radiant only).
///   - The ACTIVE STEW (ExpeditionStewManager): Capture Power's time bonus
///     and hit-power bonus (the latter applied by PlayerCapture, not here),
///     both family-scoped like every other modifier.
///
/// Freezes player movement/capture input for the duration (camera stays
/// static - see class docs on the project's rendering approach for why this
/// is a plain Screen Space Overlay Canvas rather than anything world-space).
///
/// Scene-scoped (holds direct references to player components), so unlike
/// InventoryManager/ToolInventoryManager this is NOT DontDestroyOnLoad.
/// </summary>
public class CaptureMinigameController : MonoBehaviour
{
    public static CaptureMinigameController Instance { get; private set; }

    [Header("Player refs (disabled while minigame is active)")]
    [SerializeField] private FirstPersonController playerMovement;
    [SerializeField] private PlayerCapture playerCapture;
    [SerializeField] private ToolEquipController toolEquip;

    [Header("Balance Config")]
    [Tooltip("Rarity-driven barrier count/size/movement and creature health. See CaptureMinigameConfig - CreatureAI reads health values through this controller (GetMaxHealthForRarity / GetFailedCaptureHealthRestoreFraction).")]
    [SerializeField] private CaptureMinigameConfig config;

    [Header("UI - Attempts")]
    [SerializeField] private GameObject attemptPrefab;
    [SerializeField] private GameObject[] attemptIcons;
    [SerializeField] private GameObject attemptsParent;

    [Header("UI - Popup")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Image centerIcon;
    [Tooltip("Shown if no tool is currently equipped.")]
    [SerializeField] private Sprite defaultCenterIcon;
    [SerializeField] private Slider timerSlider;
    [SerializeField] private TMP_Text attemptsText;

    [Header("UI - Wheel")]
    [Tooltip("The needle's own RectTransform, pivot at its base (e.g. 0.5, 0), positioned at the wheel's center.")]
    [SerializeField] private RectTransform needleTransform;
    [SerializeField] private Animator needleAnimator;
    [Tooltip("Parent for spawned hit-area arcs, positioned at the wheel's center (pivot 0.5, 0.5).")]
    [SerializeField] private RectTransform hitAreaParent;
    [SerializeField] private CaptureHitAreaUI hitAreaPrefab;

    [Header("Needle Marks")]
    [SerializeField] private GameObject needleHitMark;
    [Tooltip("Distance from the wheel's center to place needle marks - should roughly match the doll sprite's edge radius, in this canvas's local UI units (pixels under the Canvas). Tune this by eye once the doll art is in.")]
    [SerializeField] private float needleMarkRadius = 100f;
    [Tooltip("If true, a mark is left for every attempt (hit or miss). If false, only successful hits leave one.")]
    [SerializeField] private bool markOnMissToo = true;

    [Header("Default Settings (fallbacks - see Balance Config / ToolData for the real per-run values)")]
    [Tooltip("Fallback barrier count/attempt count, used only if config or the equipped tool is unassigned.")]
    [SerializeField] private int defaultHitAreaCount = 3;
    [Tooltip("Fallback barrier width, used only if config is unassigned.")]
    [SerializeField] private float defaultHitAreaWidthDegrees = 30f;
    [Tooltip("Degrees per second the NEEDLE spins - unaffected by rarity/tool, unlike the barriers themselves.")]
    [SerializeField] private float defaultNeedleSpeed = 180f;
    [Tooltip("Fallback time limit, used only if the equipped tool is unassigned.")]
    [SerializeField] private float defaultTimeLimit = 5f;
    [Tooltip("Minimum angular gap enforced between adjacent arcs, on top of their width, so they never touch or overlap.")]
    [SerializeField] private float minGapBetweenAreasDegrees = 10f;
    [SerializeField] private KeyCode hitKey = KeyCode.Mouse0;
    [SerializeField] private float defaultCooldown = 0.3f;

    [Header("Audio")]
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip missSound;
    [SerializeField] private AudioClip successSound;
    [SerializeField] private AudioClip failSound;

    /// <summary>Fires each time the player scores a hit on an arc.</summary>
    public event Action OnHitScored;
    /// <summary>Fires each time the player's press misses every arc.</summary>
    public event Action OnMiss;
    /// <summary>Fires when the minigame ends, with whether the capture succeeded.</summary>
    public event Action<bool> OnMinigameEnded;

    public bool IsRunning { get; private set; }

    private ICapturable targetCreature;
    private CreatureData creatureData;
    private readonly List<CaptureHitAreaUI> activeHitAreas = new List<CaptureHitAreaUI>();
    private readonly List<GameObject> activeNeedleMarks = new List<GameObject>();

    private int hitAreaCount;
    private float hitAreaWidthDegrees;
    private float needleSpeed;
    private float timeRemaining;
    private int attemptsRemaining;
    private int hitsScored;
    private float needleAngle;

    private bool ending;
    private bool cooldown;

    // ---------------- Per-run config, resolved once in BeginCapture from the equipped tool + creature rarity + active stew ----------------

    /// <summary>Whichever tool triggered this attempt (e.g. the doll) - cached once at the start, since the player can't switch tools while frozen mid-minigame anyway. Null-safe everywhere it's read.</summary>
    private ToolData equippedToolForThisRun;
    private float timeLimitForThisRun;
    private int attemptsAtStart;

    // ---------------- Moving barriers (Shiny/Radiant only - see CaptureMinigameConfig.barriersMovePerRarity) ----------------

    private bool barriersMoving;
    private float barrierOrbitSpeed;
    private float barrierOrbitDirection;
    private float barrierOrbitOffset;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (popupRoot != null) popupRoot.SetActive(false);
    }

    private void Update()
    {
        if (!IsRunning) return;

        TickNeedle();
        TickBarrierOrbit();
        TickTimer();

        if (Input.GetKeyDown(hitKey))
            HandleHitAttempt();

        if (timeRemaining <= 0f || attemptsRemaining <= 0 || hitsScored >= hitAreaCount)
            if (!ending)
            {
                ending = true;
                StartCoroutine(WaitAndEnd(0.5f)); // short delay so player sees the last hit/miss feedback
            }

    }

    /// <summary>Max health for a creature of the given rarity - see CaptureMinigameConfig.healthPerRarity. Falls back to 10 if no config is assigned.</summary>
    public float GetMaxHealthForRarity(CreatureData.Rarity rarity)
    {
        return config != null ? config.GetMaxHealth(rarity) : 10f;
    }

    /// <summary>Fraction of base health restored when a stun ends without a capture - see CaptureMinigameConfig.failedCaptureHealthRestoreFraction. Falls back to 0.5 if no config is assigned.</summary>
    public float GetFailedCaptureHealthRestoreFraction()
    {
        return config != null ? config.failedCaptureHealthRestoreFraction : 0.5f;
    }

    /// <summary>
    /// Entry point - call this instead of ICapturable.TryCapture directly.
    /// Configures the whole run from three sources (see class docs and
    /// decision log): the equipped tool (time limit, attempt count, barrier
    /// speed multiplier, family capture bonus), the creature's rarity
    /// (barrier count/size, whether/how fast they move - CaptureMinigameConfig),
    /// and the active stew (Capture Power's time bonus, family-scoped).
    /// </summary>
    /// <returns>True if the minigame started. False if it didn't (already running, or the creature is gone / no longer stunned) - callers must not consume anything for a capture that never began.</returns>
    public bool BeginCapture(ICapturable creature)
    {
        if (IsRunning || creature == null || !creature.IsStunned) return false;
        if (popupRoot != null) popupRoot.SetActive(true);


        targetCreature = creature;
        creatureData = creature.Data;
        // Keeps the creature stunned until EndMinigame resolves the attempt -
        // the wheel's own timer plus the end-of-minigame delay can outlast the
        // creature's normal stun, which used to discard the result.
        targetCreature.StartCapture();

        ending = false;
        cooldown = false;

        // Cached once - the player can't swap tools while frozen mid-minigame
        // (ToolEquipController is disabled by PlayerStateManager.Freeze below),
        // so re-fetching later would only risk reading something stale anyway.
        equippedToolForThisRun = ToolInventoryManager.Instance != null ? ToolInventoryManager.Instance.EquippedSlot?.data : null;

        CreatureData.Rarity rarity = creature.Rarity;
        hitAreaCount = config != null ? config.GetBarrierCount(rarity) : defaultHitAreaCount;
        hitAreaWidthDegrees = config != null ? config.GetBarrierWidthDegrees(rarity) : defaultHitAreaWidthDegrees;

        barriersMoving = config != null && config.GetBarriersMove(rarity);
        float toolSpeedMultiplier = equippedToolForThisRun != null ? equippedToolForThisRun.barrierSpeedMultiplier : 1f;
        barrierOrbitSpeed = (config != null ? config.GetBarrierOrbitSpeed(rarity) : 0f) * toolSpeedMultiplier;
        barrierOrbitDirection = UnityEngine.Random.value < 0.5f ? 1f : -1f; // not always clockwise
        barrierOrbitOffset = 0f;

        needleSpeed = defaultNeedleSpeed;

        float toolTimeLimit = equippedToolForThisRun != null ? equippedToolForThisRun.minigameTimeLimit : defaultTimeLimit;
        float captureTimeBonus = ExpeditionStewManager.Instance != null && creatureData != null
            ? ExpeditionStewManager.Instance.GetCaptureTimeBonus(creatureData.family)
            : 0f;
        timeLimitForThisRun = toolTimeLimit + captureTimeBonus;
        timeRemaining = timeLimitForThisRun;

        attemptsAtStart = equippedToolForThisRun != null ? equippedToolForThisRun.minigameAttemptCount : hitAreaCount;
        attemptsRemaining = attemptsAtStart;
        hitsScored = 0;
        needleAngle = 0f;
        IsRunning = true;

        PlayerStateManager.Instance.Freeze();

        SetupAttemptsUI();
        SetupCenterIcon();
        SpawnHitAreas();
        ApplyCapturePowerAutoBreak();
        ClearNeedleMarks(); // defensive - a previous session should have already cleared these in EndMinigame
        UpdateTimerUI();
        UpdateAttemptsUI();
        return true;
    }

    /// <summary>Capture Power head-start: marks some hit areas as already hit, without spending attempts. Only if this creature's family matches the stew's affected family.</summary>
    private void ApplyCapturePowerAutoBreak()
    {
        if (ExpeditionStewManager.Instance == null || creatureData == null) return;

        int autoBreakCount = ExpeditionStewManager.Instance.GetCaptureAutoBreakCount(creatureData.family);
        for (int i = 0; i < autoBreakCount && i < activeHitAreas.Count; i++)
        {
            activeHitAreas[i].MarkHit();
            hitsScored++;
        }
    }

    private void SetupAttemptsUI()
    {
        if (attemptsParent != null)
        {
            attemptIcons = new GameObject[attemptsAtStart];
            for (int i = 0; i < attemptsAtStart; i++)
            {
                var icon = Instantiate(attemptPrefab, attemptsParent.transform);
                attemptIcons[i] = icon;
            }
        }
    }

    private void ClearAttemptsUI()
    {
        if (attemptsParent != null)
        {
            foreach (Transform child in attemptsParent.transform)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void TickNeedle()
    {
        needleAngle = (needleAngle + needleSpeed * Time.deltaTime) % 360f;

        if (needleTransform != null)
            needleTransform.localRotation = Quaternion.Euler(0f, 0f, -needleAngle); // see CaptureHitAreaUI for the sign note
    }

    private void TickTimer()
    {
        timeRemaining -= Time.deltaTime;
        UpdateTimerUI();
    }

    /// <summary>
    /// Orbits every still-active (not yet hit) barrier by the same amount,
    /// as one rigid ring - their relative spacing never changes, so they can
    /// never drift into overlapping each other regardless of how long the
    /// wheel runs. Already-hit barriers stop moving where they were hit, as
    /// a visual "broken" marker of progress. No-op for rarities where
    /// CaptureMinigameConfig.barriersMovePerRarity is false (barrierOrbitSpeed is 0).
    /// </summary>
    private void TickBarrierOrbit()
    {
        if (!barriersMoving || barrierOrbitSpeed <= 0f) return;

        barrierOrbitOffset += barrierOrbitDirection * barrierOrbitSpeed * Time.deltaTime;

        foreach (var area in activeHitAreas)
        {
            if (area == null || area.IsHit) continue;
            area.UpdateAngle(area.BaseStartAngle + barrierOrbitOffset);
        }
    }

    private void HandleHitAttempt()
    {
        if (cooldown) return;
        attemptsRemaining--;
        needleAnimator.SetTrigger("Hit");
        bool hitSomething = false;
        foreach (var area in activeHitAreas)
        {
            if (!area.IsHit && area.ContainsAngle(needleAngle))
            {
                area.MarkHit();
                hitsScored++;
                hitSomething = true;
                break;
            }
        }

        if (hitSomething)
        {
            SoundFXManager.instance.PlaySoundFX(hitSound, transform, 0.5f);
            OnHitScored?.Invoke();
        }
        else
        {
            SoundFXManager.instance.PlaySoundFX(missSound, transform, 0.5f);
            OnMiss?.Invoke();
        }

        if (hitSomething || markOnMissToo)
            SpawnNeedleMark();

        UpdateAttemptsUI();
        StartCoroutine(CoolDown());
    }

    /// <param name="forceEnd">
    /// True when ending because something else took over the player (see
    /// ForceEndMinigame): the player is deliberately left FROZEN for that
    /// caller instead of being released here.
    /// </param>
    private void EndMinigame(bool forceEnd = false)
    {
        IsRunning = false;
        ending = false;
        cooldown = false; // a CoolDown() coroutine cancelled by ForceEndMinigame would otherwise leave this stuck true forever

        float ratio = hitAreaCount > 0 ? (float)hitsScored / hitAreaCount : 0f;

        float stewBonus = ExpeditionStewManager.Instance != null && creatureData != null
            ? ExpeditionStewManager.Instance.GetCaptureChanceBonus(creatureData.family)
            : 0f;

        // Tool's own family bonus (ToolData.bonusFamily/bonusFamilyCaptureChance) -
        // stacks additively with the stew's Capture Power bonus above.
        float toolBonus = 0f;
        if (equippedToolForThisRun != null && creatureData != null && creatureData.family == equippedToolForThisRun.bonusFamily)
            toolBonus = equippedToolForThisRun.bonusFamilyCaptureChance;

        ratio = Mathf.Clamp01(ratio + stewBonus + toolBonus);

        bool success = targetCreature != null && targetCreature.TryCapture(ratio);

        if (popupRoot != null) popupRoot.SetActive(false);

        if (!forceEnd)
        {
            PlayerStateManager.Instance.Unfreeze();
            // Unfreeze() already set toolEquip.CanUse = true, but this minigame
            // specifically wants a brief extra cooldown before the tool can be
            // used again (so closing the wheel doesn't also fire the equipped
            // tool via the same key) - EnableUse() runs its own short delay and
            // overrides CanUse back to true afterward, on top of the generic unfreeze.
            if (toolEquip != null) toolEquip.EnableUse();
        }

        ClearHitAreas();
        ClearNeedleMarks();
        ClearAttemptsUI();
        targetCreature = null;
        creatureData = null;

        if (success)
            SoundFXManager.instance.PlaySoundFX(successSound, transform, 0.5f);
        else
            SoundFXManager.instance.PlaySoundFX(failSound, transform, 0.5f);

        OnMinigameEnded?.Invoke(success);
    }

    private IEnumerator WaitAndEnd(float delay)
    {
        yield return new WaitForSeconds(delay);
        EndMinigame();
    }

    private IEnumerator CoolDown()
    {
        cooldown = true;
        yield return new WaitForSeconds(defaultCooldown);
        cooldown = false;
    }

    private void SetupCenterIcon()
    {
        if (centerIcon == null) return;

        centerIcon.sprite = equippedToolForThisRun != null ? equippedToolForThisRun.icon : defaultCenterIcon;
        centerIcon.enabled = centerIcon.sprite != null;
    }

    /// <summary>
    /// Places a needle-mark instance at the doll's edge, at the current
    /// needle angle - same clockwise-from-top convention as the needle
    /// and hit areas, so it visually lines up with wherever the needle
    /// currently points. Parented under needleTransform.parent (the
    /// wheel's center pivot), NOT under hitAreaParent, since marks should
    /// persist independently of hit-area lifetime.
    /// </summary>
    private void SpawnNeedleMark()
    {
        if (needleHitMark == null || needleTransform == null) return;

        Quaternion markRotation = Quaternion.Euler(0f, 0f, -needleAngle);
        Vector2 localOffset = markRotation * Vector3.up * needleMarkRadius;

        var mark = Instantiate(needleHitMark, needleTransform.parent);
        var markRect = mark.transform as RectTransform;

        if (markRect != null)
        {
            markRect.anchoredPosition = localOffset;
            markRect.localRotation = markRotation;
        }
        else
        {
            // Fallback in case needleHitMark isn't a UI element - shouldn't
            // happen given this whole minigame is Screen Space Overlay UI.
            mark.transform.localPosition = localOffset;
            mark.transform.localRotation = markRotation;
        }

        activeNeedleMarks.Add(mark);
    }

    private void ClearNeedleMarks()
    {
        foreach (var mark in activeNeedleMarks)
            if (mark != null) Destroy(mark);

        activeNeedleMarks.Clear();
    }

    private void SpawnHitAreas()
    {
        ClearHitAreas();

        var usedStarts = new List<float>();
        const int maxAttemptsPerArea = 50;

        for (int i = 0; i < hitAreaCount; i++)
        {
            float start = 0f;
            bool placed = false;

            for (int attempt = 0; attempt < maxAttemptsPerArea; attempt++)
            {
                start = UnityEngine.Random.Range(0f, 360f);
                if (IsFarEnoughFromExisting(start, usedStarts))
                {
                    placed = true;
                    break;
                }
            }

            if (!placed)
            {
                // Random placement kept colliding (can happen with a lot of wide
                // areas) - fall back to even spacing so we never soft-lock.
                start = (360f / hitAreaCount) * i;
            }

            usedStarts.Add(start);

            var areaUI = Instantiate(hitAreaPrefab, hitAreaParent);
            areaUI.SetArc(start, hitAreaWidthDegrees);
            activeHitAreas.Add(areaUI);
        }
    }

    private bool IsFarEnoughFromExisting(float candidateStart, List<float> existingStarts)
    {
        foreach (var existing in existingStarts)
        {
            float distance = Mathf.Abs(Mathf.DeltaAngle(candidateStart, existing));
            if (distance < hitAreaWidthDegrees + minGapBetweenAreasDegrees)
                return false;
        }
        return true;
    }

    private void ClearHitAreas()
    {
        foreach (var area in activeHitAreas)
            if (area != null) Destroy(area.gameObject);

        activeHitAreas.Clear();
    }

    private void UpdateTimerUI()
    {
        if (timerSlider != null) timerSlider.value = Mathf.Clamp01(timeRemaining / Mathf.Max(0.01f, timeLimitForThisRun));
    }

    private void UpdateAttemptsUI()
    {
        if (attemptsRemaining == attemptsAtStart)
        {
            return;
        }
        if (attemptIcons != null && attemptsRemaining < attemptIcons.Length)
            attemptIcons[Mathf.Max(0, attemptsRemaining)]?.SetActive(false);
    }

    /// <summary>
    /// Force-ends the minigame immediately, resolving with whatever hits
    /// were scored so far, rather than leaving the popup open. For cases
    /// like the expedition's overall time running out while a capture is
    /// mid-attempt (see RunSummaryUI.ShowSummary) - the wheel's own timer is
    /// separate from that, so it wouldn't otherwise close on its own. Safe
    /// to call even if no minigame is running.
    ///
    /// Also safe while the normal end is already pending (the short delay
    /// after the last attempt): that pending end is cancelled and resolved
    /// here instead - it would otherwise fire later and unfreeze the player
    /// out from under whatever popup the caller just opened. The player is
    /// left frozen; the caller decides when to release them.
    /// </summary>
    public void ForceEndMinigame()
    {
        if (!IsRunning) return;

        StopAllCoroutines(); // cancel any pending WaitAndEnd/CoolDown
        EndMinigame(forceEnd: true);
    }
}