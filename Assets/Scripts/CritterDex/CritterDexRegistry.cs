using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The fixed, ordered list of every species in the game. This is what lets
/// the dex draw a locked silhouette for a creature the player has never
/// seen - CreatureData assets alone have no way to enumerate "all species".
/// List order = dex number shown in the grid.
/// </summary>
[CreateAssetMenu(fileName = "CritterDexRegistry", menuName = "ShinyHunt/Critter Dex Registry")]
public class CritterDexRegistry : ScriptableObject
{
    public List<CreatureData> species = new List<CreatureData>();
}