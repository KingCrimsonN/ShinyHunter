using UnityEngine;

/// <summary>
/// Widemouth: lives in the trees. Harmless on the ground (give the species the
/// Passive temperament) - dangerous from above.
///
///  - On its own, now and then instead of an ordinary wander leg, it walks to a
///    free ClimbSpot nearby, climbs up along the trunk, perches and climbs down.
///  - On the ground, when it notices the player it flees to the NEAREST free tree
///    (one the player isn't closer to) and climbs it. No tree in reach = ordinary flee.
///  - Up a tree, if the player is within Pounce Range (and in sight) it leaps at
///    them in a ballistic arc, playing its Attack animation. If the player is
///    still near the landing point when it lands, they take the species' Attack
///    Damage. Then it is on the ground - harmless again, so it flees to a tree.
///  - Hit while up a tree (or mid-leap): it falls straight down, then the hit
///    plays out normally (stunned or fleeing).
///
/// Staying on the bark: every frame it casts a ray at the trunk from outside, at
/// its current height, and sits just off the surface it hits - so it follows the
/// real collider (a terrain tree's capsule, a rock's mesh...). No collider found =
/// the spot's trunk radius. With Stay On Camera Side it also creeps around the
/// trunk to the side facing the camera: a billboard sprite on the far side would
/// cut through the trunk.
/// </summary>
public class Climber : CreatureBehaviour
{
    [Header("Climbing on its own")]
    [Tooltip("Chance per wander leg that it heads for a climb spot instead.")]
    [SerializeField, Range(0f, 1f)] private float climbChance = 0.35f;
    [Tooltip("How far away a climb spot may be when it climbs for fun.")]
    [SerializeField] private float searchRadius = 12f;
    [Tooltip("How close to the bark counts as having arrived at the foot.")]
    [SerializeField] private float arriveDistance = 1.2f;

    [Header("Fleeing to a tree")]
    [Tooltip("How far away a tree may be for it to run there when it notices the player.")]
    [SerializeField] private float refugeSearchRadius = 20f;

    [Header("Climbing")]
    [SerializeField] private float climbSpeed = 2f;
    [SerializeField] private float descendSpeed = 3f;
    [Tooltip("Falling speed when knocked down by a hit.")]
    [SerializeField] private float fallSpeed = 10f;
    [Tooltip("Seconds it stays up there (random in range). It never climbs down while the player is within its detection radius.")]
    [SerializeField] private Vector2 perchTimeRange = new Vector2(4f, 10f);

    [Header("Pounce")]
    [Tooltip("Leaps at the player when they come this close while it is up a tree.")]
    [SerializeField] private float pounceRange = 10f;
    [Tooltip("It must be at least this high up the trunk to leap.")]
    [SerializeField] private float minPounceHeight = 1f;
    [Tooltip("Only leaps if nothing on these layers is between it and the player. Untick everything to skip the check.")]
    [SerializeField] private LayerMask sightBlockers = ~0;
    [Tooltip("Horizontal speed of the leap (units/s) - sets how long it is in the air.")]
    [SerializeField] private float pounceSpeed = 9f;
    [Tooltip("How far the arc rises above the straight line from the tree to the player.")]
    [SerializeField] private float pounceArcHeight = 1.5f;
    [Tooltip("The player takes damage if within this distance of where it lands. Damage = the species' Attack Damage.")]
    [SerializeField] private float pounceHitRadius = 1.5f;
    [Tooltip("Seconds after a leap before it can leap again.")]
    [SerializeField] private float pounceCooldown = 8f;

    [Header("Staying on the bark")]
    [Tooltip("Layers the trunk colliders are on (a Terrain's tree colliders belong to the Terrain's layer).")]
    [SerializeField] private LayerMask surfaceLayers = ~0;
    [Tooltip("Distance kept between the critter's pivot and the bark.")]
    [SerializeField] private float barkGap = 0.1f;
    [Tooltip("Creep around the trunk to the side facing the camera, so the flat sprite never cuts into the trunk.")]
    [SerializeField] private bool stayOnCameraSide = true;
    [Tooltip("Degrees per second it moves around the trunk.")]
    [SerializeField] private float orbitSpeed = 90f;
    [Tooltip("How quickly it settles onto the surface point (higher = snappier).")]
    [SerializeField] private float followSharpness = 15f;

    private enum Phase { None, Approaching, Ascending, Perched, Descending, Falling, Pouncing }
    private Phase phase = Phase.None;

