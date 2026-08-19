using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HealthIndicator : MonoBehaviour
{
    Slider healthSlider;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        healthSlider = GetComponent<Slider>();
        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateHealthUI;
            UpdateHealthUI(playerHealth.currentHealth);
        }
    }

    void UpdateHealthUI(float currentHealth)
    {
        healthSlider.value = currentHealth;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
