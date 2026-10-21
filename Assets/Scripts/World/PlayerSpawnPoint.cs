using UnityEngine;

/// <summary>
/// One place the player can appear when entering this scene - put it on the
/// PlayerSpawnPoint prefab and drop one wherever a player can emerge (in front of
/// each exit, where the hub path arrives...). Position AND rotation are used:
/// point the blue arrow (the object's forward, Z) the way the player should face.
///
/// Its ID is its index in the scene's ScenePlayerSpawns list - a SceneExit in
/// ANOTHER scene names that ID to arrive here.
/// </summary>
public class PlayerSpawnPoint : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        Vector3 feet = transform.position;
        Vector3 forward = transform.forward;

        Gizmos.color = new Color(0.3f, 0.6f, 1f);
        Gizmos.DrawWireSphere(feet + Vector3.up * 0.5f, 0.4f);
        Gizmos.DrawWireSphere(feet + Vector3.up * 1.4f, 0.4f);
        Gizmos.DrawLine(feet + Vector3.up * 1f, feet + Vector3.up * 1f + forward * 1.5f);
        Gizmos.DrawLine(feet + Vector3.up * 1f + forward * 1.5f, feet + Vector3.up * 1f + forward * 1.1f + transform.right * 0.3f);
        Gizmos.DrawLine(feet + Vector3.up * 1f + forward * 1.5f, feet + Vector3.up * 1f + forward * 1.1f - transform.right * 0.3f);
    }
}
