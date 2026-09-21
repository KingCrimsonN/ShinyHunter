using UnityEngine;
using TMPro;

/// <summary>
/// Shows the player's money. Part of the persistent Overlay, so it subscribes
/// in OnEnable/OnDisable (a duplicate Overlay's copy is destroyed and must
/// unsubscribe) and refreshes whenever it's shown.
/// </summary>
public class MoneyUI : MonoBehaviour
{
    private TMP_Text moneyText;

    private void Awake()
    {
        moneyText = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        Subscribe(); // OnEnable can run before MoneyManager exists in the very first scene
    }

    private void OnDisable()
    {
        if (MoneyManager.Instance != null)
            MoneyManager.Instance.OnMoneyChanged -= UpdateMoneyText;
    }

    private void Subscribe()
    {
        if (MoneyManager.Instance == null) return;

        MoneyManager.Instance.OnMoneyChanged -= UpdateMoneyText;
        MoneyManager.Instance.OnMoneyChanged += UpdateMoneyText;
        UpdateMoneyText();
    }

    private void UpdateMoneyText()
    {
        if (moneyText != null)
            moneyText.text = MoneyManager.Instance.GetCurrentMoney().ToString();
    }
}
