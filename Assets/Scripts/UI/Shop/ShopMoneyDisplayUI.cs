using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Shows the player's total money in the shop UI. Shows the live total
/// instantly whenever the shop is opened; call AnimateFrom() right after a
/// purchase to count down from the pre-purchase amount instead of snapping.
///
/// Deliberately does NOT subscribe to MoneyManager.OnMoneyChanged for live
/// updates - by the time that event fires, MoneyManager's total has ALREADY
/// changed, so there'd be no "before" value left to animate from. Since
/// nothing else can spend money while the shop is open (player is frozen),
/// every change this display needs to show goes through AnimateFrom() instead.
/// </summary>
public class ShopMoneyDisplayUI : MonoBehaviour
{
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private float countDuration = 0.6f;

    private Coroutine countRoutine;

    private void OnEnable()
    {
        if (countRoutine != null)
        {
            StopCoroutine(countRoutine);
            countRoutine = null;
        }

        SetText(MoneyManager.Instance.GetCurrentMoney());
    }

    /// <summary>Call right after a purchase succeeds - animates from the pre-purchase amount down to the current total.</summary>
    public void AnimateFrom(int previousAmount)
    {
        if (countRoutine != null) StopCoroutine(countRoutine);
        countRoutine = StartCoroutine(CountRoutine(previousAmount, MoneyManager.Instance.GetCurrentMoney()));
    }

    private IEnumerator CountRoutine(int from, int to)
    {
        float t = 0f;
        while (t < countDuration)
        {
            t += Time.deltaTime;
            float progress = countDuration > 0f ? t / countDuration : 1f;
            SetText(Mathf.RoundToInt(Mathf.Lerp(from, to, progress)));
            yield return null;
        }

        SetText(to);
        countRoutine = null;
    }

    private void SetText(int amount)
    {
        if (moneyText != null) moneyText.text = amount.ToString("N0");
    }
}