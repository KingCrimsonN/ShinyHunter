using UnityEngine;

/// <summary>
/// A critter's sounds: a natural sound every now and then, and a hit sound
/// whenever it is hit (or bumps into something). The clips are per species, on
/// CreatureData (Sounds); this component only plays them, from a 3D AudioSource
/// on the critter so you can hear where it is and it follows the critter.
///
/// CreatureAI adds this automatically to every critter. Add it to a prefab
/// yourself only to tune the hearing distances.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class CreatureAudio : CreatureBehaviour
{
    [Tooltip("Full volume within this distance.")]
    [SerializeField] private float minDistance = 2f;
    [Tooltip("Silent beyond this distance.")]
    [SerializeField] private float maxDistance = 25f;
    [Tooltip("Random pitch range per sound, so repeats don't sound identical. (1,1) = off.")]
    [SerializeField] private Vector2 pitchRange = new Vector2(0.92f, 1.08f);

    private AudioSource source;
    private float ambientTimer;

    protected override void OnBound()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;

        // Random first delay, so a group spawned together doesn't call in unison.
        ScheduleAmbient(firstTime: true);
    }

    private void Update()
    {
        if (AI == null || AI.Data == null) return;
        if (AI.CurrentState == CreatureAI.State.Captured || AI.CurrentState == CreatureAI.State.Stunned) return;

        ambientTimer -= Time.deltaTime;
        if (ambientTimer > 0f) return;

        PlayRandom(AI.Data.ambientSounds);
        ScheduleAmbient(firstTime: false);
    }

    public override void OnHit(bool stunned)
    {
        if (AI.Data != null) PlayRandom(AI.Data.hitSounds);
    }

    private void ScheduleAmbient(bool firstTime)
    {
        Vector2 range = AI.Data != null ? AI.Data.ambientSoundInterval : new Vector2(6f, 15f);
        ambientTimer = Random.Range(firstTime ? 0f : range.x, range.y);
    }

    private void PlayRandom(AudioClip[] clips)
    {
        if (source == null || clips == null || clips.Length == 0) return;

        var clip = clips[Random.Range(0, clips.Length)];
        if (clip == null) return;

        source.pitch = Random.Range(pitchRange.x, pitchRange.y);
        source.PlayOneShot(clip, AI.Data.soundVolume);
    }
}
