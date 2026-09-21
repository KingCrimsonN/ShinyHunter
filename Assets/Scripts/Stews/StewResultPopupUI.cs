using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Shown right after brewing - stats + Accept (into a bowl) / Dump (discard the stew, bowl stays free).</summary>
public class StewResultPopupUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text timeText;
    [Tooltip("Exactly 5, in order: Sweet, Fresh, Putrid, Metallic, Marine.")]
    [SerializeField] private TMP_Text[] scentTexts;
    [SerializeField] private TMP_Text modifierText;
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

        if (icon != null) icon.sprite = stew.icon;
        if (timeText != null) timeText.text = $"{stew.timeSeconds / 60f:0.#} min";
        StewDisplayUtil.SetScentTexts(scentTexts, stew.scents);
        if (modifierText != null) modifierText.text = StewDisplayUtil.FormatModifier(stew);

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