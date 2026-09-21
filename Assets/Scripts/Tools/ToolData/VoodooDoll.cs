using System.Collections;
using UnityEngine;

/// <summary>
/// Consumable capture tool: always throws when used (even with no target,
/// per design - it just "misses"). If a stunned creature was targeted at
/// the moment of throwing, the capture wheel opens and the doll is
/// consumed once the throw lands - regardless of the wheel's eventual
/// outcome (same convention as a Poke Ball). No target / not stunned = the
/// doll simply lands on nothing, not consumed, no capture starts.
///
/// A click also swings the stick (PlayerCapture) in the same frame, which is
/// what stuns a creature that isn't stunned yet - so a click on an unstunned
/// creature only works if it's within the STICK's reach too.
///
/// Aiming uses CreatureTargeting (shared with the stick), and while held it
/// draws a CaptureTargetIndicator ring around the aimed-at creature: red = too
/// far, yellow = in reach but not stunned, green = ready to capture.
/// </summary>
public class VoodooDoll : ToolBehaviour
{
    [SerializeField] private Animator handAnimator;

    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip swingSound;

    [Header("Capture")]
    [Tooltip("How far the doll reaches to a STUNNED creature, measured to the nearest point of the creature.")]
    [SerializeField] private float captureRange = 3f;
    [Tooltip("Fallback for how far off the crosshair still counts as aiming at a creature (world units, on top of its own size). Normally the stick's own value is used so both agree.")]
    [SerializeField] private float aimForgiveness = 0.5f;

    [Header("Target Marker")]
    [SerializeField] private bool showTargetMarker = true;
    [Tooltip("Creatures up to this far BEYOND captureRange still get a (red) 'too far' marker, so the range is visible instead of the doll silently doing nothing.")]
    [SerializeField] private float outOfRangeMarkerExtra = 4f;

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

    private PlayerCapture stick;
    private CaptureTargetIndicator indicator;
    private GameObject flyingProjectile;

    private CreatureTarget target;
    private CaptureTargetState targetState;

    public override void OnEquip()
    {
        stick = GetComponentInParent<PlayerCapture>();
        if (stick == null) stick = FindFirstObjectByType<PlayerCapture>();

        if (showTargetMarker && indicator == null)
            indicator = CaptureTargetIndicator.Create();
    }

    public override void OnUnequip()
    {
        DestroyIndicator(); // idempotent - OnUnequip can fire more than once on the same instance
    }

    private void OnDestroy()
    {
        DestroyIndicator();
        if (flyingProjectile != null) Destroy(flyingProjectile); // the throw coroutine dies with this object - don't leave its projectile hanging in the world
    }

    private void DestroyIndicator()
    {
        if (indicator == null) return;

        Destroy(indicator.gameObject);
        indicator = null;
    }

    public override void OnHeldUpdate()
    {
        UpdateTarget();
    }

    private void UpdateTarget()
    {
        target = default;
        targetState = CaptureTargetState.None;

        Camera cam = Camera.main;
        if (cam != null)
        {
            float forgiveness = stick != null ? stick.HitRadius : aimForgiveness;

            if (CreatureTargeting.TryFind(cam.transform, captureRange + outOfRangeMarkerExtra, forgiveness, out target))
                targetState = Classify(target);
        }

        if (indicator != null)
            indicator.Show(target, targetState);
    }

    private CaptureTargetState Classify(CreatureTarget t)
    {
        if (t.creature.IsStunned)
            return t.distance <= captureRange ? CaptureTargetState.Ready : CaptureTargetState.TooFar;

        // Not stunned yet: this click's stick swing has to stun it first, so it
        // must be within the stick's reach as well as the doll's.
        float reach = stick != null ? Mathf.Min(captureRange, stick.HitRange) : captureRange;
        return t.distance <= reach ? CaptureTargetState.NeedsStun : CaptureTargetState.TooFar;
    }

    public override void UseTool()
    {
        if (handAnimator != null) handAnimator.SetTrigger("Hit");
        SoundFXManager.instance.PlaySoundFX(swingSound, transform, 0.5f);

        // Snapshot NOW, as local values, not fields - OnHeldUpdate() keeps
        // overwriting target/targetState every frame, and the throw takes
        // ~throwDuration seconds to land, so reading the fields directly at
        // landing time could pick up whatever the player is looking at THEN
        // instead of what they aimed at when they actually threw.
        ICapturable creatureAtThrowTime = null;
        Vector3 aimPoint;

        if (targetState == CaptureTargetState.Ready || targetState == CaptureTargetState.NeedsStun)
        {
            creatureAtThrowTime = target.creature;
            aimPoint = target.center;
        }
        else
        {
            Transform cam = Camera.main.transform;
            aimPoint = cam.position + cam.forward * missThrowDistance;
        }

        StartCoroutine(ThrowDollAt(aimPoint, creatureAtThrowTime));
    }

    private IEnumerator ThrowDollAt(Vector3 aimPoint, ICapturable creature)
    {
        Vector3 startPos = throwOrigin != null ? throwOrigin.position : transform.position;
        flyingProjectile = dollProjectilePrefab != null
            ? Instantiate(dollProjectilePrefab, startPos, Quaternion.identity)
            : null;

        float t = 0f;
        while (t < throwDuration)
        {
            t += Time.deltaTime;
            float progress = throwDuration > 0f ? t / throwDuration : 1f;
            if (flyingProjectile != null)
                flyingProjectile.transform.position = Vector3.Lerp(startPos, aimPoint, progress);
            yield return null;
        }

        if (flyingProjectile != null) Destroy(flyingProjectile);
        flyingProjectile = null;

        bool hitSomething = IsAlive(creature) && creature.IsStunned;
        if (!hitSomething) yield break; // missed / nothing there / it recovered mid-flight - doll just lands, not consumed, no capture

        SoundFXManager.instance.PlaySoundFX(hitSound, transform, 0.5f);
        if (captureParticlesPrefab != null)
            Instantiate(captureParticlesPrefab, aimPoint, Quaternion.identity);

        // Only a capture that actually started costs a doll (e.g. a second doll
        // landing while the wheel is already open must not be eaten for nothing).
        if (CaptureMinigameController.Instance != null && CaptureMinigameController.Instance.BeginCapture(creature))
            RaiseConsumed(); // deliberately last - only after everything above has safely run
    }

    /// <summary>ICapturable is an interface, so a destroyed creature isn't C#-null - check the underlying Unity object too.</summary>
    private static bool IsAlive(ICapturable creature)
    {
        if (creature == null) return false;
        return !(creature is Object unityObject) || unityObject != null;
    }
}
