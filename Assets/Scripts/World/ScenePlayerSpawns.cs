using UnityEngine;

/// <summary>
/// One per scene: the list of this scene's PlayerSpawnPoints. The index in the
/// list IS the spawn ID - so don't reorder it once exits in other scenes point at
/// it (add new ones at the end).
///
/// On load it asks SceneTransitionManager which spawn the player was sent to
/// (SceneExit sets it). A valid ID = the player is moved there and turned to face
/// its direction; no ID, or one this scene doesn't have = the player stays where
/// it was placed in the scene (the default position).
///
/// Scene-scoped (holds scene objects, CLAUDE.md #4) - the pending ID itself lives
/// on the persistent SceneTransitionManager, since it has to survive the load.
/// </summary>
public class ScenePlayerSpawns : MonoBehaviour
{
    [Tooltip("Index = spawn ID. A SceneExit in another scene sends the player to one of these by its index.")]
    [SerializeField] private PlayerSpawnPoint[] spawnPoints;

    // Start (not Awake): every Awake in the scene, the player's included, has run by now.
    private void Start()
    {
        int id = SceneTransitionManager.ConsumePendingSpawnId();
        if (id < 0) return; // entered without a spawn ID - default position

        var spawn = GetSpawn(id);
        if (spawn == null)
        {
            Debug.LogWarning($"ScenePlayerSpawns: no spawn point with ID {id} in this scene - the player starts at the default position.");
            return;
        }

        var player = FindFirstObjectByType<FirstPersonController>();
        if (player == null)
        {
            Debug.LogWarning("ScenePlayerSpawns: no FirstPersonController in this scene to place.");
            return;
        }

        player.PlaceAt(spawn.transform.position, spawn.transform.rotation);
    }

    public PlayerSpawnPoint GetSpawn(int id) =>
        spawnPoints != null && id >= 0 && id < spawnPoints.Length ? spawnPoints[id] : null;

#if UNITY_EDITOR
    // Each spawn's ID floats above it, so wiring exits in other scenes is easy.
    private void OnDrawGizmos()
    {
        if (spawnPoints == null) return;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null) continue;
            UnityEditor.Handles.Label(spawnPoints[i].transform.position + Vector3.up * 2.2f, $"Spawn {i}");
        }
    }
#endif
}
