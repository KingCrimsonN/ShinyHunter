using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Defaults, ranges and audio wiring for the player settings (GameSettings).
/// Exactly one lives at Assets/Resources/GameSettingsConfig.asset - GameSettings
/// loads it by that name, so don't rename or move it.
///
/// Volumes: each mixer must EXPOSE its Master group's Volume under the
/// parameter name below (Audio Mixer window > Exposed Parameters).
/// Music and SoundFX mixers output into the Master mixer, so the Master slider
/// scales everything.
/// </summary>
[CreateAssetMenu(fileName = "GameSettingsConfig", menuName = "ShinyHunt/Game Settings Config")]
public class GameSettingsConfig : ScriptableObject
{
    [Header("Audio mixers")]
    public AudioMixer masterMixer;
    public string masterParameter = "MasterVolume";
    public AudioMixer musicMixer;
    public string musicParameter = "MusicVolume";
    public AudioMixer sfxMixer;
    public string sfxParameter = "SFXVolume";

    [Header("Audio defaults (0-1, slider position)")]
    [Range(0f, 1f)] public float defaultMasterVolume = 1f;
    [Range(0f, 1f)] public float defaultMusicVolume = 0.8f;
    [Range(0f, 1f)] public float defaultSfxVolume = 1f;

    [Header("Mouse sensitivity (multiplier on FirstPersonController's base)")]
    [Min(0.01f)] public float minMouseSensitivity = 0.1f;
    [Min(0.01f)] public float maxMouseSensitivity = 3f;
    [Min(0.01f)] public float defaultMouseSensitivity = 1f;
}
