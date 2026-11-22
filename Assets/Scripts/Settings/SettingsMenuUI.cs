using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The settings panel: fills its controls from GameSettings, and writes every
/// change straight back (applied immediately - no Apply button). Every control
/// is optional, so a panel can show only some of them.
///
/// Works with the Option prefab rows (each row has a Dropdown, a Toggle and a
/// Slider - drag in whichever one that row uses). Dropdown options and slider
/// ranges are set here in code, whatever the prefab says.
///
/// Open()/Close() toggle Panel Root. It doesn't freeze the player or touch the
/// cursor (the main menu has no player) - if it's ever opened in-game, the
/// caller does that, as for any popup. Scene-scoped.
/// </summary>
public class SettingsMenuUI : MonoBehaviour
{
    [Tooltip("What Open/Close shows and hides. Empty = this object.")]
    [SerializeField] private GameObject panelRoot;
    [Tooltip("Optional: shown again when the panel closes (e.g. the main menu buttons), hidden while it's open.")]
    [SerializeField] private GameObject hideWhileOpen;
    [SerializeField] private bool closeOnEscape = true;

    [Header("Video")]
    [SerializeField] private TMP_Dropdown windowModeDropdown;
    [Tooltip("Labels for GameSettings.WindowMode, in order: Fullscreen (exclusive), Borderless, Windowed.")]
    [SerializeField] private string[] windowModeLabels = { "Fullscreen", "Borderless", "Windowed" };
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private Toggle vSyncToggle;

    [Header("Gameplay")]
    [SerializeField] private Slider mouseSensitivitySlider;
    [Tooltip("Optional value readout, e.g. '1.0x'.")]
    [SerializeField] private TMP_Text mouseSensitivityValue;

    [Header("Audio")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private TMP_Text masterVolumeValue;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private TMP_Text musicVolumeValue;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TMP_Text sfxVolumeValue;

    [Header("Misc")]
    [SerializeField] private TMP_Dropdown languageDropdown;
    [Tooltip("Labels for GameSettings.Language, in order: English, Spanish, Catalan (each in its own language).")]
    [SerializeField] private string[] languageLabels = { "English", "Español", "Català" };
    [Tooltip("Language is saved but nothing is translated yet - untick to grey the dropdown out until it is.")]
    [SerializeField] private bool languageSelectable = true;

    private readonly List<Vector2Int> resolutions = new List<Vector2Int>();
    private bool initialized;

    public bool IsOpen => Root.activeSelf;
    private GameObject Root => panelRoot != null ? panelRoot : gameObject;

    private void Awake() => Initialize();

    private void OnEnable()
    {
        RefreshValues();
    }

    private void OnDisable() => GameSettings.Save();

    private void Update()
    {
        if (closeOnEscape && Input.GetKeyDown(KeyCode.Escape) && IsOpen) Close();
    }

    // ---------------- Open / Close ----------------

    /// <summary>Wire to the menu's Settings button.</summary>
    public void Open()
    {
        if (hideWhileOpen != null) hideWhileOpen.SetActive(false);
        Root.SetActive(true);
        RefreshValues();
    }

    /// <summary>Wire to a Back/Close button. Saves.</summary>
    public void Close()
    {
        GameSettings.Save();
        Root.SetActive(false);
        if (hideWhileOpen != null) hideWhileOpen.SetActive(true);
    }

    public void ToggleOpen()
    {
        if (IsOpen) Close(); else Open();
    }

    // ---------------- Setup ----------------

    private void Initialize()
    {
        if (initialized) return;
        initialized = true;

        if (windowModeDropdown != null)
        {
            SetOptions(windowModeDropdown, windowModeLabels);
            windowModeDropdown.onValueChanged.AddListener(i => GameSettings.SetWindowMode((GameSettings.WindowMode)i));
        }

        if (resolutionDropdown != null)
        {
            BuildResolutionList();
            var labels = new List<string>();
            foreach (var r in resolutions) labels.Add($"{r.x} x {r.y}");
            SetOptions(resolutionDropdown, labels);
            resolutionDropdown.onValueChanged.AddListener(i =>
            {
                if (i >= 0 && i < resolutions.Count) GameSettings.SetResolution(resolutions[i].x, resolutions[i].y);
            });
        }

        if (qualityDropdown != null)
        {
            SetOptions(qualityDropdown, QualitySettings.names);
            qualityDropdown.onValueChanged.AddListener(GameSettings.SetQualityLevel);
        }

        if (vSyncToggle != null)
            vSyncToggle.onValueChanged.AddListener(GameSettings.SetVSync);

        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.wholeNumbers = false;
            mouseSensitivitySlider.minValue = GameSettings.MinMouseSensitivity;
            mouseSensitivitySlider.maxValue = GameSettings.MaxMouseSensitivity;
            mouseSensitivitySlider.onValueChanged.AddListener(v =>
            {
                GameSettings.SetMouseSensitivity(v);
                SetText(mouseSensitivityValue, $"{GameSettings.MouseSensitivity:0.0}x");
            });
        }

        SetupVolume(masterVolumeSlider, masterVolumeValue, GameSettings.VolumeChannel.Master);
        SetupVolume(musicVolumeSlider, musicVolumeValue, GameSettings.VolumeChannel.Music);
        SetupVolume(sfxVolumeSlider, sfxVolumeValue, GameSettings.VolumeChannel.Sfx);

        if (languageDropdown != null)
        {
            SetOptions(languageDropdown, languageLabels);
            languageDropdown.interactable = languageSelectable;
            languageDropdown.onValueChanged.AddListener(i => GameSettings.SetLanguage((GameSettings.Language)i));
        }
    }

