using System;

/// <summary>
/// One caught critter. The critter inventory is a list of these - one entry
/// per creature actually caught - rather than a count per species+rarity, so
/// every critter can carry its own state (today: whether it yields double
/// ingredients) and the UI can show and move a SPECIFIC one. Grouping is only
/// a display choice (CritterStack). A plain class, compared by reference.
/// </summary>
[Serializable]
public class CapturedCritter
{
    /// <summary>Unique within the play session (InventoryManager hands them out). Not used for equality - handy for debugging and a future save system.</summary>
    public int id;
    public CreatureData species;
    public CreatureData.Rarity rarity;

    /// <summary>Flagged at capture (stew Ingredient Power or the tool's extra-ingredient chance): transforming THIS critter gives double resources.</summary>
    public bool doubleYield;
}
