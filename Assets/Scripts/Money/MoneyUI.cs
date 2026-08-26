using UnityEngine;
using TMPro;

public class MoneyUI : MonoBehaviour
{

    private TMP_Text moneyText;


    void Start()
    {
        moneyText = GetComponent<TMP_Text>();
        MoneyManager.Instance.OnMoneyChanged += UpdateMoneyText;
        UpdateMoneyText();
    }

    private void UpdateMoneyText()
    {
        moneyText.text = MoneyManager.Instance.GetCurrentMoney().ToString();
    }

}
