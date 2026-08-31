using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ApplyButtonSounds : MonoBehaviour
{
    [SerializeField] private AudioClip click_sound;
    [SerializeField] private AudioClip hover_sound;
    private Transform camTransform;
    // Start is called before the first frame update
    void Start()
    {
        camTransform = Camera.main.transform;
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button b in buttons)
        {
            b.onClick.AddListener(ButtonSound);
            AddEventByScript(b);
        }

    }

    private void ButtonSound()
    {
        SoundFXManager.instance.PlaySoundFX(click_sound, camTransform, 1f);
    }

    private void HoverSound()
    {
        SoundFXManager.instance.PlaySoundFX(hover_sound, camTransform, 1f);
    }

    // Adds triggers for click and hover on the buttons
    private void AddEventByScript(Button button)
    {
        if (button.GetComponent<EventTrigger>() == null)
        {
            button.AddComponent<EventTrigger>();
        }
        EventTrigger trigger = button.GetComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerEnter;
        entry.callback.AddListener(func => { HoverSound(); });
        trigger.triggers.Add(entry);
    }

}