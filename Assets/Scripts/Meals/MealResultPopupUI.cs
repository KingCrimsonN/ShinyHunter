using UnityEngine;
using UnityEngine.UI;

/// <summary>Shown right after brewing - the meal's read-out + Accept (into a bowl) / Dump (discard the meal, bowl stays free).</summary>
public class MealResultPopupUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [Tooltip("The shared meal read-out (icon, name, time, scent chart, modifiers).")]
    [SerializeField] private MealDetailsUI details;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button dumpButton;

    private MealInstance currentMeal;

    private void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (acceptButton != null) acceptButton.onClick.AddListener(Accept);
        if (dumpButton != null) dumpButton.onClick.AddListener(Dump);
    }

    public void Show(MealInstance meal)
    {
        currentMeal = meal;
        if (details != null) details.Show(meal);
        if (panelRoot != null) panelRoot.SetActive(true);
    }

    private void Accept()
    {
        MealInventoryManager.Instance.TryAddMeal(currentMeal);
        Hide();
    }

    private void Dump()
    {
        // Ingredients were already spent when brewing started - dumping just discards the meal, the bowl stays free.
        Hide();
    }

    private void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        currentMeal = null;
        BrewingStationUI.Instance?.RefreshAfterResult();
    }
}
