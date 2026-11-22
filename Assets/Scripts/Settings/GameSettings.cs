using System;
using UnityEngine;

/// <summary>
/// The player's settings: current values, saving (PlayerPrefs) and applying
/// them to the engine. Static - no scene object, no persistent manager - so it
/// works in every scene, including pressing Play straight from a level.
///
/// Loaded and applied once before the first scene loads (video right away,
/// audio on the first frame - see AudioApplier). After that only the Set*
/// methods change anything; each applies immediately and stores the value.
/// Call Save() to flush to disk (the settings panel does on close; quitting
/// does too).
///
/// UI goes through SettingsMenuUI; gameplay code just reads the values
/// (e.g. FirstPersonController reads MouseSensitivity every frame).
/// </summary>
public static class GameSettings
{
    public enum WindowMode { Fullscreen, Borderless, Windowed }
    public enum Language { English, Spanish, Catalan }
    public enum VolumeChannel { Master, Music, Sfx }

    private const string Prefix = "settings.";
    private const string KeyWindowMode = Prefix + "windowMode";
    private const string KeyWidth = Prefix + "resolutionWidth";
    private const string KeyHeight = Prefix + "resolutionHeight";
    private const string KeyQuality = Prefix + "quality";
    private const string KeyVSync = Prefix + "vsync";
    private const string KeySensitivity = Prefix + "mouseSensitivity";
    private const string KeyMaster = Prefix + "volumeMaster";
    private const string KeyMusic = Prefix + "volumeMusic";
    private const string KeySfx = Prefix + "volumeSfx";
    private const string KeyLanguage = Prefix + "language";

    private const string ConfigResourceName = "GameSettingsConfig";

    public static WindowMode CurrentWindowMode { get; private set; }
    public static int ResolutionWidth { get; private set; }
    public static int ResolutionHeight { get; private set; }
    public static int QualityLevel { get; private set; }
    public static bool VSync { get; private set; }
    /// <summary>Multiplier on FirstPersonController's base sensitivity (1 = as designed).</summary>
    public static float MouseSensitivity { get { EnsureLoaded(); return mouseSensitivity; } }
    public static float MasterVolume { get; private set; }
    public static float MusicVolume { get; private set; }
    public static float SfxVolume { get; private set; }
    /// <summary>Saved only - nothing is translated yet. Subscribe to LanguageChanged when localisation arrives.</summary>
    public static Language CurrentLanguage { get; private set; }

    /// <summary>Fires after the language changes (nothing listens yet).</summary>
    public static event Action<Language> LanguageChanged;

    private static float mouseSensitivity = 1f;
    private static bool loaded;
    private static GameSettingsConfig config;

    public static GameSettingsConfig Config
    {
        get
        {
            if (config == null) config = Resources.Load<GameSettingsConfig>(ConfigResourceName);
            return config;
        }
    }

    private static UnityEngine.Audio.AudioMixerGroup sfxGroup;
    private static UnityEngine.Audio.AudioMixerGroup musicGroup;

    /// <summary>The SoundFX mixer's Master group - the Output for any sound-effect AudioSource made in code. Null if the config/mixer is missing.</summary>
    public static UnityEngine.Audio.AudioMixerGroup SfxGroup => sfxGroup != null ? sfxGroup : (sfxGroup = MasterGroupOf(Config != null ? Config.sfxMixer : null));
    /// <summary>The Music mixer's Master group.</summary>
    public static UnityEngine.Audio.AudioMixerGroup MusicGroup => musicGroup != null ? musicGroup : (musicGroup = MasterGroupOf(Config != null ? Config.musicMixer : null));

    private static UnityEngine.Audio.AudioMixerGroup MasterGroupOf(UnityEngine.Audio.AudioMixer mixer)
    {
        if (mixer == null) return null;
        var groups = mixer.FindMatchingGroups("Master");
        return groups != null && groups.Length > 0 ? groups[0] : null;
    }

    public static float MinMouseSensitivity =>Config != null ? Config.minMouseSensitivity : 0.1f;
    public static float MaxMouseSensitivity => Config != null ? Config.maxMouseSensitivity : 3f;

