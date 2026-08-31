using UnityEngine;

/// <summary>
/// Handles the "hit with a stick, then capture" loop.
/// Swing (SphereCast) stuns any ICapturable it connects with; capture only
/// succeeds against a currently-stunned, targeted creature.
///
/// This is intentionally decoupled from CreatureAI via ICapturable so it can
/// later be replaced/extended with the GDD's seal-throw + QTE system without
/// touching creature code.
/// </summary>
public class PlayerCapture : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Animator handAnimator;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip swingSound;

    [Header("Stick Hit")]
    [SerializeField] private float hitRange = 2.5f;
    [SerializeField] private float hitRadius = 0.5f;
    [SerializeField] private LayerMask creatureLayer;
    [SerializeField] private KeyCode hitKey = KeyCode.Mouse0;

    private ICapturable targetedCreature;

    public bool isActive;

    private void Update()
    {
        if (!isActive)
        {
            return;
        }

        if (Input.GetKeyDown(hitKey))
            TrySwingStick();
    }

    private void TrySwingStick()
    {
        Vector3 origin = Camera.main.transform.position;
        Vector3 dir = Camera.main.transform.forward;
        handAnimator.SetTrigger("Hit");
        SoundFXManager.instance.PlaySoundFX(swingSound, transform, 0.5f);

        if (Physics.SphereCast(origin, hitRadius, dir, out RaycastHit hit, hitRange, creatureLayer))
        {
            var creature = hit.collider.GetComponentInParent<ICapturable>();
            creature?.OnHit();
            if (creature != null)
            {
                SoundFXManager.instance.PlaySoundFX(hitSound, transform, 0.5f);
            }
        }
    }
}