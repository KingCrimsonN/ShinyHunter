using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// End-of-run summary popup: page 1 (grid of creatures caught this run,
/// rarest first), page 2 (stats + final reward), then a money count-up
/// animation before handing off to ExpeditionTimeIndicator's existing
/// blackout+teleport.
///
/// ASSUMPTION: expects a MoneyManager singleton exposing `int CurrentMoney`
/// and `void AddMoney(int amount)`. Every call into it is isolated to the
/// two one-line methods at the bottom of this file - adjust those if your
/// actual API differs, I don't have visibility into that script.
/// </summary>
public class RunSummaryUI : MonoBehaviour
{
    public static RunSummaryUI Instance { get; private set; }

    [Header("Popup")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private GameObject page1Grid;
    [SerializeField] private GameObject page2Stats;
    [SerializeField] private Button nextButton;
    [Tooltip("Total money owned, shown top-right on both pages.")]
    [SerializeField] private TMP_Text totalMoneyText;

    [Header("Sub-panels")]
    [SerializeField] private RunCreatureGridUI creatureGrid;
    [SerializeField] private RunStatsUI statsUI;

    [Header("Reward Formula (tune these to taste)")]
    [SerializeField] private float rewardPerCritter = 5f;
    [SerializeField] private float rewardPerNewSpecies = 20f;
    [Tooltip("Extra bonus per critter of each rarity, ON TOP of rewardPerCritter. Index 0=Normal..3=Legendary.")]
    [SerializeField] private float[] rewardPerRarityCritter = { 1f, 3f, 8f, 20f };
    [SerializeField] private int milestoneThreshold = 30;
    [SerializeField] private float milestoneBonus = 50f;

    [Header("Money Animation")]
    [SerializeField] private float moneyCountUpDuration = 1.5f;
    [SerializeField] private float holdAfterCountUp = 0.3f;

    [SerializeField] private ExpeditionTimeIndicator expeditionTimeIndicator;

    private int currentPage;
    private RunStats cachedStats;

    private void Awake()
    {
        Instance = this;
        if (popupRoot != null) popupRoot.SetActive(false);
        expeditionTimeIndicator = FindFirstObjectByType<ExpeditionTimeIndicator>();
    }

    private void OnEnable()
    {
        if (nextButton != null) nextButton.onClick.AddListener(OnNextPressed);
    }

    private void OnDisable()
    {
        if (nextButton != null) nextButton.onClick.RemoveListener(OnNextPressed);
    }

    /// <summary>Entry point - call when an expedition's time runs out (see ExpeditionTimeIndicator).</summary>
    public void ShowSummary()
    {
        // Resolve any in-progress capture BEFORE computing stats, so a
        // last-second successful capture still counts toward this run's totals.
        if (CaptureMinigameController.Instance != null)
            CaptureMinigameController.Instance.ForceEndMinigame();

        cachedStats = ComputeStats();

        if (popupRoot != null) popupRoot.SetActive(true);
        if (nextButton != null) nextButton.interactable = true;

        PlayerStateManager.Instance.Freeze();
        RefreshTotalMoneyText();
        ShowPage(0);
    }

    private void ShowPage(int page)
    {
        currentPage = page;

        if (page1Grid != null) page1Grid.SetActive(page == 0);
        if (page2Stats != null) page2Stats.SetActive(page == 1);

        if (page == 0 && creatureGrid != null)
            creatureGrid.BuildGrid();

        if (page == 1 && statsUI != null)
            statsUI.DisplayStats(cachedStats, milestoneThreshold);
    }

    private void OnNextPressed()
    {
        if (currentPage == 0)
            ShowPage(1);
        else
            StartCoroutine(FinishRunSequence());
    }

    private IEnumerator FinishRunSequence()
    {
        if (nextButton != null) nextButton.interactable = false; // prevent double-clicking mid-sequence

        int startMoney = GetCurrentMoney();
        int endMoney = startMoney + cachedStats.finalReward;

        // Apply the reward immediately so it's never lost even if this
        // coroutine gets interrupted - everything below is purely visual.
        AddMoney(cachedStats.finalReward);

        float t = 0f;
        while (t < moneyCountUpDuration)
        {
            t += Time.deltaTime;
            float progress = moneyCountUpDuration > 0f ? t / moneyCountUpDuration : 1f;
            SetMoneyText(Mathf.RoundToInt(Mathf.Lerp(startMoney, endMoney, progress)));
            yield return null;
        }
        SetMoneyText(endMoney);

        yield return new WaitForSeconds(holdAfterCountUp);

        if (popupRoot != null) popupRoot.SetActive(false);
        PlayerStateManager.Instance.Unfreeze();

        if (expeditionTimeIndicator != null)
            expeditionTimeIndicator.PlayBlackoutAndTeleport();
    }

    private RunStats ComputeStats()
    {
        var runCaptures = InventoryManager.Instance.GetRunCaptures();

        var stats = new RunStats { countsByRarity = new int[4] };
        var speciesSet = new HashSet<CreatureData>();

        foreach (var kvp in runCaptures)
        {
            stats.totalCaught += kvp.Value;
            stats.countsByRarity[(int)kvp.Key.rarity] += kvp.Value;
            speciesSet.Add(kvp.Key.species);
        }

        stats.differentSpecies = speciesSet.Count;
        stats.newSpeciesDiscovered = InventoryManager.Instance.GetNewSpeciesThisRunCount();
        stats.finalReward = CalculateFinalReward(stats);

        return stats;
    }

    private int CalculateFinalReward(RunStats stats)
    {
        float reward = rewardPerCritter * stats.totalCaught;
        reward += rewardPerNewSpecies * stats.newSpeciesDiscovered;

        for (int i = 0; i < stats.countsByRarity.Length && i < rewardPerRarityCritter.Length; i++)
            reward += rewardPerRarityCritter[i] * stats.countsByRarity[i];

        if (stats.totalCaught >= milestoneThreshold)
            reward += milestoneBonus;

        return Mathf.RoundToInt(reward);
    }

    private void RefreshTotalMoneyText() => SetMoneyText(GetCurrentMoney());

    private void SetMoneyText(int amount)
    {
        if (totalMoneyText != null) totalMoneyText.text = amount.ToString("N0");
    }

    // --- MoneyManager integration - the only place this script touches it ---
    private int GetCurrentMoney() => MoneyManager.Instance.GetCurrentMoney();
    private void AddMoney(int amount) => MoneyManager.Instance.AddMoney(amount);
}