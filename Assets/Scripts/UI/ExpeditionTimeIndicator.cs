using UnityEngine;

/// <summary>
/// Top-right corner clock-dial HUD for expedition time remaining. Replaces
/// the old Slider-based HealthIndicator with a rotating needle.
///
/// Part of the persistent Overlay: UIManager shows this object on
/// expeditions and hides it in the hub, so it subscribes to PlayerHealth in
/// OnEnable/OnDisable (not Start, which only ever runs once). PlayerHealth
/// reports a 0-1 RATIO of time remaining, which is what this displays.
///
/// When time runs out it hands off to RunSummaryUI (which fades back to the
/// hub through SceneTransitionManager once the player is done), or - if no
/// summary exists - goes straight to the hub through SceneTransitionManager.
///
/// Angle convention (same as CaptureHitAreaUI elsewhere in this project):
/// degrees, clockwise, 0 = 12 o'clock/top. The needle sweeps a single 90-degree
/// quadrant - startAngle (180, bottom/6 o'clock) at full time, to endAngle
/// (270, left/9 o'clock) at zero time, matching the "third quadrant" arc.
///
/// Editor setup note: this only reads correctly if the dial's rotation
/// center is anchored at the screen's TOP-RIGHT corner (anchorMin/Max = (1,1))
/// with most of the circle sitting off-screen - see the class's setup
/// instructions for why.
/// </summary>
public class ExpeditionTimeIndicator : MonoBehaviour
{
    [Header("Main Needle (time remaining)")]
    [SerializeField] private RectTransform needleTransform;
    [Tooltip("Clock-face angle where the needle sits at FULL time remaining. 180 = bottom/6 o'clock.")]
    [SerializeField] private float startAngle = 180f;
    [Tooltip("Clock-face angle where the needle sits at ZERO time remaining - reaching this ends the run. 270 = left/9 o'clock.")]
    [SerializeField] private float endAngle = 270f;

    [Header("Inner Ticking Clock (decorative)")]
    [Tooltip("Purely cosmetic - always ticking, unrelated to actual time remaining.")]
    [SerializeField] private RectTransform tickClockTransform;
    [Tooltip("Seconds for the inner clock to complete one full 360-degree rotation.")]
    [SerializeField] private float tickClockPeriod = 60f;

    private float tickTimer;
    private bool depleted;

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        Subscribe(); // OnEnable can run before PlayerHealth exists in the very first scene
    }

    private void OnDisable()
    {
        if (PlayerHealth.Instance != null)
            PlayerHealth.Instance.OnHealthChanged -= OnHealthChanged;
    }

    private void Subscribe()
    {
        var health = PlayerHealth.Instance;
        if (health == null) return;

        // -= first so calling this from both OnEnable and Start never double-subscribes
        health.OnHealthChanged -= OnHealthChanged;
        health.OnHealthChanged += OnHealthChanged;

        // Sync the needle only. Deliberately NOT routed through OnHealthChanged:
        // if this is shown before PlayerHealth has reset for the new scene,
        // currentHealth is still the previous run's ~0 and would end the run
        // instantly. PlayerHealth fires a real update as soon as the run starts.
        float ratio = health.MaxHealth > 0f ? Mathf.Clamp01(health.currentHealth / health.MaxHealth) : 0f;
        depleted = ratio <= 0f;
        UpdateNeedleRotation(ratio);
    }

    private void Update()
    {
        TickInnerClock();
    }

    /// <param name="ratio">Time remaining as 0-1 (PlayerHealth.OnHealthChanged).</param>
    private void OnHealthChanged(float ratio)
    {
        UpdateNeedleRotation(ratio);

        if (ratio > 0f)
        {
            depleted = false;
        }
        else if (!depleted)
        {
            depleted = true;
            EndRun();
        }
    }

    private void EndRun()
    {
        if (RunSummaryUI.Instance != null)
        {
            RunSummaryUI.Instance.ShowSummary();
        }
        else if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionToScene(SceneNames.Hub);
        }
        else
        {
            Debug.LogError("ExpeditionTimeIndicator: time ran out but there is no RunSummaryUI or SceneTransitionManager to end the run.");
        }
    }

    private void UpdateNeedleRotation(float ratio)
    {
        if (needleTransform == null) return;

        // ratio 1 (full time) -> startAngle, ratio 0 (no time) -> endAngle
        float clockAngle = Mathf.Lerp(endAngle, startAngle, Mathf.Clamp01(ratio));

        needleTransform.localRotation = Quaternion.Euler(0f, 0f, clockAngle);
    }

    private void TickInnerClock()
    {
        if (tickClockTransform == null || tickClockPeriod <= 0f) return;

        tickTimer += Time.deltaTime;
        float angle = (tickTimer / tickClockPeriod) * 360f % 360f;
        tickClockTransform.localRotation = Quaternion.Euler(0f, 0f, -angle);
    }
}
