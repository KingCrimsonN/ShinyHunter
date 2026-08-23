using System.Collections.Generic;

/// <summary>
/// All the animation clips for one rarity variant of a creature. CreatureData
/// holds one of these per rarity (same indexing convention as the old flat
/// sprites array: 0=Normal, 1=Uncommon, 2=Rare, 3=Legendary).
/// </summary>
[System.Serializable]
public class CreatureVariantVisuals
{
    public List<CreatureAnimationClip> animations = new List<CreatureAnimationClip>();

    /// <summary>Returns the clip for a state, or null if this variant doesn't have one authored yet.</summary>
    public CreatureAnimationClip GetClip(CreatureAnimState state)
    {
        for (int i = 0; i < animations.Count; i++)
        {
            if (animations[i].state == state)
                return animations[i];
        }
        return null;
    }
}