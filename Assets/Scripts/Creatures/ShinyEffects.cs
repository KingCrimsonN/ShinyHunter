using UnityEngine;

/// <summary>
/// A critter's shiny particles (FX/Shiny/ShinyFX.prefab, built by
/// Tools > ShinyHunt > Build Shiny FX Prefab). One prefab holds all three
/// shiny rarities; CreatureAI calls Setup with the rolled rarity and only that
/// rarity's effects run:
///   - CONSTANT effects loop for as long as the critter lives (motes, thin
///     stars, glow and rays, depending on rarity).
///   - The SPAWN burst plays once - by default the first time the player
///     actually sees the critter, since a burst nobody saw is wasted (like the
///     shiny sparkle on an encounter). Can be switched to "on spawn".
/// Arrays are indexed by CreatureData.Rarity: 0 = Normal (nothing), 1 =
/// Sparkling, 2 = Shining, 3 = Radiant.
///
/// Sits under the critter, whose Billboard turns +Z toward the camera, so the
/// prefab's "behind" parts are simply placed at negative local Z.
/// </summary>
public class ShinyEffects : MonoBehaviour
{
    public enum SpawnTrigger { FirstSeen, OnSpawn, Never }

    [Header("Per rarity: 0 Normal (unused), 1 Sparkling, 2 Shining, 3 Radiant")]
    [Tooltip("Looping effects - the object is enabled only for the rolled rarity.")]
    [SerializeField] private GameObject[] constantEffects = new GameObject[4];
    [Tooltip("One-shot burst roots - every ParticleSystem under it plays once.")]
    [SerializeField] private GameObject[] spawnEffects = new GameObject[4];
    [Tooltip("Optional sound played with the spawn burst (e.g. the shiny chime).")]
    [SerializeField] private AudioClip[] spawnSounds = new AudioClip[4];
    [SerializeField, Range(0f, 1f)] private float spawnSoundVolume = 0.6f;

    [Header("Spawn burst")]
    [SerializeField] private SpawnTrigger spawnTrigger = SpawnTrigger.FirstSeen;
    [Tooltip("FirstSeen: farther than this doesn't count as seen.")]
    [SerializeField] private float seenDistance = 30f;
    [Tooltip("FirstSeen: how far inside the screen edges it must be (0 = anywhere on screen).")]
    [SerializeField, Range(0f, 0.4f)] private float viewMargin = 0.1f;
    [Tooltip("FirstSeen: things that hide it (terrain, trees...). Nothing = no line-of-sight check.")]
    [SerializeField] private LayerMask occluders = ~0;

    private int rarity;
    private bool burstPending;
    private Camera cachedCamera;

    /// <summary>Called by CreatureAI right after it rolls the rarity. Safe before this object's Awake.</summary>
    public void Setup(CreatureData.Rarity rolledRarity)
    {
        rarity = (int)rolledRarity;

        for (int i = 0; i < 4; i++)
        {
            SetActive(constantEffects, i, i == rarity);
            SetActive(spawnEffects, i, false); // activated when the burst plays
        }

        burstPending = rarity > 0 && spawnTrigger != SpawnTrigger.Never;
        if (burstPending && spawnTrigger == SpawnTrigger.OnSpawn) PlaySpawnBurst();
    }

    /// <summary>Plays this rarity's one-shot burst (and sound) now. Can be called again, e.g. for a re-reveal.</summary>
    public void PlaySpawnBurst()
    {
        burstPending = false;
        if (rarity <= 0 || spawnEffects == null || rarity >= spawnEffects.Length || spawnEffects[rarity] == null) return;

        var root = spawnEffects[rarity];
        root.SetActive(true);
        foreach (var ps in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.Clear(true);
            ps.Play(false);
        }

        var clip = spawnSounds != null && rarity < spawnSounds.Length ? spawnSounds[rarity] : null;
        if (clip != null && SoundFXManager.instance != null)
            SoundFXManager.instance.PlaySoundFX(clip, transform, spawnSoundVolume);
    }

    private void Update()
    {
        if (!burstPending || spawnTrigger != SpawnTrigger.FirstSeen) return;
        if (IsSeen()) PlaySpawnBurst();
    }

    private bool IsSeen()
    {
        if (cachedCamera == null) cachedCamera = Camera.main;
        if (cachedCamera == null) return false;

        Vector3 eye = cachedCamera.transform.position;
        Vector3 point = transform.position;
        Vector3 toPoint = point - eye;
        if (toPoint.sqrMagnitude > seenDistance * seenDistance) return false;

        Vector3 viewport = cachedCamera.WorldToViewportPoint(point);
        if (viewport.z <= 0f) return false;
        if (viewport.x < viewMargin || viewport.x > 1f - viewMargin || viewport.y < viewMargin || viewport.y > 1f - viewMargin) return false;

        if (occluders.value == 0) return true;

        float distance = toPoint.magnitude;
        var owner = GetComponentInParent<CreatureAI>();
        Transform critter = owner != null ? owner.transform : transform;
        foreach (var hit in Physics.RaycastAll(eye, toPoint / distance, distance, occluders, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider == null || hit.collider.transform.IsChildOf(critter)) continue;
            if (hit.collider.CompareTag("Player") || hit.collider.transform.IsChildOf(cachedCamera.transform.root)) continue;
            return false;
        }
        return true;
    }

    private static void SetActive(GameObject[] array, int index, bool active)
    {
        if (array != null && index < array.Length && array[index] != null) array[index].SetActive(active);
    }
}
