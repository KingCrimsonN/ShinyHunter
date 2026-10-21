using UnityEngine;

/// <summary>
/// An exit to a neighbouring scene of the world grid. Like Doors, but it also
/// names WHERE in the next scene the player comes out: Target Spawn ID is an index
/// into that scene's ScenePlayerSpawns list (-1 = that scene's default position).
///
/// Works either way:
///  - Interact (press E while looking at it) - needs a collider the Interactor's ray hits;
///  - Walk-in - tick Use Trigger and give it a collider with Is Trigger on.
/// Walk-in ignores the player for a moment after the scene loads, so arriving on
/// top of an exit can't bounce you straight back.
///
/// This only moves between expedition scenes: the run, its clock and its
/// captures carry on (PlayerHealth / InventoryManager only start a new run when
/// coming from the hub).
/// </summary>
public class SceneExit : MonoBehaviour, IInteractable
{
    [Tooltip("Scene to load (must be in Build Settings).")]
    [SerializeField] private string sceneName;
    [Tooltip("Index of the spawn point in the NEXT scene's ScenePlayerSpawns list. -1 = its default player position.")]
    [SerializeField] private int targetSpawnId = -1;

    [Header("Walk-in")]
    [Tooltip("Leave the scene by walking into this object's trigger collider, instead of pressing E.")]
    [SerializeField] private bool useTrigger = true;
    [Tooltip("Seconds after the scene starts during which walking in does nothing.")]
    [SerializeField] private float armDelay = 1f;

    private float armedAt;

    private void Start()
    {
        armedAt = Time.time + armDelay;
    }

    public void Interact()
    {
        Use();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!useTrigger || Time.time < armedAt || !other.CompareTag("Player")) return;
        Use();
    }

    public void Use()
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning($"{name}: SceneExit has no scene name.");
            return;
        }

        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogError($"{name}: no SceneTransitionManager - can't change scene.");
            return;
        }

        SceneTransitionManager.Instance.TransitionToScene(sceneName, targetSpawnId);
    }
}