    private ClimbSpot spot;
    /// <summary>True while Approaching because it is fleeing to this tree (not wandering to it).</summary>
    private bool approachingAsRefuge;

    /// <summary>Horizontal direction from the trunk's centre to the critter - which side of the trunk it is on.</summary>
    private Vector3 side;
    private float height;
    private float groundHeight;
    /// <summary>How high the critter's pivot sits above the NavMesh when standing (the agent's Base Offset, in world units).</summary>
    private float pivotLift;
    private float perchTimer;
    private float pounceCooldownTimer;
    /// <summary>After a flee search found no tree, wait this long before searching again (a forest can have thousands of spots).</summary>
    private float refugeRetryTimer;
    private Camera cachedCamera;

    // Pounce / fall flight
    private Vector3 flightStart;
    private Vector3 flightEnd;
    private float flightDuration;
    private float flightTime;

    public override bool HasControl => phase >= Phase.Ascending;

    private void Update()
    {
        if (pounceCooldownTimer > 0f) pounceCooldownTimer -= Time.deltaTime;
        if (refugeRetryTimer > 0f) refugeRetryTimer -= Time.deltaTime;
    }

    // ---------------- Getting to a tree ----------------

    public override bool TryPickWanderTarget(Vector3 center, float radius, out Vector3 target)
    {
        target = default;
        if (phase != Phase.None || Random.value > climbChance) return false;

        var found = FindSpot(searchRadius, nearest: false);
        if (found == null || !found.TryClaim(AI)) return false;

        spot = found;
        phase = Phase.Approaching;
        approachingAsRefuge = false;
        target = spot.GetFoot(transform.position, arriveDistance * 0.5f);
        return true;
    }

    public override bool TryGetFleeTarget(out Vector3 target)
    {
        target = default;
        if (phase >= Phase.Ascending) return false;

        if (phase != Phase.Approaching || spot == null)
        {
            if (refugeRetryTimer > 0f) return false;

            var found = FindSpot(refugeSearchRadius, nearest: true);
            if (found == null || !found.TryClaim(AI))
            {
                refugeRetryTimer = 0.5f;
                return false;
            }

            spot = found;
            phase = Phase.Approaching;
        }

        approachingAsRefuge = true;

        if (HorizontalDistance(transform.position, spot.AxisBase) <= spot.TrunkRadius + arriveDistance)
        {
            StartAscending(); // made it - up we go (CreatureAI's own target call is a no-op now the agent is off)
            return false;
        }

        target = spot.GetFoot(transform.position, arriveDistance * 0.5f);
        return true;
    }

    public override void OnStateEntered(CreatureAI.State state)
    {
        if (phase != Phase.Approaching) return;
        if (state == CreatureAI.State.Wander && !approachingAsRefuge) return;
        if (state == CreatureAI.State.Flee) return; // TryGetFleeTarget keeps steering to the same tree

        // The leg toward the spot ended: start climbing if it got there, otherwise give up.
        if (state == CreatureAI.State.Idle && HorizontalDistance(transform.position, spot.AxisBase) <= spot.TrunkRadius + arriveDistance)
            StartAscending();
        else
            ReleaseSpot();
    }

    public override void OnHit(bool stunned)
    {
        if (phase >= Phase.Ascending && phase != Phase.Falling)
            StartFalling();
    }

    // ---------------- Up the tree ----------------

    public override void ControlTick()
    {
        switch (phase)
        {
            case Phase.Ascending:
                if (TryPounce()) break;
                if (Climb(Mathf.Max(groundHeight, spot.TopHeight), climbSpeed))
                {
                    phase = Phase.Perched;
                    perchTimer = Random.Range(perchTimeRange.x, perchTimeRange.y);
                    AI.PlayAnimation(CreatureAnimState.Idle);
                }
                break;

            case Phase.Perched:
                if (TryPounce()) break;
                Climb(height, 0f); // keep hugging the bark (and creeping to the camera side)

                // Safe up here: it doesn't come down while the player is around.
                if (!AI.IsPlayerWithin(AI.EffectiveDetectionRadius)) perchTimer -= Time.deltaTime;
                if (perchTimer <= 0f)
                {
                    phase = Phase.Descending;
                    PlayClimbAnimation();
                }
                break;

            case Phase.Descending:
                if (Climb(groundHeight, descendSpeed)) Land();
                break;

            case Phase.Falling:
            case Phase.Pouncing:
                if (FlyAlongArc()) FinishFlight();
                break;
        }
    }

