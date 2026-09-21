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
    [Tooltip("Max creatures alive from this spawner at once.")]
    [SerializeField] private int populationCap = 5;
    [Tooltip("Seconds to wait before refilling a slot that's freed up (e.g. after a capture).")]
    [SerializeField] private float respawnDelay = 60f;
    [Tooltip("How often the spawner checks for freed-up slots.")]
    [SerializeField] private float checkInterval = 2f;

    private readonly List<GameObject> activeCreatures = new List<GameObject>();
    private float respawnTimer;

    private void Start()
    {
        for (int i = 0; i < populationCap; i++)
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

            if (activeCreatures.Count < populationCap)
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
    /// Base weight, scaled by how well this species' scent preference
    /// matches the player's current stew scent profile, and by Encounter
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
        if (data.scentPreference != null && data.scentPreference.Length == 5)
        {
            float scentScore = 0f;
            for (int i = 0; i < 5; i++)
                scentScore += data.scentPreference[i] * (scents[i] / 5f); // scent 1-5 -> 0-1
            weight *= 1f + scentScore;
        }

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