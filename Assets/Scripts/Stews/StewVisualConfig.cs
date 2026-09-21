using UnityEngine;

/// <summary>Icon per dominant family, used for a brewed stew's display icon.</summary>
[CreateAssetMenu(fileName = "StewVisualConfig", menuName = "ShinyHunt/Stew Visual Config")]
public class StewVisualConfig : ScriptableObject
{
    [Tooltip("Index matches IngredientFamily enum order: Animal, Plant, Stranger, Warm, Cold.")]
    public Sprite[] familyIcons = new Sprite[5];

    public Sprite GetFamilyIcon(IngredientFamily family)
    {
        if (familyIcons == null || familyIcons.Length == 0) return null;
        int index = (int)family;
        return index < familyIcons.Length ? familyIcons[index] : familyIcons[0];
    }
}