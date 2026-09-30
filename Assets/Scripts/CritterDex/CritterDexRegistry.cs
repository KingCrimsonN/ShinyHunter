using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// The list of every species in the game - what lets the dex show a page for
/// a creature the player has never seen (CreatureData assets alone have no way
/// to enumerate "all species").
///
/// The ORDER of `species` no longer matters: the book sorts it by family
/// (see familyOrder) and keeps registry order only as the tiebreaker inside a
/// family. So adding a new critter is just "drop it in the list, anywhere" -
/// it lands on the right page automatically.
/// </summary>
[CreateAssetMenu(fileName = "CritterDexRegistry", menuName = "ShinyHunt/Critter Dex Registry")]
public class CritterDexRegistry : ScriptableObject
{
    public List<CreatureData> species = new List<CreatureData>();

    [Tooltip("Order families appear in the book, first to last. A family missing from this list (e.g. one added to the enum later) is placed after all listed ones instead of breaking the book.")]
    public List<IngredientFamily> familyOrder = new List<IngredientFamily>
    {
        IngredientFamily.Bug,
        IngredientFamily.Plant,
        IngredientFamily.Freaky,
        IngredientFamily.Warm,
        IngredientFamily.Cold,
    };

    /// <summary>
    /// Every species in BOOK order: grouped by familyOrder, registry order
    /// within a family (OrderBy is stable). Null and duplicate entries are
    /// skipped, so a half-filled inspector list can't produce blank pages.
    /// Dex number = position in this list + 1.
    /// </summary>
    public List<CreatureData> GetOrderedSpecies()
    {
        var unique = new List<CreatureData>();
        foreach (var s in species)
            if (s != null && !unique.Contains(s)) unique.Add(s);

        return unique.OrderBy(s => FamilyRank(s.family)).ToList();
    }

    private int FamilyRank(IngredientFamily family)
    {
        int index = familyOrder != null ? familyOrder.IndexOf(family) : -1;
        return index >= 0 ? index : int.MaxValue;
    }
}
