using UnityEngine;

/// <summary>How a click with the doll would go on the creature being aimed at (VoodooDoll.Classify).</summary>
public enum CaptureTargetState
{
    /// <summary>Nothing aimed at.</summary>
    None,
    /// <summary>Aimed at, but out of reach.</summary>
    TooFar,
    /// <summary>In reach but not stunned yet - this click's stick swing stuns it, then the doll lands on it.</summary>
    NeedsStun,
    /// <summary>In reach and stunned - the doll captures it.</summary>
    Ready,
}

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
/// Aiming uses CreatureTargeting (shared with the stick). The on-screen target
/// marker belongs to the stick (PlayerCapture / TargetLockMarker), not the doll.
///
/// The throw is ballistic (ThrownDoll): at a target, an arc solved to land on
/// it; at nothing, a real physics throw that falls, bounces and lies there.
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

    [Header("Throw")]
    [Tooltip("The thrown doll's visual. A Rigidbody and a sphere collider are added at runtime if it has none - no setup needed.")]
    [SerializeField] private GameObject dollProjectilePrefab;
    [Tooltip("Burst effect spawned where the thrown doll lands on the critter - only on an actual hit.")]
    [SerializeField] private GameObject captureParticlesPrefab;
    [Tooltip("Where the thrown doll starts from. Defaults to this object's position if left unset.")]
    [SerializeField] private Transform throwOrigin;

    [Header("Throw - at a critter (arc solved to land on it)")]
    [Tooltip("How high the arc rises above the straight line to the critter (world units)...")]
    [SerializeField, Min(0.01f)] private float arcHeight = 0.25f;
    [Tooltip("...plus this much per metre of distance, so longer throws arc higher.")]
    [SerializeField, Min(0f)] private float arcHeightPerMeter = 0.04f;
    [Tooltip("Longest a throw may take, whatever the arc says (seconds).")]
    [SerializeField, Min(0.05f)] private float maxFlightTime = 0.7f;

    [Header("Throw - at nothing (plain physics)")]
    [Tooltip("Launch speed along the view direction (m/s).")]
    [SerializeField, Min(0f)] private float missThrowSpeed = 9f;
    [Tooltip("Extra upward speed so it's lobbed, not fired (m/s).")]
    [SerializeField] private float missThrowUpward = 2.5f;

    [Header("Throw - look")]
    [Tooltip("How fast the doll tumbles in the air (degrees per second). Bounces slow it.")]
    [SerializeField] private float tumbleSpeed = 540f;
    [Tooltip("Seconds a missed doll lies around before shrinking away.")]
    [SerializeField, Min(0f)] private float missedDollLifetime = 2.5f;
    [Tooltip("World radius of the doll's physics collider. 0 = from its sprite.")]
    [SerializeField, Min(0f)] private float dollColliderRadius = 0f;

    private PlayerCapture stick;

    private CreatureTarget target;
    private CaptureTargetState targetState;

    public override void OnEquip()
    {
        stick = GetComponentInParent<PlayerCapture>();
        if (stick == null) stick = FindFirstObjectByType<PlayerCapture>();
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
        if (cam == null) return;

        float forgiveness = stick != null ? stick.HitRadius : aimForgiveness;
        if (CreatureTargeting.TryFind(cam.transform, captureRange, forgiveness, out target))
            targetState = Classify(target);
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
        if (swingSound != null && SoundFXManager.instance != null)
            SoundFXManager.instance.PlaySoundFX(swingSound, transform, 0.5f);

        // Snapshot NOW, as local values, not fields - OnHeldUpdate() keeps
        // overwriting target/targetState every frame, and the throw takes a
        // moment to land, so reading the fields at landing time could pick up
        // whatever the player is looking at THEN instead of what they aimed at.
        bool atCritter = targetState == CaptureTargetState.Ready || targetState == CaptureTargetState.NeedsStun;
        CreatureAI critter = atCritter ? target.creature : null;
        Vector3 aimPoint = atCritter ? target.center : Vector3.zero;

        if (dollProjectilePrefab == null)
        {
            // Nothing to throw visually - resolve straight away, as if it landed.
            if (critter != null) TryCaptureOnLanding(critter, aimPoint);
            return;
        }

        Vector3 start = throwOrigin != null ? throwOrigin.position : transform.position;
        var doll = ThrownDoll.Spawn(dollProjectilePrefab, start, dollColliderRadius, PlayerColliders());

        if (critter != null)
        {
            float distance = Vector3.Distance(start, aimPoint);
            float height = arcHeight + arcHeightPerMeter * distance;
            // Time to rise `height` above the line and come back down: T = sqrt(8h / g).
            float flightTime = Mathf.Min(maxFlightTime, Mathf.Sqrt(8f * height / Mathf.Max(0.01f, -Physics.gravity.y)));
            doll.LaunchAt(aimPoint, flightTime, critter, d => TryCaptureOnLanding(critter, d.transform.position), tumbleSpeed, missedDollLifetime);
        }
        else
        {
            Transform cam = Camera.main != null ? Camera.main.transform : transform;
            doll.LaunchFree(cam.forward * missThrowSpeed + Vector3.up * missThrowUpward, tumbleSpeed, missedDollLifetime);
        }
    }

    /// <summary>
    /// The thrown doll reached the critter. Returns true if it "caught" (the doll
    /// disappears); false = it missed (the critter recovered / is gone) and the
    /// doll keeps falling - not consumed, no capture.
    /// </summary>
    private bool TryCaptureOnLanding(ICapturable creature, Vector3 landedAt)
    {
        bool hit = IsAlive(creature) && creature.IsStunned;
        if (!hit) return false;

        if (hitSound != null && SoundFXManager.instance != null)
            SoundFXManager.instance.PlaySoundFX(hitSound, transform, 0.5f);
        if (captureParticlesPrefab != null)
            Instantiate(captureParticlesPrefab, landedAt, Quaternion.identity);

        // Only a capture that actually started costs a doll (e.g. a second doll
        // landing while the wheel is already open must not be eaten for nothing).
        if (CaptureMinigameController.Instance != null && CaptureMinigameController.Instance.BeginCapture(creature))
            RaiseConsumed(); // deliberately last - only after everything above has safely run
        return true;
    }

    /// <summary>The player's own colliders - the doll starts next to them and must not bounce off the player.</summary>
    private Collider[] PlayerColliders()
    {
        var player = GetComponentInParent<FirstPersonController>();
        if (player == null) player = FindFirstObjectByType<FirstPersonController>();
        return player != null ? player.GetComponentsInChildren<Collider>(true) : GetComponentsInParent<Collider>(true);
    }

    /// <summary>ICapturable is an interface, so a destroyed creature isn't C#-null - check the underlying Unity object too.</summary>
    private static bool IsAlive(ICapturable creature)
    {
        if (creature == null) return false;
        return !(creature is Object unityObject) || unityObject != null;
    }
}
