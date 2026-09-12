using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Drives the expedition "life" (time remaining) value that
/// ExpeditionTimeIndicator displays. Persistent (DontDestroyOnLoad) like the
/// other core managers - it needs to be, since a potion drunk in the hub
/// should be able to affect the NEXT expedition's max time.
///
/// Inert while in the Hub scene (no ticking, TakeDamage is a no-op there) -
/// via an internal flag rather than disabling the component, since a
/// persistent object's OnDisable would sever the very scene-load
/// subscription needed to detect leaving the hub again. Re-evaluated on
/// every scene load rather than once in Start(), since Start() only ever
/// runs once for a persistent object.
///
/// Potion-system hooks: AddTemporaryMaxHealth (cleared automatically on
/// return to the hub - "for one run" effects) and AddPermanentMaxHealth
/// (persists forever - upgrades). MaxHealth is always their sum.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance { get; private set; }

    [Tooltip("Exact name of the hub scene - PlayerHealth stops ticking there and clears temporary modifiers.")]
    [SerializeField] private string hubSceneName = "Hub";

    [SerializeField] public float poisonRate = 1f;
    [Tooltip("Permanent base value - change this via AddPermanentMaxHealth for upgrades, not directly.")]
    [SerializeField] private float baseMaxHealth = 300f;
    [SerializeField] public float currentHealth;

    /// <summary>Additive bonus/penalty for the CURRENT run only - cleared when returning to the hub. "For one run" potions use this.</summary>
    private float temporaryMaxHealthModifier;

    /// <summary>Permanent base + this run's temporary modifier.</summary>
    public float MaxHealth => baseMaxHealth + temporaryMaxHealthModifier;

    public Action<float> OnHealthChanged;

    private bool isRunning;

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

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        HandleSceneChange(SceneManager.GetActiveScene().name);
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        HandleSceneChange(scene.name);
    }

    private void HandleSceneChange(string sceneName)
    {
        if (sceneName == hubSceneName)
        {
            isRunning = false;
            temporaryMaxHealthModifier = 0f; // "for one run" effects are consumed by the run that just ended
        }
        else
        {
            isRunning = true;
            currentHealth = MaxHealth; // fresh full time for the new expedition
            OnHealthChanged?.Invoke(currentHealth / MaxHealth);
        }
    }

    private void Update()
    {
        if (!isRunning || currentHealth <= 0f) return;

        currentHealth -= poisonRate * Time.deltaTime;
        OnHealthChanged?.Invoke(currentHealth / MaxHealth);

        if (currentHealth <= 0f)
            Die();
    }

    public void TakeDamage(float damage)
    {
        if (!isRunning) return;

        currentHealth -= damage;
        OnHealthChanged?.Invoke(currentHealth / MaxHealth);

        if (currentHealth <= 0f)
            Die();
    }

    public void Die()
    {
        // Handle player death logic here
        Debug.Log("Player has died.");
    }

    // ---------------- Potion system hooks ----------------

    /// <summary>"For one run" potion effect - added on top of the permanent max, cleared automatically on return to the hub.</summary>
    public void AddTemporaryMaxHealth(float amount)
    {
        temporaryMaxHealthModifier += amount;
    }

    /// <summary>Permanent upgrade - persists across every future run.</summary>
    public void AddPermanentMaxHealth(float amount)
    {
        baseMaxHealth += amount;
    }

    /// <summary>Refills currentHealth to the current MaxHealth (permanent + this run's temporary bonus) - e.g. an instant-refill potion drunk mid-run.</summary>
    public void RefillToMax()
    {
        currentHealth = MaxHealth;
        OnHealthChanged?.Invoke(currentHealth / MaxHealth);
    }
}