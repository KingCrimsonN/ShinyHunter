using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// The mid-run panel: what the stew you took is doing for this expedition (the
/// same StewDetailsUI read-out - scent chart, modifiers) plus how much expedition
/// time is LEFT, in seconds, ticking live.
///
/// It's a plain panel, not a popup: it doesn't freeze the player. Tab (Toggle
/// Key) opens/closes it during an expedition, and its Close Button hides it.
/// Put this component on an object that stays ACTIVE and give it a separate Panel
/// Root to show/hide - if it sat on the panel itself, hiding the panel would stop
/// its own key check.
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
    [Tooltip("Optional - expedition time left as text.")]
    [SerializeField] private TMP_Text timeLeftText;
    [Tooltip("Optional - expedition time left as a bar (0-1, full = full time). Turn its Interactable off.")]
    [SerializeField] private Slider timeLeftSlider;
    [Tooltip("Optional - hides the panel.")]
    [SerializeField] private Button closeButton;
    [Tooltip("{0} = whole seconds left.")]
    [SerializeField] private string timeLeftFormat = "{0}s";
    [Tooltip("Key that opens/closes the panel. None = open it from a button instead (wire its OnClick to Toggle).")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

    private bool Visible => panelRoot != null && panelRoot.activeSelf;

    private void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
    }

    // The Overlay is persistent, so Start only ever runs once: hide again on every scene load.
    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Hide();

    private void Start()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void Update()
    {
        if (toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey) && CanToggleWithKey())
            Toggle();

        if (Visible) RefreshTimeLeft();
    }

    /// <summary>Only on an expedition, and not while a popup (shop, tablet...) has the player frozen - hiding is always allowed.</summary>
    private bool CanToggleWithKey()
    {
        if (Visible) return true;
        if (SceneNames.IsHub(SceneManager.GetActiveScene().name)) return false;
        return PlayerStateManager.Instance == null || !PlayerStateManager.Instance.IsFrozen;
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
        var health = PlayerHealth.Instance;
        float left = health != null ? Mathf.Max(0f, health.currentHealth) : 0f;

        if (timeLeftText != null) timeLeftText.text = StewDisplayUtil.FormatTime(left);
        if (timeLeftSlider != null)
            timeLeftSlider.value = health != null && health.MaxHealth > 0f ? Mathf.Clamp01(left / health.MaxHealth) : 0f;
    }
}