    private void StartAscending()
    {
        groundHeight = transform.position.y;
        height = groundHeight;
        pivotLift = UnityEngine.AI.NavMesh.SamplePosition(transform.position, out var navHit, 5f, UnityEngine.AI.NavMesh.AllAreas)
            ? groundHeight - navHit.position.y
            : 0f;

        side = transform.position - spot.AxisBase;
        side.y = 0f;
        side = side.sqrMagnitude > 0.0001f ? side.normalized : Vector3.forward;

        AI.SuspendNavigation();
        phase = Phase.Ascending;
        approachingAsRefuge = false;
        PlayClimbAnimation();
    }

    /// <summary>Moves the height toward targetHeight and the critter onto the bark at that height. Returns true once the height is reached.</summary>
    private bool Climb(float targetHeight, float speed)
    {
        height = Mathf.MoveTowards(height, targetHeight, speed * Time.deltaTime);

        if (stayOnCameraSide)
        {
            if (cachedCamera == null) cachedCamera = Camera.main;
            if (cachedCamera != null)
            {
                Vector3 toCamera = cachedCamera.transform.position - spot.AxisBase;
                toCamera.y = 0f;
                if (toCamera.sqrMagnitude > 0.0001f)
                    side = Vector3.RotateTowards(side, toCamera.normalized, orbitSpeed * Mathf.Deg2Rad * Time.deltaTime, 0f);
            }
        }

        Vector3 goal = SurfacePoint(height);
        float blend = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
        Vector3 position = Vector3.Lerp(transform.position, goal, blend);
        position.y = height; // height is exact - only the sideways settling is smoothed
        transform.position = position;

        return Mathf.Approximately(height, targetHeight);
    }

    /// <summary>The point on this side of the trunk, at height h, just off the bark.</summary>
    private Vector3 SurfacePoint(float h)
    {
        Vector3 axis = spot.AxisBase;
        axis.y = h;

        float radius = spot.TrunkRadius;
        float reach = radius * 2f + 2f;
        float acceptDistance = radius * 1.5f + 0.3f; // a hit farther from the axis than this belongs to something else
        Vector3 origin = axis + side * reach;

        RaycastHit best = default;
        bool found = false;

        var hits = Physics.RaycastAll(origin, -side, reach, surfaceLayers, QueryTriggerInteraction.Ignore);
        foreach (var hit in hits)
        {
            if (hit.collider == null || hit.collider.transform.IsChildOf(transform)) continue;
            if (hit.collider.GetComponentInParent<CreatureAI>() != null || hit.collider.CompareTag("Player")) continue;
            if (hit.normal.y > 0.7f) continue; // the ground, not bark
            if (HorizontalDistance(hit.point, axis) > acceptDistance) continue;
            if (found && hit.distance >= best.distance) continue;

            best = hit;
            found = true;
        }

        if (!found) return axis + side * (radius + barkGap);

        Vector3 outward = best.normal;
        outward.y = 0f;
        outward = outward.sqrMagnitude > 0.0001f ? outward.normalized : side;

        Vector3 point = best.point + outward * barkGap;
        point.y = h;
        return point;
    }

    // ---------------- Pounce / fall ----------------

    /// <summary>Leaps at the player if they're in range and sight and it's high enough and rested. Returns true if it just leapt.</summary>
    private bool TryPounce()
    {
        if (pounceCooldownTimer > 0f || height < groundHeight + minPounceHeight) return false;
        if (CaptureMinigameController.Instance != null && CaptureMinigameController.Instance.IsRunning) return false; // no attacks during the minigame

        var player = AI.Player;
        if (player == null || !AI.IsPlayerWithin(pounceRange) || !CanSee(player)) return false;

        // Land where the player stands (on the NavMesh), at the critter's normal standing height.
        Vector3 landing = UnityEngine.AI.NavMesh.SamplePosition(player.position, out var navHit, 3f, UnityEngine.AI.NavMesh.AllAreas)
            ? navHit.position + Vector3.up * pivotLift
            : player.position;

        BeginFlight(landing, HorizontalDistance(transform.position, landing) / Mathf.Max(0.1f, pounceSpeed));
        phase = Phase.Pouncing;
        pounceCooldownTimer = pounceCooldown;
        if (!AI.PlayAnimation(CreatureAnimState.Attack)) AI.PlayAnimation(CreatureAnimState.Move);
        return true;
    }

