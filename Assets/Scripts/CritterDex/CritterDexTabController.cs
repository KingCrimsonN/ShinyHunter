using UnityEngine;

/// <summary>
/// Sub-tab navigation WITHIN the CritterDex tablet page - "Critters" (fully
/// built) plus stub panels for future compendium sections (Tools/Ingredients/
/// NPCs/Locations). Each stub is just a placeholder GameObject (e.g. a
/// "Coming soon" label) for now - no script of its own needed. Mirrors
/// UIManager's own page-switching pattern one level down: wire each tab
/// button's OnClick to the matching Show* method in the inspector.
/// </summary>
public class CritterDexTabController : MonoBehaviour
{
    [SerializeField] private GameObject crittersPanel;
    [SerializeField] private GameObject toolsPanel;
    [SerializeField] private GameObject ingredientsPanel;
    [SerializeField] private GameObject npcsPanel;
    [SerializeField] private GameObject locationsPanel;

    private void OnEnable()
    {
        // Always land back on Critters when the dex (re)opens, rather than
        // remembering whatever sub-tab was last open.
        ShowCritters();
    }

    public void ShowCritters() => SetActivePanel(crittersPanel);
    public void ShowTools() => SetActivePanel(toolsPanel);
    public void ShowIngredients() => SetActivePanel(ingredientsPanel);
    public void ShowNpcs() => SetActivePanel(npcsPanel);
    public void ShowLocations() => SetActivePanel(locationsPanel);

    private void SetActivePanel(GameObject panel)
    {
        if (crittersPanel != null) crittersPanel.SetActive(panel == crittersPanel);
        if (toolsPanel != null) toolsPanel.SetActive(panel == toolsPanel);
        if (ingredientsPanel != null) ingredientsPanel.SetActive(panel == ingredientsPanel);
        if (npcsPanel != null) npcsPanel.SetActive(panel == npcsPanel);
        if (locationsPanel != null) locationsPanel.SetActive(panel == locationsPanel);
    }
}
