using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Drives the capture minigame: spins a needle around a wheel with randomly
/// placed, non-overlapping hit arcs. Player has one attempt per arc and a
/// time limit to hit as many as possible; final capture chance = hits/total.
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

    [Header("UI - Popup")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Image centerIcon;
    [Tooltip("Shown if no tool is currently equipped.")]
    [SerializeField] private Sprite defaultCenterIcon;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text attemptsText;

    [Header("UI - Wheel")]
    [Tooltip("The needle's own RectTransform, pivot at its base (e.g. 0.5, 0), positioned at the wheel's center.")]
    [SerializeField] private RectTransform needleTransform;
    [SerializeField] private Animator needleAnimator;
    [Tooltip("Parent for spawned hit-area arcs, positioned at the wheel's center (pivot 0.5, 0.5).")]
    [SerializeField] private RectTransform hitAreaParent;
    [SerializeField] private CaptureHitAreaUI hitAreaPrefab;

    [Header("Default Settings")]
    [Tooltip("Also equals the number of hit attempts the player gets.")]
    [SerializeField] private int defaultHitAreaCount = 3;
    [SerializeField] private float defaultHitAreaWidthDegrees = 30f;
    [Tooltip("Degrees per second.")]
    [SerializeField] private float defaultNeedleSpeed = 180f;
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

    private int hitAreaCount;
    private float hitAreaWidthDegrees;
    private float needleSpeed;
    private float timeRemaining;
    private int attemptsRemaining;
    private int hitsScored;
    private float needleAngle;

    private bool ending;
    private bool cooldown;

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

    /// <summary>Entry point - call this instead of ICapturable.TryCapture directly.</summary>
    public void BeginCapture(ICapturable creature)
    {
        if (toolEquip != null) toolEquip.CanUse = false;
        if (IsRunning || creature == null || !creature.IsStunned) return;
        if (popupRoot != null) popupRoot.SetActive(true);


        targetCreature = creature;
        creatureData = creature.Data;
        targetCreature?.StartCapture(defaultTimeLimit);

        hitAreaCount = defaultHitAreaCount;
        hitAreaWidthDegrees = defaultHitAreaWidthDegrees;
        needleSpeed = defaultNeedleSpeed;
        timeRemaining = defaultTimeLimit;
        attemptsRemaining = hitAreaCount;
        hitsScored = 0;
        needleAngle = 0f;
        IsRunning = true;

        if (playerMovement != null) playerMovement.enabled = false;
        if (playerCapture != null) playerCapture.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetupCenterIcon();
        SpawnHitAreas();
        UpdateTimerUI();
        UpdateAttemptsUI();


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

        UpdateAttemptsUI();
        StartCoroutine(CoolDown());
    }

    private void EndMinigame()
    {
        IsRunning = false;

        float ratio = hitAreaCount > 0 ? (float)hitsScored / hitAreaCount : 0f;
        bool success = targetCreature != null && targetCreature.TryCapture(ratio);

        if (popupRoot != null) popupRoot.SetActive(false);

        if (playerMovement != null) playerMovement.enabled = true;
        if (playerCapture != null) playerCapture.enabled = true;
        if (toolEquip != null) toolEquip.EnableUse();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        ClearHitAreas();
        targetCreature = null;
        creatureData = null;

        if (success)
            SoundFXManager.instance.PlaySoundFX(successSound, transform, 0.5f);
        else
            SoundFXManager.instance.PlaySoundFX(failSound, transform, 0.5f);

        OnMinigameEnded?.Invoke(success);
        ending = false;
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

        var equippedData = ToolInventoryManager.Instance != null
            ? ToolInventoryManager.Instance.EquippedSlot?.data
            : null;

        centerIcon.sprite = equippedData != null ? equippedData.icon : defaultCenterIcon;
        centerIcon.enabled = centerIcon.sprite != null;
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
        if (timerText != null) timerText.text = Mathf.Max(0f, timeRemaining).ToString("0.0") + "s";
    }

    private void UpdateAttemptsUI()
    {
        if (attemptsText != null) attemptsText.text = $"{Mathf.Max(0, attemptsRemaining)} attempts left";
    }
}