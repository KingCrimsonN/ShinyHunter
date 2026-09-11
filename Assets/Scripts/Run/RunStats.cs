/// <summary>Computed once per run-end, shared between RunSummaryUI (reward calc) and RunStatsUI (display).</summary>
public struct RunStats
{
    public int totalCaught;
    public int differentSpecies;
    public int newSpeciesDiscovered;
    /// <summary>Index matches CreatureData.Rarity: 0=Normal, 1=Uncommon, 2=Rare, 3=Legendary.</summary>
    public int[] countsByRarity;
    public int finalReward;
}