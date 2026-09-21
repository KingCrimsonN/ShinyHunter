using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Legacy slider health display (superseded by ExpeditionTimeIndicator, but
/// still on the Overlay's Life object). Part of the persistent Overlay, so it
/// subscribes in OnEnable/OnDisable rather than Start. PlayerHealth reports a
/// 0-1 ratio, which is what the slider shows.
/// </summary>
public class HealthIndicator : MonoBehaviour
{
    private Slider healthSlider;

    private void Awake()
    {
        healthSlider = GetComponent<Slider>();
    }

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
            PlayerHealth.Instance.OnHealthChanged -= UpdateHealthUI;
    }

    private void Subscribe()
    {
        var health = PlayerHealth.Instance;
        if (health == null) return;

        health.OnHealthChanged -= UpdateHealthUI;
        health.OnHealthChanged += UpdateHealthUI;

        UpdateHealthUI(health.MaxHealth > 0f ? health.currentHealth / health.MaxHealth : 0f);
    }

    private void UpdateHealthUI(float ratio)
    {
        if (healthSlider != null) healthSlider.value = ratio;
    }
}
