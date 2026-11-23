using UnityEngine;

/// <summary>
/// How a speaker "sounds" while their text types out (Undertale / Celeste
/// style): a short blip per revealed letter, picked at random from Blips and
/// played at a random pitch inside Pitch Range, so it reads as a voice rather
/// than a ticking clock. Spaces and punctuation are silent.
///
/// Lives on DialogueData (one per NPC); DialogueManager's default voice is
/// used for any DialogueData whose voice has no blips.
/// </summary>
[System.Serializable]
public class DialogueVoice
{
    [Tooltip("Very short sounds (~0.03-0.1 s), one played per letter. Several similar ones = less repetitive.")]
    public AudioClip[] blips;

    [Tooltip("Each blip gets a random pitch in this range. Lower = deeper voice. (1,1) = no variation.")]
    public Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    [Range(0f, 1f)] public float volume = 0.5f;

    [Tooltip("A blip every N letters. 1 = every letter (Undertale); 2-3 = calmer, and better for fast text.")]
    [Min(1)] public int lettersPerBlip = 2;

    [Tooltip("Optional sound when a conversation with this speaker opens. Empty = DialogueManager's default.")]
    public AudioClip openSound;

    public bool HasBlips => blips != null && blips.Length > 0;
}