    // ---------------- Startup ----------------

    /// <summary>Static state survives play sessions when domain reload is off - start clean each time.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        loaded = false;
        config = null;
        sfxGroup = null;
        musicGroup = null;
        LanguageChanged = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        EnsureLoaded();
        ApplyVideo();
        ApplyAudio();

        // AudioMixer.SetFloat can be ignored this early - apply again on the first frame.
        var applier = new GameObject("GameSettings Audio Applier") { hideFlags = HideFlags.HideAndDontSave };
        UnityEngine.Object.DontDestroyOnLoad(applier);
        applier.AddComponent<AudioApplier>();

        Application.quitting -= Save;
        Application.quitting += Save;
    }

    private class AudioApplier : MonoBehaviour
    {
        private void Start()
        {
            ApplyAudio();
            Destroy(gameObject);
        }
    }

    private static void EnsureLoaded()
    {
        if (loaded) return;
        loaded = true;

        var cfg = Config;
        if (cfg == null) Debug.LogWarning($"GameSettings: no '{ConfigResourceName}' asset in a Resources folder - volumes won't reach the mixers.");

        // First run: start from whatever the game is already doing (player settings), not from made-up values.
        CurrentWindowMode = (WindowMode)PlayerPrefs.GetInt(KeyWindowMode, (int)FromUnity(Screen.fullScreenMode));
        ResolutionWidth = PlayerPrefs.GetInt(KeyWidth, Screen.currentResolution.width);
        ResolutionHeight = PlayerPrefs.GetInt(KeyHeight, Screen.currentResolution.height);
        QualityLevel = Mathf.Clamp(PlayerPrefs.GetInt(KeyQuality, QualitySettings.GetQualityLevel()), 0, QualitySettings.names.Length - 1);
        VSync = PlayerPrefs.GetInt(KeyVSync, QualitySettings.vSyncCount > 0 ? 1 : 0) == 1;

        mouseSensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(KeySensitivity, cfg != null ? cfg.defaultMouseSensitivity : 1f),
            MinMouseSensitivity, MaxMouseSensitivity);

        MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KeyMaster, cfg != null ? cfg.defaultMasterVolume : 1f));
        MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KeyMusic, cfg != null ? cfg.defaultMusicVolume : 1f));
        SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KeySfx, cfg != null ? cfg.defaultSfxVolume : 1f));

        CurrentLanguage = (Language)Mathf.Clamp(PlayerPrefs.GetInt(KeyLanguage, 0), 0, Enum.GetValues(typeof(Language)).Length - 1);
    }

    /// <summary>Writes the settings to disk. Set* only stage them in PlayerPrefs.</summary>
    public static void Save() => PlayerPrefs.Save();

    // ---------------- Setters (apply immediately) ----------------

    public static void SetWindowMode(WindowMode mode)
    {
        EnsureLoaded();
        CurrentWindowMode = mode;
        PlayerPrefs.SetInt(KeyWindowMode, (int)mode);
        ApplyScreen();
    }

    public static void SetResolution(int width, int height)
    {
        EnsureLoaded();
        ResolutionWidth = width;
        ResolutionHeight = height;
        PlayerPrefs.SetInt(KeyWidth, width);
        PlayerPrefs.SetInt(KeyHeight, height);
        ApplyScreen();
    }

    public static void SetQualityLevel(int level)
    {
        EnsureLoaded();
        QualityLevel = Mathf.Clamp(level, 0, QualitySettings.names.Length - 1);
        PlayerPrefs.SetInt(KeyQuality, QualityLevel);
        ApplyQuality();
    }

    public static void SetVSync(bool on)
    {
        EnsureLoaded();
        VSync = on;
        PlayerPrefs.SetInt(KeyVSync, on ? 1 : 0);
        QualitySettings.vSyncCount = on ? 1 : 0;
    }

    public static void SetMouseSensitivity(float value)
    {
        EnsureLoaded();
        mouseSensitivity = Mathf.Clamp(value, MinMouseSensitivity, MaxMouseSensitivity);
        PlayerPrefs.SetFloat(KeySensitivity, mouseSensitivity);
    }

    /// <param name="value">0-1 (slider position; converted to decibels for the mixer).</param>
    public static void SetVolume(VolumeChannel channel, float value)
    {
        EnsureLoaded();
        value = Mathf.Clamp01(value);
        switch (channel)
        {
            case VolumeChannel.Master: MasterVolume = value; PlayerPrefs.SetFloat(KeyMaster, value); break;
            case VolumeChannel.Music: MusicVolume = value; PlayerPrefs.SetFloat(KeyMusic, value); break;
            case VolumeChannel.Sfx: SfxVolume = value; PlayerPrefs.SetFloat(KeySfx, value); break;
        }
        ApplyVolume(channel);
    }

    public static float GetVolume(VolumeChannel channel)
    {
        EnsureLoaded();
        switch (channel)
        {
            case VolumeChannel.Music: return MusicVolume;
            case VolumeChannel.Sfx: return SfxVolume;
            default: return MasterVolume;
        }
    }

    public static void SetLanguage(Language language)
    {
        EnsureLoaded();
        if (CurrentLanguage == language) return;
        CurrentLanguage = language;
        PlayerPrefs.SetInt(KeyLanguage, (int)language);
        LanguageChanged?.Invoke(language);
    }

    // ---------------- Applying ----------------

    private static void ApplyVideo()
    {
        ApplyQuality(); // first: changing the quality level also resets vSyncCount
        ApplyScreen();
    }

    private static void ApplyQuality()
    {
        QualitySettings.SetQualityLevel(QualityLevel, true);
        QualitySettings.vSyncCount = VSync ? 1 : 0;
    }

    private static void ApplyScreen()
    {
        if (ResolutionWidth <= 0 || ResolutionHeight <= 0)
        {
            ResolutionWidth = Screen.currentResolution.width;
            ResolutionHeight = Screen.currentResolution.height;
        }
        Screen.SetResolution(ResolutionWidth, ResolutionHeight, ToUnity(CurrentWindowMode));
    }

    private static void ApplyAudio()
    {
        ApplyVolume(VolumeChannel.Master);
        ApplyVolume(VolumeChannel.Music);
        ApplyVolume(VolumeChannel.Sfx);
    }

    private static void ApplyVolume(VolumeChannel channel)
    {
        var cfg = Config;
        if (cfg == null) return;

        switch (channel)
        {
            case VolumeChannel.Master: SetMixer(cfg.masterMixer, cfg.masterParameter, MasterVolume); break;
            case VolumeChannel.Music: SetMixer(cfg.musicMixer, cfg.musicParameter, MusicVolume); break;
            case VolumeChannel.Sfx: SetMixer(cfg.sfxMixer, cfg.sfxParameter, SfxVolume); break;
        }
    }

    private static void SetMixer(UnityEngine.Audio.AudioMixer mixer, string parameter, float linear)
    {
        if (mixer == null || string.IsNullOrEmpty(parameter)) return;
        if (!mixer.SetFloat(parameter, ToDecibels(linear)))
            Debug.LogWarning($"GameSettings: mixer '{mixer.name}' has no exposed parameter '{parameter}'.");
    }

    /// <summary>Slider 0-1 to mixer dB: perceptually even steps; 0 = silent (-80 dB).</summary>
    public static float ToDecibels(float linear) => linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f;

    private static FullScreenMode ToUnity(WindowMode mode)
    {
        switch (mode)
        {
            case WindowMode.Fullscreen: return FullScreenMode.ExclusiveFullScreen;
            case WindowMode.Windowed: return FullScreenMode.Windowed;
            default: return FullScreenMode.FullScreenWindow;
        }
    }

    private static WindowMode FromUnity(FullScreenMode mode)
    {
        switch (mode)
        {
            case FullScreenMode.ExclusiveFullScreen: return WindowMode.Fullscreen;
            case FullScreenMode.Windowed: return WindowMode.Windowed;
            default: return WindowMode.Borderless;
        }
    }
}
