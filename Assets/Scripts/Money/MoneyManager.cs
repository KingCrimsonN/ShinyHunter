using System;
using UnityEngine;

public class MoneyManager : MonoBehaviour
{
    [SerializeField] private int startingMoney = 0;
    private int currentMoney;

    public static MoneyManager Instance { get; private set; }

    public event Action OnMoneyChanged;

    [SerializeField] private AudioClip moneySound;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        currentMoney = startingMoney;
    }

    public int GetCurrentMoney()
    {
        return currentMoney;
    }

    public void AddMoney(int amount)
    {
        if (amount <= 0) return;
        currentMoney += amount;
        OnMoneyChanged?.Invoke();
        PlayMoneySound();
    }

    public bool TrySpendMoney(int amount)
    {
        if (currentMoney >= amount)
        {
            currentMoney -= amount;
            OnMoneyChanged?.Invoke();
            PlayMoneySound();
            return true;
        }
        return false;
    }

    public void PlayMoneySound()
    {
        if (moneySound != null)
        {
            SoundFXManager.instance.PlaySoundFX(moneySound, transform, 1f);
        }
    }
}