    private bool CanSee(Transform player)
    {
        if (sightBlockers.value == 0) return true;

        Vector3 from = AI.BodyCollider != null ? AI.BodyCollider.bounds.center : transform.position;
        Vector3 to = player.position + Vector3.up * 0.8f;

        foreach (var hit in Physics.RaycastAll(from, to - from, Vector3.Distance(from, to), sightBlockers, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider == null || hit.collider.transform.IsChildOf(transform)) continue;
            if (hit.collider.CompareTag("Player") || hit.collider.transform.IsChildOf(player)) continue;
            return false;
        }

        return true;
    }

    /// <summary>Hit up a tree or mid-leap: drops straight down onto the ground below it.</summary>
    private void StartFalling()
    {
        Vector3 below = transform.position;
        Vector3 landing = UnityEngine.AI.NavMesh.SamplePosition(new Vector3(below.x, groundHeight, below.z), out var navHit, 5f, UnityEngine.AI.NavMesh.AllAreas)
            ? navHit.position + Vector3.up * pivotLift
            : new Vector3(below.x, groundHeight, below.z);

        BeginFlight(landing, Mathf.Abs(below.y - landing.y) / Mathf.Max(0.1f, fallSpeed));
        phase = Phase.Falling;
    }

    private void BeginFlight(Vector3 end, float duration)
    {
        flightStart = transform.position;
        flightEnd = end;
        flightDuration = Mathf.Max(0.15f, duration);
        flightTime = 0f;
    }

    /// <summary>A pounce follows a parabola (straight line + an arc on top); a fall drops straight. Returns true on landing.</summary>
    private bool FlyAlongArc()
    {
        flightTime += Time.deltaTime;
        float t = Mathf.Clamp01(flightTime / flightDuration);

        Vector3 position = Vector3.Lerp(flightStart, flightEnd, t);
        if (phase == Phase.Pouncing) position.y += 4f * pounceArcHeight * t * (1f - t);
        transform.position = position;

        return t >= 1f;
    }

    private void FinishFlight()
    {
        if (phase == Phase.Pouncing)
        {
            var player = AI.Player;
            if (player != null && Vector3.Distance(player.position, transform.position) <= pounceHitRadius + pivotLift)
            {
                if (PlayerHealth.Instance != null) PlayerHealth.Instance.TakeDamage(AI.Data.attackDamage);
                if (UIManager.Instance != null) UIManager.Instance.ShowHurtScreen();
            }
        }

        Land();
    }

    private void Land()
    {
        phase = Phase.None;
        ReleaseSpot();
        AI.ResumeNavigation(); // nearest NavMesh point
        AI.ResumeAfterControl(); // whatever state a hit left it in (stunned/fleeing), or idle - it'll notice the player and flee to a tree
    }

    private void PlayClimbAnimation()
    {
        if (!AI.PlayAnimation(CreatureAnimState.Climb)) AI.PlayAnimation(CreatureAnimState.Move);
    }

    // ---------------- Spots ----------------

    /// <summary>A free spot within radius: the nearest one (fleeing - and never one the player is closer to), or a random one (wandering).</summary>
    private ClimbSpot FindSpot(float radius, bool nearest)
    {
        ClimbSpot choice = null;
        float best = float.MaxValue;
        int seen = 0;
        var player = AI.Player;

        foreach (var candidate in ClimbSpot.All)
        {
            if (candidate == null || !candidate.IsFree) continue;

            float distance = HorizontalDistance(transform.position, candidate.AxisBase);
            if (distance > radius) continue;

            if (nearest)
            {
                // Running past the player to reach a tree is no refuge.
                if (player != null && HorizontalDistance(player.position, candidate.AxisBase) < distance) continue;
                if (distance >= best) continue;
                best = distance;
                choice = candidate;
            }
            else
            {
                // A random free spot in range (reservoir pick), not always the nearest one.
                seen++;
                if (Random.Range(0, seen) == 0) choice = candidate;
            }
        }

        return choice;
    }

    private void ReleaseSpot()
    {
        if (spot != null) spot.Release(AI);
        spot = null;
        approachingAsRefuge = false;
        if (phase == Phase.Approaching) phase = Phase.None;
    }

    private void OnDisable()
    {
        ReleaseSpot();
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
