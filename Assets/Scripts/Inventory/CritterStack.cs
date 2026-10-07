using System.Collections.Generic;

/// <summary>
/// What one inventory tile shows: either a single critter (individual view,
/// the default) or every critter of one species+rarity (grouped view). The
/// UI builds these from a list of CapturedCritter with Build() - the inventory
/// itself never stores groups.
/// </summary>
public class CritterStack
{
    public CreatureData Species { get; }
    public CreatureData.Rarity Rarity { get; }
    public IReadOnlyList<CapturedCritter> Critters => critters;

    /// <summary>True for a grouped tile (shows a count), false for a single critter.</summary>
    public bool IsGroup { get; }

    public int Count => critters.Count;

    public int DoubleYieldCount
    {
        get
        {
            int n = 0;
            foreach (var c in critters) if (c.doubleYield) n++;
            return n;
        }
    }

    private readonly List<CapturedCritter> critters;

    private CritterStack(CreatureData species, CreatureData.Rarity rarity, List<CapturedCritter> critters, bool isGroup)
    {
        Species = species;
        Rarity = rarity;
        this.critters = critters;
        IsGroup = isGroup;
    }

    /// <summary>
    /// Turns a flat list of critters into tiles. Species+rarity groups appear in
    /// the order they were first caught. Grouped: one tile per group. Individual:
    /// one tile per critter, but still laid out group by group (capture order
    /// inside a group), so identical critters sit next to each other instead of
    /// being scattered in pure capture order.
    /// </summary>
    public static List<CritterStack> Build(IEnumerable<CapturedCritter> source, bool grouped)
    {
        var order = new List<(CreatureData species, CreatureData.Rarity rarity)>();
        var byKey = new Dictionary<(CreatureData, CreatureData.Rarity), List<CapturedCritter>>();

        foreach (var critter in source)
        {
            if (critter == null || critter.species == null) continue;

            var key = (critter.species, critter.rarity);
            if (!byKey.TryGetValue(key, out var list))
            {
                list = new List<CapturedCritter>();
                byKey[key] = list;
                order.Add(key);
            }
            list.Add(critter);
        }

        var stacks = new List<CritterStack>();
        foreach (var key in order)
        {
            var list = byKey[key];
            if (grouped)
            {
                stacks.Add(new CritterStack(key.species, key.rarity, list, isGroup: true));
            }
            else
            {
                foreach (var critter in list)
                    stacks.Add(new CritterStack(key.species, key.rarity, new List<CapturedCritter> { critter }, isGroup: false));
            }
        }

        return stacks;
    }
}
