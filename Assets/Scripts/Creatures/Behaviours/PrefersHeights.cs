using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Alien: prefers taller places. When it picks where to wander, it samples a
/// few NavMesh points around its home and - most of the time - heads for the
/// highest one. Everything else (fleeing, temperament) stays normal.
///
/// It can only reach what the NavMesh reaches: rocks/ledges must be part of the
/// baked NavMesh (walkable tops, connected by slopes or NavMesh Links), or it
/// will just walk to the foot of them.
/// </summary>
public class PrefersHeights : CreatureBehaviour
{
    [Tooltip("How often it goes for the highest spot it found (the rest of the time it wanders normally).")]
    [SerializeField, Range(0f, 1f)] private float heightPreference = 0.8f;
    [Tooltip("How many spots it compares per wander leg.")]
    [SerializeField, Min(1)] private int candidates = 8;
    [Tooltip("Search radius as a multiple of the species' wander radius.")]
    [SerializeField, Min(0.1f)] private float searchRadiusMultiplier = 1.5f;
    [Tooltip("Each spot is looked for from this high above the ground, so raised surfaces are found rather than the ground under them.")]
    [SerializeField] private float sampleHeight = 6f;
    [Tooltip("The highest spot must be at least this much higher than where it stands now, or it wanders normally.")]
    [SerializeField] private float minHeightGain = 0.3f;

    public override bool TryPickWanderTarget(Vector3 center, float radius, out Vector3 target)
    {
        target = default;
        if (Random.value > heightPreference) return false;

        float searchRadius = radius * searchRadiusMultiplier;
        bool found = false;
        float bestHeight = float.MinValue;

        for (int i = 0; i < candidates; i++)
        {
            Vector2 offset = Random.insideUnitCircle * searchRadius;
            Vector3 probe = new Vector3(center.x + offset.x, center.y + sampleHeight, center.z + offset.y);

            if (!NavMesh.SamplePosition(probe, out NavMeshHit hit, sampleHeight + 2f, NavMesh.AllAreas)) continue;
            if (hit.position.y <= bestHeight) continue;

            bestHeight = hit.position.y;
            target = hit.position;
            found = true;
        }

        if (!found) return false;

        // Compare NavMesh heights with NavMesh heights (the transform sits above the mesh by the agent's Base Offset).
        float currentHeight = NavMesh.SamplePosition(transform.position, out NavMeshHit here, sampleHeight + 2f, NavMesh.AllAreas)
            ? here.position.y
            : transform.position.y;
        return bestHeight >= currentHeight + minHeightGain;
    }
}
