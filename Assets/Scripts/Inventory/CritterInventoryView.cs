using System;

/// <summary>
/// The player's chosen layout for critter inventories: individual (default -
/// one tile per caught critter) or grouped (one tile per species+rarity with a
/// count). ONE shared setting, so flipping it in the inventory also flips the
/// transform table and vice versa. Static, so it survives scene loads; resets
/// to individual when the game is restarted.
/// </summary>
public static class CritterInventoryView
{
    public static bool Grouped { get; private set; }

    /// <summary>Fired whenever the layout changes - every critter grid re-draws on it.</summary>
    public static event Action Changed;

    public static void SetGrouped(bool grouped)
    {
        if (Grouped == grouped) return;
        Grouped = grouped;
        Changed?.Invoke();
    }

    public static void Toggle() => SetGrouped(!Grouped);
}
