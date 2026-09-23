using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Spawns creatures from a weighted species list within a radius around
/// this object, keeping the active population under a cap. As creatures
/// are removed (captured, or deactivated by any other system) the spawner
/// notices and refills up to the cap after a delay.
/// </summary>
public class Spawner : MonoBehaviour
{
    /// <summary>Floor for the scent weight multiplier - a stew a species maximally hates only ever makes it rare, never literally unspawnable (avoids permanently blocking bestiary completion for a whole run).</summary>
    private const float MinScentWeightMultiplier = 0.05f;

    [System.Serializable]
    public struct SpawnableCreature
    {
        public GameObject prefab;
        [Tooltip("Relative weight - doesn't need to sum to 1. Higher = more likely.")]
        [Min(0f)] public float weight;
    }

    [Header("Species")]
    [SerializeField] private List<SpawnableCreature> spawnableCreatures;

    [Header("Area of Effect")]
    [Tooltip("Radius (world units) around this spawner that creatures can appear in.")]
    [SerializeField] private float areaRadius = 10f;
    [Tooltip("If true, spawn positions are snapped to the nearest NavMesh point (recommended for Ground/Swimming creatures).")]
    [SerializeField] private bool snapToNavMesh = true;

    [Header("Population")]
    [Tooltip("Max creatures alive from this spawner at once, AT FULL scent strength. Scaled down when the active stew smells weak - see EffectivePopulationCap / StewCalculationConfig.scentPopulationMinFraction.")]
    [SerializeField] private int populationCap = 5;
    [Tooltip("Seconds to wait before refilling a slot that's freed up (e.g. after a capture).")]
    [SerializeField] private float respawnDelay = 60f;
    [Tooltip("How often the spawner checks for freed-up slots.")]
    [SerializeField] private float checkInterval = 2f;

    private readonly List<GameObject> activeCreatures = new List<GameObject>();
    private float respawnTimer;

    /// <summary>
    /// populationCap, scaled down by how weak the active stew's scent
    /// currently is - a weak/neutral smell (or no stew at all) draws fewer
    /// creatures to the area; a strong one draws up to the full populationCap.
    /// "Strength" is the loudest single scent axis (0-100), not an average -
    /// one dominant note is what a stew "smells like". Recomputed on every
    /// use rather than cached: cheap (a 5-element scan), and the active stew
    /// never changes mid-expedition anyway, so it's always consistent without
    /// needing invalidation. See decision log.
    /// </summary>
    private int EffectivePopulationCap
    {
        get
        {
            if (ExpeditionStewManager.Instance == null) return populationCap;

            float[] scents = ExpeditionStewManager.Instance.GetScents();
            float strength = 0f;
            if (scents != null)
                foreach (float s in scents) strength = Mathf.Max(strength, s);
            strength = Mathf.Clamp01(strength / 100f);

            float minFraction = ExpeditionStewManager.Instance.GetScentPopulationMinFraction();
            float fraction = Mathf.Lerp(minFraction, 1f, strength);

            return Mathf.Max(1, Mathf.RoundToInt(populationCap * fraction));
        }
    }

    private void Start()
    {
        for (int i = 0; i < EffectivePopulationCap; i++)
            SpawnOne();

        StartCoroutine(RespawnLoop());
    }

    private IEnumerator RespawnLoop()
    {
        var wait = new WaitForSeconds(checkInterval);

        while (true)
        {
            yield return wait;

            activeCreatures.RemoveAll(c => c == null || !c.activeInHierarchy);

            if (activeCreatures.Count < EffectivePopulationCap)
            {
                respawnTimer += checkInterval;
                if (respawnTimer >= respawnDelay)
                {
                    SpawnOne();
                    respawnTimer = 0f;
                }
            }
            else
            {
                respawnTimer = 0f;
            }
        }
    }

    private void SpawnOne()
    {
        GameObject prefab = PickWeightedCreature();
        if (prefab == null) return;

        Vector3 spawnPos = GetRandomPositionInArea();

        GameObject creature = Instantiate(prefab, spawnPos, Quaternion.identity, transform);
        activeCreatures.Add(creature);
    }

    private GameObject PickWeightedCreature()
    {
        if (spawnableCreatures == null || spawnableCreatures.Count == 0) return null;

        float totalWeight = 0f;
        foreach (var entry in spawnableCreatures)
            totalWeight += GetEffectiveWeight(entry);

        if (totalWeight <= 0f) return null;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var entry in spawnableCreatures)
        {
            cumulative += GetEffectiveWeight(entry);
            if (roll <= cumulative)
                return entry.prefab;
        }

        return spawnableCreatures[spawnableCreatures.Count - 1].prefab;
    }

    /// <summary>
    /// Base weight, scaled by how well this species' scent preference/aversion
    /// matches the player's current stew scent profile (loved scents boost
    /// weight, hated scents suppress it - see decision log), and by Encounter
    /// Power's spawn-weight boost if this species' family is the boosted one.
    /// </summary>
    private float GetEffectiveWeight(SpawnableCreature entry)
    {
        float weight = entry.weight;
        if (weight <= 0f) return 0f;
        if (entry.prefab == null) return weight;

        var creatureAI = entry.prefab.GetComponent<CreatureAI>();
        var data = creatureAI != null ? creatureAI.Data : null;
        if (data == null || ExpeditionStewManager.Instance == null) return weight;

        float[] scents = ExpeditionStewManager.Instance.GetScents();
        data.GetScentAffinity(scents, out float lovedScore, out float hatedScore);
        weight *= Mathf.Max(MinScentWeightMultiplier, 1f + lovedScore - hatedScore);

        if (ExpeditionStewManager.Instance.TryGetEncounterBoostFamily(out var boostedFamily, out float boostMultiplier)
            && data.family == boostedFamily)
        {
            weight *= boostMultiplier;
        }

        return weight;
    }

    private Vector3 GetRandomPositionInArea()
    {
        Vector2 offset = Random.insideUnitCircle * areaRadius;
        Vector3 worldPos = transform.position + new Vector3(offset.x, 0f, offset.y);

        if (snapToNavMesh && NavMesh.SamplePosition(worldPos, out NavMeshHit hit, areaRadius, NavMesh.AllAreas))
            return hit.position;

        return worldPos;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, areaRadius);
    }
}