    private void SetupVolume(Slider slider, TMP_Text valueText, GameSettings.VolumeChannel channel)
    {
        if (slider == null) return;
        slider.wholeNumbers = false;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.onValueChanged.AddListener(v =>
        {
            GameSettings.SetVolume(channel, v);
            SetText(valueText, VolumeLabel(v));
        });
    }

    /// <summary>Distinct sizes the monitor supports (refresh rates merged), biggest first, plus the saved one if it isn't among them.</summary>
    private void BuildResolutionList()
    {
        resolutions.Clear();
        foreach (var r in Screen.resolutions)
        {
            var size = new Vector2Int(r.width, r.height);
            if (!resolutions.Contains(size)) resolutions.Add(size);
        }

        var saved = new Vector2Int(GameSettings.ResolutionWidth, GameSettings.ResolutionHeight);
        if (saved.x > 0 && saved.y > 0 && !resolutions.Contains(saved)) resolutions.Add(saved);

        resolutions.Sort((a, b) => a.x != b.x ? b.x.CompareTo(a.x) : b.y.CompareTo(a.y));
    }

    /// <summary>Shows the current values without firing the change listeners.</summary>
    private void RefreshValues()
    {
        if (!initialized) return;

        if (windowModeDropdown != null) SetDropdown(windowModeDropdown, (int)GameSettings.CurrentWindowMode);
        if (resolutionDropdown != null)
            SetDropdown(resolutionDropdown, resolutions.IndexOf(new Vector2Int(GameSettings.ResolutionWidth, GameSettings.ResolutionHeight)));
        if (qualityDropdown != null) SetDropdown(qualityDropdown, GameSettings.QualityLevel);
        if (vSyncToggle != null) vSyncToggle.SetIsOnWithoutNotify(GameSettings.VSync);

        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.SetValueWithoutNotify(GameSettings.MouseSensitivity);
            SetText(mouseSensitivityValue, $"{GameSettings.MouseSensitivity:0.0}x");
        }

        RefreshVolume(masterVolumeSlider, masterVolumeValue, GameSettings.VolumeChannel.Master);
        RefreshVolume(musicVolumeSlider, musicVolumeValue, GameSettings.VolumeChannel.Music);
        RefreshVolume(sfxVolumeSlider, sfxVolumeValue, GameSettings.VolumeChannel.Sfx);

        if (languageDropdown != null) SetDropdown(languageDropdown, (int)GameSettings.CurrentLanguage);
    }

    private static void RefreshVolume(Slider slider, TMP_Text valueText, GameSettings.VolumeChannel channel)
    {
        if (slider == null) return;
        float v = GameSettings.GetVolume(channel);
        slider.SetValueWithoutNotify(v);
        SetText(valueText, VolumeLabel(v));
    }

    // ---------------- Helpers ----------------

    private static void SetOptions(TMP_Dropdown dropdown, IList<string> labels)
    {
        EnsureItemText(dropdown);
        dropdown.ClearOptions();
        var options = new List<TMP_Dropdown.OptionData>();
        foreach (var label in labels) options.Add(new TMP_Dropdown.OptionData(label));
        dropdown.AddOptions(options);
    }

    /// <summary>
    /// A TMP_Dropdown with no Item Text leaves every list entry showing the template's placeholder text.
    /// If it's unassigned, use the template item's label (the one named "Item Label", else the first text under the item's Toggle).
    /// </summary>
    private static void EnsureItemText(TMP_Dropdown dropdown)
    {
        if (dropdown.itemText != null || dropdown.template == null) return;

        var item = dropdown.template.GetComponentInChildren<Toggle>(true);
        if (item == null) return;

        TMP_Text label = null;
        foreach (var text in item.GetComponentsInChildren<TMP_Text>(true))
        {
            if (label == null) label = text;
            if (text.name == "Item Label") { label = text; break; }
        }

        if (label != null) dropdown.itemText = label;
        else Debug.LogWarning($"SettingsMenuUI: dropdown '{dropdown.name}' has no Item Text and no label in its template item.", dropdown);
    }

    private static void SetDropdown(TMP_Dropdown dropdown, int index)
    {
        if (index < 0 || index >= dropdown.options.Count) return;
        dropdown.SetValueWithoutNotify(index);
        dropdown.RefreshShownValue();
    }

    private static string VolumeLabel(float v) => $"{Mathf.RoundToInt(v * 100f)}%";

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null) text.text = value;
    }
}
