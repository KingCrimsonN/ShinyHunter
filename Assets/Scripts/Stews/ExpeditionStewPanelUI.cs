using UnityEngine;
using TMPro;

/// <summary>
/// The mid-run panel: what the stew you took is doing for this expedition (the
/// same StewDetailsUI read-out - scent chart, modifiers) plus how much expedition
/// time is LEFT, in seconds, ticking live.
///
/// It's a plain panel, not a popup: it doesn't freeze the player. Open/close it
/// with Toggle() from a button, or set a Toggle Key. Put this component on an
/// object that stays ACTIVE and give it a separate Panel Root to show/hide -
/// if it sat on the panel itself, hiding the panel would stop its own key check.
///
/// Reads PlayerHealth (time left, in seconds - it IS the expedition clock) and
/// ExpeditionStewManager.ActiveStew every time it is used, holding no references
/// to either, so it's safe on the persistent Overlay (CLAUDE.md #3).
/// </summary>
public class ExpeditionStewPanelUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [Tooltip("The stew's effects. Leave its Time Text empty - the time shown here is the time LEFT, not the stew's total.")]
    [SerializeField] private StewDetailsUI details;
    [Tooltip("Optional - expedition time left. Where it should appear in the layout is up to the design.")]
    [SerializeField] private TMP_Text timeLeftText;
    [Tooltip("{0} = whole seconds left.")]
    [SerializeField] private string timeLeftFormat = "{0}s";
    [Tooltip("Optional key that opens/closes the panel. None = open it from a button instead (wire its OnClick to Toggle).")]
    [SerializeField] private KeyCode toggleKey = KeyCode.None;

    private bool Visible => panelRoot != null && panelRoot.activeSelf;

    private void Start()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void Update()
    {
        if (toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey))
            Toggle();

        if (Visible) RefreshTimeLeft();
    }

    public void Toggle()
    {
        if (Visible) Hide();
        else Show();
    }

    public void Show()
    {
        if (panelRoot == null) return;

        var stew = ExpeditionStewManager.Instance != null ? ExpeditionStewManager.Instance.ActiveStew : null;
        if (details != null)
        {
            if (stew != null) details.Show(stew);
            else details.ShowEmpty();
        }

        panelRoot.SetActive(true);
        RefreshTimeLeft();
    }

    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void RefreshTimeLeft()
    {
        if (timeLeftText == null) return;

        float left = PlayerHealth.Instance != null ? Mathf.Max(0f, PlayerHealth.Instance.currentHealth) : 0f;
        timeLeftText.text = string.Format(timeLeftFormat, Mathf.CeilToInt(left));
    }
}
