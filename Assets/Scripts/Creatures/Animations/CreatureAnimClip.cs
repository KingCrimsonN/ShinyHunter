using UnityEngine;

/// <summary>
/// One named, frame-by-frame animation: a state, its frames, and playback
/// settings. Plain serializable data (not a ScriptableObject) so it shows
/// up inline in CreatureData's inspector.
/// </summary>
[System.Serializable]
public class CreatureAnimationClip
{
    public CreatureAnimState state;
    public Sprite[] frames;
    [Tooltip("Frames per second.")]
    public float frameRate = 8f;
    [Tooltip("If false, this clip plays once and holds on the last frame (e.g. a hit reaction or capture animation).")]
    public bool loop = true;
    [Tooltip("Looping clips only: mirror the sprite (flip X) every time the clip starts over, so a one-step walk alternates left and right steps.")]
    public bool mirrorEachLoop;
}