using UnityEngine;
using UnityEngine.UI;

/// <summary>Shown right after brewing - the stew's read-out + Accept (into a bowl) / Dump (discard the stew, bowl stays free).</summary>
public class StewResultPopupUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [Tooltip("The shared stew read-out (icon, name, time, scent chart, modifiers).")]
    [SerializeField] private StewDetailsUI details;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button dumpButton;

    private StewInstance currentStew;

    private void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (acceptButton != null) acceptButton.onClick.AddListener(Accept);
        if (dumpButton != null) dumpButton.onClick.AddListener(Dump);
    }

    public void Show(StewInstance stew)
    {
        currentStew = stew;
        if (details != null) details.Show(stew);
        if (panelRoot != null) panelRoot.SetActive(true);
    }

    private void Accept()
    {
        StewInventoryManager.Instance.TryAddStew(currentStew);
        Hide();
    }

    private void Dump()
    {
        // Ingredients were already spent when brewing started - dumping just discards the stew, the bowl stays free.
        Hide();
    }

    private void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        currentStew = null;
        BrewingStationUI.Instance?.RefreshAfterResult();
    }
}
