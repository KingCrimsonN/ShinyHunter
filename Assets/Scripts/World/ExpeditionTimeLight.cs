using UnityEngine;

/// <summary>
/// A light "connected" to the expedition clock: during the last
/// <see cref="dimSeconds"/> of the run it dims and shifts colour, reaching
/// its end look exactly when time runs out. Above that it is left at whatever
/// it was set to in the editor. Put it on any Light in an expedition scene
/// (sun, lamps, lanterns...); every one reacts independently with its own settings.
///
/// Reads PlayerHealth.currentHealth (seconds left) every frame - no
/// subscription, so it doesn't matter which of the two wakes first. Time
/// added mid-run (Chrono Power) brightens it back up. Scene-scoped, holds
/// nothing persistent. In the hub (no run) it stays at its editor values.
/// </summary>
[RequireComponent(typeof(Light))]
public class ExpeditionTimeLight : MonoBehaviour
{
    [Tooltip("It starts dimming when this many seconds of expedition time are left.")]
    [SerializeField, Min(0.01f)] private float dimSeconds = 30f;

    [Header("End look (when time runs out)")]
    [Tooltip("Intensity at zero time, as a fraction of the editor intensity (0 = completely off).")]
    [SerializeField, Range(0f, 1f)] private float endIntensity = 0.15f;
    [Tooltip("Also shift the colour toward End Color.")]
    [SerializeField] private bool changeColor = true;
    [SerializeField] private Color endColor = new Color(0.55f, 0.25f, 0.3f);

    [Header("Shape")]
    [Tooltip("How the change progresses over the dim window: X 0 = dimming starts, X 1 = time ran out; Y 0 = editor look, Y 1 = end look.")]
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Ambient (optional - set it on ONE light per scene, e.g. the sun)")]
    [Tooltip("Also dim the scene's ambient light (RenderSettings.ambientIntensity and ambientLight) by the same amount.")]
    [SerializeField] private bool alsoDimAmbient = false;

    private Light lightSource;
    private float baseIntensity;
    private Color baseColor;
    private float baseAmbientIntensity;
    private Color baseAmbientLight;
    private float lastProgress = -1f;

    private void Awake()
    {
        lightSource = GetComponent<Light>();
        baseIntensity = lightSource.intensity;
        baseColor = lightSource.color;
        baseAmbientIntensity = RenderSettings.ambientIntensity;
        baseAmbientLight = RenderSettings.ambientLight;
    }

    private void OnDisable()
    {
        Apply(0f); // back to the editor look
        lastProgress = -1f;
    }

    private void Update()
    {
        var health = PlayerHealth.Instance;
        float progress = 0f;

        // Only while a run is going - and once it has ended (time out) it stays at the end look.
        if (health != null && (health.IsRunning || health.currentHealth <= 0f) && !SceneNames.IsHub(gameObject.scene.name))
        {
            float left = Mathf.Max(0f, health.currentHealth);
            progress = left >= dimSeconds ? 0f : 1f - left / dimSeconds;
        }

        if (Mathf.Approximately(progress, lastProgress)) return;
        lastProgress = progress;
        Apply(progress);
    }

    /// <param name="progress">0 = editor look, 1 = end look (before the curve).</param>
    private void Apply(float progress)
    {
        if (lightSource == null) return;

        float t = progress <= 0f ? 0f : Mathf.Clamp01(curve.Evaluate(progress));
        float intensityFactor = Mathf.Lerp(1f, endIntensity, t);

        lightSource.intensity = baseIntensity * intensityFactor;
        if (changeColor) lightSource.color = Color.Lerp(baseColor, endColor, t);

        if (alsoDimAmbient)
        {
            RenderSettings.ambientIntensity = baseAmbientIntensity * intensityFactor;
            RenderSettings.ambientLight = changeColor
                ? Color.Lerp(baseAmbientLight, baseAmbientLight * endColor, t) * intensityFactor
                : baseAmbientLight * intensityFactor;
        }
    }
}
