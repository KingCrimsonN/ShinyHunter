using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DG.Tweening;

/// <summary>
/// Drives the capture minigame: spins a needle around a wheel with randomly
/// placed hit arcs ("barriers"). Player gets a number of attempts and a time
/// limit to hit as many as possible; final capture chance = hits/total
/// (plus Capture Power's meal bonus and the tool's own family bonus, if
/// either applies - see EndMinigame).
///
/// Every run is configured from three independent sources (see BeginCapture
/// and decision log):
///   - The EQUIPPED TOOL (ToolData): time limit, attempt count, barrier
///     orbit-speed multiplier, extra-ingredient chance, family capture bonus.
///   - The CREATURE'S RARITY (CaptureMinigameConfig): barrier count/width,
///     and whether/how fast barriers orbit the wheel (Shiny/Radiant only).
///   - The ACTIVE MEAL (ExpeditionMealManager): Capture Power's time bonus
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
    [Tooltip("Base degrees per second the NEEDLE spins. The equipped tool scales it (ToolData.needleSpeedMultiplier); the creature's rarity does not.")]
    [SerializeField] private float defaultNeedleSpeed = 180f;
    [Tooltip("Fallback time limit, used only if the equipped tool is unassigned.")]
    [SerializeField] private float defaultTimeLimit = 5f;
    [Tooltip("Minimum angular gap enforced between adjacent arcs, on top of their width, so they never touch or overlap.")]
    [SerializeField] private float minGapBetweenAreasDegrees = 10f;
    [SerializeField] private KeyCode hitKey = KeyCode.Mouse0;

    [Header("Capture Power head-start")]
    [Tooltip("Seconds the wheel sits still after opening before Capture Power breaks its first barrier - so you see it happen. The needle, timer and input wait for the head-start to finish.")]
    [SerializeField, Min(0f)] private float autoBreakDelay = 0.6f;
    [Tooltip("Seconds between barriers when it breaks more than one, and after the last before play starts.")]
    [SerializeField, Min(0f)] private float autoBreakInterval = 0.4f;
    [Tooltip("Optional full-screen vignette (e.g. an Image with a dark-edged sprite, Raycast Target OFF) faded in for the head-start only. Leave it inactive or at alpha 0 in the editor - it is reset either way.")]
    [SerializeField] private Graphic breakVignette;
    [Tooltip("Vignette opacity while the head-start plays.")]
    [SerializeField, Range(0f, 1f)] private float vignetteAlpha = 1f;
    [Tooltip("Seconds to fade the vignette in/out.")]
    [SerializeField, Min(0f)] private float vignetteFade = 0.2f;
    [Tooltip("Seconds the nails (attempt icons) take to pop in once the head-start is over.")]
    [SerializeField, Min(0f)] private float nailsAppearDuration = 0.2f;
    [SerializeField] private float defaultCooldown = 0.3f;

    [Header("Break particles")]
    [Tooltip("Spawned at a barrier's middle each time one breaks (player hit or Capture Power), under Hit Area Parent. Every ParticleSystem in it is played; the instance is removed after FX Lifetime or when the wheel closes.")]
    [SerializeField] private GameObject breakParticlesPrefab;
    [Tooltip("Distance from the wheel centre to spawn it - match the barrier ring's radius (canvas units, like Needle Mark Radius).")]
    [SerializeField] private float breakParticlesRadius = 100f;
    [Tooltip("Rotate it so its local up points outward from the wheel at the barrier (for directional bursts). Off = prefab's own rotation.")]
    [SerializeField] private bool alignBreakParticles = true;
    [Tooltip("Seconds before a spawned particle instance (break or success) is destroyed.")]
    [SerializeField, Min(0.1f)] private float fxLifetime = 3f;

    [Header("Result reveal (after the last attempt)")]
    [Tooltip("What shrinks away with the nails at the end - defaults to the Timer Slider's object.")]
    [SerializeField] private Transform timerRoot;
    [Tooltip("Seconds the nails and timer take to shrink away.")]
    [SerializeField, Min(0f)] private float resultHideDuration = 0.2f;
    [Tooltip("The doll (Center Icon) first shrinks to this scale...")]
    [SerializeField, Min(0f)] private float dollShrinkScale = 0.75f;
    [SerializeField, Min(0f)] private float dollShrinkDuration = 0.35f;
    [Tooltip("...then bounces up to this scale before the result.")]
    [SerializeField, Min(0f)] private float dollBounceScale = 1.2f;
    [SerializeField, Min(0f)] private float dollBounceDuration = 0.3f;
    [Tooltip("Played at the doll when the capture succeeds (same handling as the break particles).")]
    [SerializeField] private GameObject successParticlesPrefab;
    [Tooltip("Seconds the success stays on screen before the wheel closes.")]
    [SerializeField, Min(0f)] private float successHold = 0.9f;
    [Tooltip("Seconds the doll takes to vanish on a failed capture.")]
    [SerializeField, Min(0f)] private float failVanishDuration = 0.25f;
    [Tooltip("Seconds after vanishing before the wheel closes.")]
    [SerializeField, Min(0f)] private float failHold = 0.35f;

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
    /// <summary>Capture Power is breaking barriers (AutoBreakRoutine) - the wheel is on hold.</summary>
    private bool autoBreaking;
    /// <summary>The result reveal is playing (ResultRoutine) - no input, the wheel is frozen.</summary>
    private bool revealing;

    // Result is rolled ONCE when the wheel ends (RollResult) and applied to the creature when it closes (CloseMinigame).
    private bool resultRolled;
    private bool rolledSuccess;
    private bool resultSoundPlayed;

    private readonly List<GameObject> activeFx = new List<GameObject>();
    private Vector3 dollBaseScale = Vector3.one;
    private Color dollBaseColor = Color.white;
    private Vector3 needleBaseScale = Vector3.one;

    // ---------------- Per-run config, resolved once in BeginCapture from the equipped tool + creature rarity + active meal ----------------

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
        HideVignetteNow();
        if (timerRoot == null && timerSlider != null) timerRoot = timerSlider.transform;
        if (centerIcon != null)
        {
            dollBaseScale = centerIcon.rectTransform.localScale;
            dollBaseColor = centerIcon.color;
        }
        if (needleTransform != null) needleBaseScale = needleTransform.localScale;
    }

    private void Update()
    {
        if (!IsRunning || autoBreaking || revealing) return;

        TickNeedle();
        TickBarrierOrbit();
        TickTimer();

        if (Input.GetKeyDown(hitKey))
            HandleHitAttempt();

        if (timeRemaining <= 0f || attemptsRemaining <= 0 || hitsScored >= hitAreaCount)
            if (!ending)
            {
                ending = true;
                StartCoroutine(EndSequence(0.5f)); // short delay so player sees the last hit/miss feedback, then the result reveal
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

    /// <summary>Instant "blink" distance for a post-hit dash, for a creature of the given rarity - see CaptureMinigameConfig.dashBlinkDistancePerRarity. Falls back to 0 (no blink) if no config is assigned.</summary>
    public float GetDashBlinkDistance(CreatureData.Rarity rarity)
    {
        return config != null ? config.GetDashBlinkDistance(rarity) : 0f;
    }

    /// <summary>Flee-speed multiplier for a post-hit "dash", for a creature of the given rarity - see CaptureMinigameConfig.dashSpeedMultiplierPerRarity. Falls back to 1 (no dash) if no config is assigned.</summary>
    public float GetDashSpeedMultiplier(CreatureData.Rarity rarity)
    {
        return config != null ? config.GetDashSpeedMultiplier(rarity) : 1f;
    }

    /// <summary>How long a post-hit dash lasts, for a creature of the given rarity - see CaptureMinigameConfig.dashDurationPerRarity. Falls back to 0 (no dash) if no config is assigned.</summary>
    public float GetDashDuration(CreatureData.Rarity rarity)
    {
        return config != null ? config.GetDashDuration(rarity) : 0f;
    }

    /// <summary>
    /// Entry point - call this instead of ICapturable.TryCapture directly.
    /// Configures the whole run from three sources (see class docs and
    /// decision log): the equipped tool (time limit, attempt count, barrier
    /// speed multiplier, family capture bonus), the creature's rarity
    /// (barrier count/size, whether/how fast they move - CaptureMinigameConfig),
    /// and the active meal (Capture Power's time bonus, family-scoped).
    /// </summary>
    /// <returns>True if the minigame started. False if it didn't (already running, or the creature is gone / no longer stunned) - callers must not consume anything for a capture that never began.</returns>
    public bool BeginCapture(ICapturable creature)
    {
        if (IsRunning || creature == null || !creature.IsStunned) return false;
        // The run is over (time ran out while the doll was in the air, etc.) - no wheel over the summary.
        if (PlayerHealth.Instance != null && !PlayerHealth.Instance.IsRunning) return false;
        if (popupRoot != null) popupRoot.SetActive(true);


        targetCreature = creature;
        creatureData = creature.Data;
        // Keeps the creature stunned until EndMinigame resolves the attempt -
        // the wheel's own timer plus the end-of-minigame delay can outlast the
        // creature's normal stun, which used to discard the result.
        targetCreature.StartCapture();

        ending = false;
        cooldown = false;
        revealing = false;
        resultRolled = false;
        rolledSuccess = false;
        resultSoundPlayed = false;
        ResetRevealVisuals(); // defensive - the previous close already did this

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

        needleSpeed = defaultNeedleSpeed * (equippedToolForThisRun != null ? equippedToolForThisRun.needleSpeedMultiplier : 1f);

        float toolTimeLimit = equippedToolForThisRun != null ? equippedToolForThisRun.minigameTimeLimit : defaultTimeLimit;
        float captureTimeBonus = ExpeditionMealManager.Instance != null && creatureData != null
            ? ExpeditionMealManager.Instance.GetCaptureTimeBonus(creatureData.family)
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

    /// <summary>
    /// Capture Power head-start: breaks some barriers without spending attempts.
    /// Only if this creature's family matches the meal's affected family. Staged
    /// (AutoBreakRoutine) rather than instant, so the player sees it happen.
    /// </summary>
    private void ApplyCapturePowerAutoBreak()
    {
        if (ExpeditionMealManager.Instance == null || creatureData == null) return;

        int autoBreakCount = Mathf.Min(ExpeditionMealManager.Instance.GetCaptureAutoBreakCount(creatureData.family), activeHitAreas.Count);
        if (autoBreakCount > 0) StartCoroutine(AutoBreakRoutine(autoBreakCount));
    }

    /// <summary>Wheel on hold: a beat, then each barrier breaks with its focus effect, one by one, then play starts.</summary>
    private IEnumerator AutoBreakRoutine(int count)
    {
        autoBreaking = true;

        // Nails stay hidden until play starts; the vignette darkens the edges so the eye goes to the wheel.
        if (attemptsParent != null) attemptsParent.SetActive(false);
        if (breakVignette != null)
        {
            breakVignette.DOKill();
            breakVignette.gameObject.SetActive(true);
            breakVignette.color = WithAlpha(breakVignette.color, 0f);
            breakVignette.DOFade(vignetteAlpha, vignetteFade).SetLink(breakVignette.gameObject);
        }

        yield return new WaitForSeconds(autoBreakDelay);

        for (int i = 0; i < count; i++)
        {
            activeHitAreas[i].MarkHit();
            activeHitAreas[i].PlayBreakFocus();
            SpawnBreakParticles(activeHitAreas[i]);
            hitsScored++;
            if (hitSound != null) SoundFXManager.instance.PlaySoundFX(hitSound, transform, 0.5f);
            yield return new WaitForSeconds(autoBreakInterval);
        }

        if (breakVignette != null)
            breakVignette.DOFade(0f, vignetteFade).SetLink(breakVignette.gameObject)
                .OnComplete(() => breakVignette.gameObject.SetActive(false));
        ShowNails();

        autoBreaking = false;
    }

    /// <summary>The attempt nails come back (popping in) once the head-start is done.</summary>
    private void ShowNails()
    {
        if (attemptsParent == null || attemptsParent.activeSelf) return;

        attemptsParent.SetActive(true);
        var t = attemptsParent.transform;
        t.DOKill();
        t.localScale = Vector3.zero;
        t.DOScale(1f, nailsAppearDuration).SetEase(Ease.OutBack).SetLink(attemptsParent);
    }

    private void HideVignetteNow()
    {
        if (breakVignette == null) return;
        breakVignette.DOKill();
        breakVignette.color = WithAlpha(breakVignette.color, 0f);
        breakVignette.gameObject.SetActive(false);
    }

    private static Color WithAlpha(Color c, float a) { c.a = a; return c; }

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
                SpawnBreakParticles(area);
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

    /// <summary>
    /// Rolls the capture ONCE, from the hits plus the meal's and the tool's
    /// bonuses. Nothing is applied to the creature yet - CloseMinigame does that
    /// after the reveal, so the world capture effects and the capture popup
    /// still happen as the wheel closes, not hidden behind it.
    /// </summary>
    private bool RollResult()
    {
        if (resultRolled) return rolledSuccess;
        resultRolled = true;

        float ratio = hitAreaCount > 0 ? (float)hitsScored / hitAreaCount : 0f;

        float mealBonus = ExpeditionMealManager.Instance != null && creatureData != null
            ? ExpeditionMealManager.Instance.GetCaptureChanceBonus(creatureData.family)
            : 0f;

        // Tool's own family bonus (ToolData.bonusFamily/bonusFamilyCaptureChance) -
        // stacks additively with the meal's Capture Power bonus above.
        float toolBonus = 0f;
        if (equippedToolForThisRun != null && creatureData != null && creatureData.family == equippedToolForThisRun.bonusFamily)
            toolBonus = equippedToolForThisRun.bonusFamilyCaptureChance;

        ratio = Mathf.Clamp01(ratio + mealBonus + toolBonus);
        rolledSuccess = targetCreature != null && UnityEngine.Random.value <= ratio;
        return rolledSuccess;
    }

    /// <summary>
    /// The end of a normal run: a beat so the last hit/miss reads, then the
    /// reveal - nails and timer shrink away, the doll shrinks, bounces up, and
    /// either bursts (success particles) or vanishes - then the wheel closes.
    /// </summary>
    private IEnumerator EndSequence(float delay)
    {
        yield return new WaitForSeconds(delay);

        revealing = true;
        bool success = RollResult();

        HideForReveal(attemptsParent != null ? attemptsParent.transform : null);
        HideForReveal(timerRoot);
        // The needle and the marks it left on the doll go too. The needle's Animator is paused
        // so its clips can't write the scale back over the tween.
        if (needleAnimator != null) needleAnimator.enabled = false;
        HideForReveal(needleTransform);
        foreach (var mark in activeNeedleMarks)
            if (mark != null) HideForReveal(mark.transform);
        if (resultHideDuration > 0f) yield return new WaitForSeconds(resultHideDuration);

        RectTransform doll = centerIcon != null && centerIcon.enabled ? centerIcon.rectTransform : null;
        if (doll != null)
        {
            doll.DOKill();
            var anticipation = DOTween.Sequence()
                .Append(doll.DOScale(dollBaseScale * dollShrinkScale, dollShrinkDuration).SetEase(Ease.OutQuad))
                .Append(doll.DOScale(dollBaseScale * dollBounceScale, dollBounceDuration).SetEase(Ease.OutBack))
                .SetLink(doll.gameObject);
            yield return anticipation.WaitForCompletion();
        }

        PlayResultSound(success);
        if (success)
        {
            if (doll != null)
            {
                SpawnFx(successParticlesPrefab, doll.parent, doll.localPosition, Quaternion.identity);
                doll.DOPunchScale(dollBaseScale * 0.1f, 0.3f, 6, 0.5f).SetLink(doll.gameObject);
            }
            else SpawnFx(successParticlesPrefab, hitAreaParent, Vector3.zero, Quaternion.identity);
            if (successHold > 0f) yield return new WaitForSeconds(successHold);
        }
        else
        {
            if (doll != null)
            {
                doll.DOScale(0f, failVanishDuration).SetEase(Ease.InBack).SetLink(doll.gameObject);
                centerIcon.DOFade(0f, failVanishDuration).SetLink(doll.gameObject);
            }
            float wait = failVanishDuration + failHold;
            if (wait > 0f) yield return new WaitForSeconds(wait);
        }

        CloseMinigame();
    }

    /// <summary>
    /// Applies the rolled result to the creature and closes the wheel.
    /// </summary>
    /// <param name="forceEnd">
    /// True when ending because something else took over the player (see
    /// ForceEndMinigame): the player is deliberately left FROZEN for that
    /// caller instead of being released here.
    /// </param>
    private void CloseMinigame(bool forceEnd = false)
    {
        bool rolled = RollResult(); // already rolled unless force-ended before the reveal

        IsRunning = false;
        ending = false;
        autoBreaking = false; // a ForceEndMinigame mid head-start stops the routine
        revealing = false;
        HideVignetteNow();
        ResetRevealVisuals();
        cooldown = false; // a CoolDown() coroutine cancelled by ForceEndMinigame would otherwise leave this stuck true forever

        // The tool is remembered from when the wheel STARTED (equippedToolForThisRun) -
        // the doll was consumed the moment the throw landed, so for the last
        // one in a stack the live inventory no longer has it.
        float extraYieldChance = equippedToolForThisRun != null ? equippedToolForThisRun.extraIngredientChance : 0f;

        bool success = targetCreature != null && targetCreature.ResolveCapture(rolled, extraYieldChance);

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

        if (!resultSoundPlayed) PlayResultSound(success);

        OnMinigameEnded?.Invoke(success);
    }

    private void PlayResultSound(bool success)
    {
        resultSoundPlayed = true;
        var clip = success ? successSound : failSound;
        if (clip != null && SoundFXManager.instance != null)
            SoundFXManager.instance.PlaySoundFX(clip, transform, 0.5f);
    }

    private void HideForReveal(Transform t)
    {
        if (t == null || !t.gameObject.activeInHierarchy) return;
        t.DOKill();
        t.DOScale(0f, resultHideDuration).SetEase(Ease.InBack).SetLink(t.gameObject);
    }

    /// <summary>Puts everything the head-start and the reveal touched back to its editor state.</summary>
    private void ResetRevealVisuals()
    {
        if (attemptsParent != null)
        {
            attemptsParent.transform.DOKill();
            attemptsParent.transform.localScale = Vector3.one;
            attemptsParent.SetActive(true);
        }
        if (timerRoot != null)
        {
            timerRoot.DOKill();
            timerRoot.localScale = Vector3.one;
        }
        if (needleTransform != null)
        {
            needleTransform.DOKill();
            needleTransform.localScale = needleBaseScale;
        }
        if (needleAnimator != null) needleAnimator.enabled = true;
        // (needle marks are destroyed on close - ClearNeedleMarks - so they need no reset)
        if (centerIcon != null)
        {
            centerIcon.rectTransform.DOKill();
            centerIcon.DOKill();
            centerIcon.rectTransform.localScale = dollBaseScale;
            centerIcon.color = dollBaseColor;
        }
        ClearFx();
    }

    /// <summary>Break particles at the barrier's middle, on the barrier ring.</summary>
    private void SpawnBreakParticles(CaptureHitAreaUI area)
    {
        if (breakParticlesPrefab == null || area == null || hitAreaParent == null) return;

        Quaternion rotation = Quaternion.Euler(0f, 0f, -area.MidAngle); // clockwise-from-top -> Unity Z
        Vector3 position = rotation * Vector3.up * breakParticlesRadius;
        SpawnFx(breakParticlesPrefab, hitAreaParent, position, alignBreakParticles ? rotation : Quaternion.identity);
    }

    private void SpawnFx(GameObject prefab, Transform parent, Vector3 localPosition, Quaternion localRotation)
    {
        if (prefab == null || parent == null) return;

        var fx = Instantiate(prefab, parent, false);
        var t = fx.transform;
        t.localPosition = new Vector3(localPosition.x, localPosition.y, prefab.transform.localPosition.z);
        t.localRotation = localRotation * prefab.transform.localRotation;
        t.SetAsLastSibling(); // draws over the wheel (UI particle renderers sort by sibling order)

        foreach (var ps in fx.GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.Clear(false);
            ps.Play(false);
        }

        activeFx.Add(fx);
        Destroy(fx, fxLifetime);
    }

    private void ClearFx()
    {
        foreach (var fx in activeFx)
            if (fx != null) Destroy(fx);
        activeFx.Clear();
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

        StopAllCoroutines(); // cancel any pending EndSequence/CoolDown - mid-reveal it resolves with the result already rolled
        CloseMinigame(forceEnd: true);
    }
}