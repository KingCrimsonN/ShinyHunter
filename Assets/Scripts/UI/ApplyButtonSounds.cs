using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Adds click/hover sounds to every Button in the scene. One instance lives on
/// the persistent Overlay and another sits in the hub scene, so buttons are
/// tracked in a shared set - each button is only ever hooked once - and the
/// scan repeats on every scene load to pick up the new scene's buttons.
/// </summary>
public class ApplyButtonSounds : MonoBehaviour
{
    [SerializeField] private AudioClip click_sound;
    [SerializeField] private AudioClip hover_sound;

    private static readonly HashSet<int> hookedButtons = new HashSet<int>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        hookedButtons.Clear(); // instance IDs aren't stable across play sessions when domain reload is disabled
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        HookAllButtons();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        HookAllButtons();
    }

    // Only the persistent Overlay's instance hooks buttons. A duplicate
    // Overlay's copy is about to be destroyed, and UnityEvent silently skips
    // listeners whose target is destroyed - hooking from it would leave those
    // buttons permanently silent (and the hooked-buttons set would block anyone
    // else from fixing that). Instances outside the Overlay are redundant,
    // since the surviving one rescans on every scene load.
    private bool IsSurvivingOverlayInstance()
    {
        return UIManager.Instance == null || transform.IsChildOf(UIManager.Instance.transform);
    }

    private void HookAllButtons()
    {
        if (!IsSurvivingOverlayInstance()) return;

        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button b in buttons)
        {
            if (!hookedButtons.Add(b.GetInstanceID())) continue;

            b.onClick.AddListener(ButtonSound);
            AddHoverTrigger(b);
        }
    }

    // The camera is resolved when a sound plays, not cached: this object is
    // persistent, so a cached Camera.main would point at a destroyed camera
    // after the first scene change.
    private Transform SoundOrigin()
    {
        Camera cam = Camera.main;
        return cam != null ? cam.transform : transform;
    }

    private void ButtonSound()
    {
        PlaySound(click_sound);
    }

    private void HoverSound()
    {
        PlaySound(hover_sound);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null || SoundFXManager.instance == null) return;
        SoundFXManager.instance.PlaySoundFX(clip, SoundOrigin(), 1f);
    }

    // Adds a hover trigger to the button
    private void AddHoverTrigger(Button button)
    {
        EventTrigger trigger = button.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = button.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerEnter;
        entry.callback.AddListener(func => { HoverSound(); });
        trigger.triggers.Add(entry);
    }
}
