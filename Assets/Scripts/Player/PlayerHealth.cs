using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float poisonRate = 1f;
    [SerializeField] private float maxHealth = 300f;
    [SerializeField] public float currentHealth;

    public Action<float> OnHealthChanged;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        // Handle player death logic here
        Debug.Log("Player has died.");
    }

    // Update is called once per frame
    void Update()
    {
        // Example of poison effect
        if (currentHealth > 0)
        {
            currentHealth -= poisonRate * Time.deltaTime;
            OnHealthChanged?.Invoke(currentHealth / maxHealth);
            if (currentHealth <= 0)
            {
                Die();
            }
        }
    }
}
