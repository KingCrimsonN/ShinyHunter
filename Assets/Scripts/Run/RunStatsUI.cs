using UnityEngine;
using TMPro;

/// <summary>Page 2 of the run summary: pure display, all data handed in from RunSummaryUI.</summary>
public class RunStatsUI : MonoBehaviour
{
    [SerializeField] private TMP_Text totalCaughtText;
    [SerializeField] private TMP_Text milestoneText;
    [SerializeField] private TMP_Text differentSpeciesText;
    [SerializeField] private TMP_Text newSpeciesText;
    [Tooltip("Exactly 4, in CreatureData.Rarity enum order: Normal, Uncommon, Rare, Legendary.")]
    [SerializeField] private TMP_Text[] rarityCountTexts;
    [SerializeField] private TMP_Text finalRewardText;

    public void DisplayStats(RunStats stats, int milestoneThreshold)
    {
        if (totalCaughtText != null) totalCaughtText.text = stats.totalCaught.ToString();
        if (milestoneText != null) milestoneText.text = stats.totalCaught >= milestoneThreshold ? "Yes" : "No";
        if (differentSpeciesText != null) differentSpeciesText.text = stats.differentSpecies.ToString();
        if (newSpeciesText != null) newSpeciesText.text = stats.newSpeciesDiscovered.ToString();
        if (finalRewardText != null) finalRewardText.text = stats.finalReward.ToString("N0");

        if (rarityCountTexts != null && stats.countsByRarity != null)
        {
            for (int i = 0; i < rarityCountTexts.Length && i < stats.countsByRarity.Length; i++)
                if (rarityCountTexts[i] != null) rarityCountTexts[i].text = stats.countsByRarity[i].ToString();
        }
    }
}