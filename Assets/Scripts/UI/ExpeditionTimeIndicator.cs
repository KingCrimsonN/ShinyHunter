using System.Collections;
using UnityEngine;

/// <summary>
/// Top-right corner clock-dial HUD for expedition time remaining. Replaces
/// the old Slider-based HealthIndicator with a rotating needle.
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
    [Header("Data Source")]
    [Tooltip("Should match PlayerHealth's actual max value - this script has no way to read that automatically without seeing PlayerHealth's public API, so keep these in sync manually.")]
    [SerializeField] private float maxTimeValue = 1f;

    [Header("Main Needle (time remaining)")]
    [SerializeField] private RectTransform needleTransform;
    [Tooltip("Clock-face angle where the needle sits at FULL time remaining. 180 = bottom/6 o'clock.")]
    [SerializeField] private float startAngle = 180f;
    [Tooltip("Clock-face angle where the needle sits at ZERO time remaining - reaching this triggers the blackout/teleport. 270 = left/9 o'clock.")]
    [SerializeField] private float endAngle = 270f;

    [Header("Inner Ticking Clock (decorative)")]
    [Tooltip("Purely cosmetic - always ticking, unrelated to actual time remaining.")]
    [SerializeField] private RectTransform tickClockTransform;
    [Tooltip("Seconds for the inner clock to complete one full 360-degree rotation.")]
    [SerializeField] private float tickClockPeriod = 60f;

    [Header("Depletion")]
    [SerializeField] private CanvasGroup blackoutCanvasGroup;
    [SerializeField] private float blackoutFadeDuration = 1f;
    [Tooltip("How long to hold on full black before fading back in, so the teleport itself is never visible.")]
    [SerializeField] private float blackoutHoldDuration = 0.25f;
    [SerializeField] private string hubSpawnPoint;
    [Tooltip("Player's CharacterController - disabled briefly during teleport so setting transform.position directly actually takes effect.")]
    [SerializeField] private CharacterController playerController;


    private PlayerHealth playerHealth;
    private float currentValue;
    private float tickTimer;
    private bool depleted;

    private void Start()
    {
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateTimeUI;
            UpdateTimeUI(playerHealth.currentHealth);
        }

        if (blackoutCanvasGroup != null)
        {
            blackoutCanvasGroup.alpha = 0f;
            blackoutCanvasGroup.blocksRaycasts = false;
            blackoutCanvasGroup.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        TickInnerClock();
    }

    private void UpdateTimeUI(float currentHealth)
    {
        currentValue = currentHealth;
        UpdateNeedleRotation();

        if (currentValue > 0f)
        {
            depleted = false;
        }
        else if (!depleted)
        {
            depleted = true;
            StartCoroutine(BlackoutAndTeleport());
        }
    }

    private void UpdateNeedleRotation()
    {
        if (needleTransform == null) return;

        float ratio = maxTimeValue > 0f ? Mathf.Clamp01(currentValue / maxTimeValue) : 0f;
        // ratio 1 (full time) -> startAngle, ratio 0 (no time) -> endAngle
        float clockAngle = Mathf.Lerp(endAngle, startAngle, ratio);

        needleTransform.localRotation = Quaternion.Euler(0f, 0f, clockAngle);
    }

    private void TickInnerClock()
    {
        if (tickClockTransform == null || tickClockPeriod <= 0f) return;

        tickTimer += Time.deltaTime;
        float angle = (tickTimer / tickClockPeriod) * 360f % 360f;
        tickClockTransform.localRotation = Quaternion.Euler(0f, 0f, -angle);
    }

    private IEnumerator BlackoutAndTeleport()
    {
        yield return FadeBlackout(0f, 1f, blackoutFadeDuration);

        TeleportPlayerToHub();

        yield return new WaitForSeconds(blackoutHoldDuration);
        yield return FadeBlackout(1f, 0f, blackoutFadeDuration);
    }

    private IEnumerator FadeBlackout(float from, float to, float duration)
    {
        if (blackoutCanvasGroup == null) yield break;

        blackoutCanvasGroup.gameObject.SetActive(true);
        blackoutCanvasGroup.blocksRaycasts = true;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            blackoutCanvasGroup.alpha = Mathf.Lerp(from, to, duration > 0f ? t / duration : 1f);
            yield return null;
        }

        blackoutCanvasGroup.alpha = to;

        if (to <= 0f)
        {
            blackoutCanvasGroup.blocksRaycasts = false;
            blackoutCanvasGroup.gameObject.SetActive(false);
        }
    }

    private void TeleportPlayerToHub()
    {
        if (hubSpawnPoint == null) return;

        UnityEngine.SceneManagement.SceneManager.LoadScene(hubSpawnPoint);

        // Swap this block for a SceneManager.LoadScene(...) call instead if
        // the hub is actually a separate Scene rather than a position in this one.
        if (playerController != null)
        {
            // playerController.enabled = false; // CharacterController fights direct position sets unless disabled first
            // playerController.transform.SetPositionAndRotation(hubSpawnPoint.position, hubSpawnPoint.rotation);
            // playerController.enabled = true;
        }

        // TODO: once PlayerHealth exposes a way to replenish/reset the time
        // value for the next expedition, call it here so currentValue (and
        // therefore the needle) resets too - otherwise the needle stays
        // pinned at the depleted end position after teleporting back.
    }
}