using System.Collections;
using UnityEngine;

/// <summary>
/// Consumable capture tool: always throws when used (even with no target,
/// per design - it just "misses"). If a stunned creature was targeted at
/// the moment of throwing, the capture wheel opens and the doll is
/// consumed once the throw lands - regardless of the wheel's eventual
/// outcome (same convention as a Poke Ball). No target / not stunned = the
/// doll simply lands on nothing, not consumed, no capture starts.
/// </summary>
public class VoodooDoll : ToolBehaviour
{
    [SerializeField] private Animator handAnimator;

    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip swingSound;

    [Header("Capture")]
    [SerializeField] private float captureRange = 3f;
    [SerializeField] private LayerMask creatureLayer;

    [Header("Throw Visualization")]
    [Tooltip("Visual-only projectile (no physics/collision needed) shown flying from the player to the target/aim point.")]
    [SerializeField] private GameObject dollProjectilePrefab;
    [Tooltip("Burst effect spawned at the target once the thrown doll 'arrives' - only shown on an actual hit.")]
    [SerializeField] private GameObject captureParticlesPrefab;
    [SerializeField] private float throwDuration = 0.4f;
    [Tooltip("Where the thrown projectile starts from. Defaults to this object's position if left unset.")]
    [SerializeField] private Transform throwOrigin;
    [Tooltip("How far ahead the doll flies when thrown with no valid target, so it still has somewhere to go.")]
    [SerializeField] private float missThrowDistance = 5f;

    private ICapturable targetedCreature;
    private Transform targetedTransform;

    public override void OnEquip()
    {
        Debug.Log("Voodoo doll equipped.");
    }

    public override void Update()
    {
        UpdateTargetedCreature();
    }

    private void UpdateTargetedCreature()
    {
        targetedCreature = null;
        targetedTransform = null;

        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward,
                out RaycastHit hit, captureRange, creatureLayer))
        {
            targetedCreature = hit.collider.GetComponentInParent<ICapturable>();
            if (targetedCreature != null)
                targetedTransform = hit.collider.transform;
        }
    }

    public override void UseTool()
    {
        if (handAnimator != null) handAnimator.SetTrigger("Hit");
        SoundFXManager.instance.PlaySoundFX(swingSound, transform, 0.5f);

        // Snapshot NOW, as local values, not fields - Update() keeps
        // overwriting targetedCreature/targetedTransform every frame, and
        // the throw takes ~throwDuration seconds to land, so reading the
        // fields directly at landing time could pick up whatever the
        // player is looking at THEN instead of what they aimed at when
        // they actually threw.
        ICapturable creatureAtThrowTime = targetedCreature;
        Vector3 aimPoint = targetedTransform != null
            ? targetedTransform.position
            : Camera.main.transform.position + Camera.main.transform.forward * missThrowDistance;

        StartCoroutine(ThrowDollAt(aimPoint, creatureAtThrowTime));
    }

    private IEnumerator ThrowDollAt(Vector3 aimPoint, ICapturable creature)
    {
        Vector3 startPos = throwOrigin != null ? throwOrigin.position : transform.position;
        GameObject projectile = dollProjectilePrefab != null
            ? Instantiate(dollProjectilePrefab, startPos, Quaternion.identity)
            : null;

        float t = 0f;
        while (t < throwDuration)
        {
            t += Time.deltaTime;
            float progress = throwDuration > 0f ? t / throwDuration : 1f;
            if (projectile != null)
                projectile.transform.position = Vector3.Lerp(startPos, aimPoint, progress);
            yield return null;
        }

        if (projectile != null) Destroy(projectile);

        bool hitSomething = creature != null && creature.IsStunned;
        if (!hitSomething) yield break; // missed / nothing there - doll just lands, not consumed, no capture

        SoundFXManager.instance.PlaySoundFX(hitSound, transform, 0.5f);
        if (captureParticlesPrefab != null)
            Instantiate(captureParticlesPrefab, aimPoint, Quaternion.identity);

        CaptureMinigameController.Instance.BeginCapture(creature);
        RaiseConsumed(); // deliberately last - only after everything above has safely run
    }
}