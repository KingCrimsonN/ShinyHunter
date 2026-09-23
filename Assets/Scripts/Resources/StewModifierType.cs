/// <summary>The one modifier a stew can carry, or None if no ingredient theme dominated the recipe enough.</summary>
public enum StewModifierType
{
    None,
    ShinyPower,      // rarer critter spawns - TRIGGERED by average ingredient RARITY, not a family, but its bonus only benefits creatures of the recipe's dominant family (StewInstance.dominantFamily) - see ExpeditionStewManager.GetRarityChanceMultiplier
    IngredientPower, // chance of double resources on transform - Animal family
    EncounterPower,  // extra spawn weight for a random family - Stranger family
    ChronoPower,     // banked bonus time for the NEXT expedition - Warm family
    CapturePower,    // easier captures - Cold family
    SoothingPower    // slower, "blinder" critters - Plant family
}