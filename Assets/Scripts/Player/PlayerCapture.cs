using UnityEngine;

/// <summary>
/// Handles the "hit with a stick, then capture" loop.
/// Swing stuns any ICapturable it connects with; capture only succeeds
/// against a currently-stunned creature.
///
/// Aiming goes through CreatureTargeting (the same code the voodoo doll uses),
/// so what the stick can hit and what the doll considers "in reach" agree.
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
    [Tooltip("How far the stick reaches, measured to the nearest point of the creature.")]
    [SerializeField] private float hitRange = 2.5f;
    [Tooltip("How far off the crosshair (world units, on top of the creature's own size) still counts as aiming at it.")]
    [SerializeField] private float hitRadius = 0.5f;
    [SerializeField] private KeyCode hitKey = KeyCode.Mouse0;

    public bool isActive;

    /// <summary>How far the stick reaches - the voodoo doll uses this to tell whether a click will also stun the creature.</summary>
    public float HitRange => hitRange;

    /// <summary>Extra off-centre aim forgiveness, shared with the doll's targeting.</summary>
    public float HitRadius => hitRadius;

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
        Camera cam = playerCamera != null ? playerCamera : Camera.main;

        if (handAnimator != null) handAnimator.SetTrigger("Hit");
        SoundFXManager.instance.PlaySoundFX(swingSound, transform, 0.5f);

        if (cam == null) return;

        if (CreatureTargeting.TryFind(cam.transform, hitRange, hitRadius, out CreatureTarget target))
        {
            target.creature.OnHit();
            SoundFXManager.instance.PlaySoundFX(hitSound, transform, 0.5f);
        }
    }
}
