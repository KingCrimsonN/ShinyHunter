using UnityEngine;

/// <summary>
/// Family-filter buttons above the CritterDex grid - "All" plus one per
/// IngredientFamily. Wire each button's OnClick to the matching method below
/// in the inspector (there are only 5 families - a fixed, small set, not
/// worth generating dynamically; same "explicit per-button method" convention
/// UIManager's own tablet-page buttons already use).
/// </summary>
public class CritterDexFamilyFilterUI : MonoBehaviour
{
    [SerializeField] private CritterDexGridUI gridUI;

    public void ClearFilter() => gridUI.SetFamilyFilter(null);
    public void FilterBug() => gridUI.SetFamilyFilter(IngredientFamily.Bug);
    public void FilterPlant() => gridUI.SetFamilyFilter(IngredientFamily.Plant);
    public void FilterFreaky() => gridUI.SetFamilyFilter(IngredientFamily.Freaky);
    public void FilterWarm() => gridUI.SetFamilyFilter(IngredientFamily.Warm);
    public void FilterCold() => gridUI.SetFamilyFilter(IngredientFamily.Cold);
}
