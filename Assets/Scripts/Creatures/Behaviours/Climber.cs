using UnityEngine;

/// <summary>
/// Widemouth: climbs trees and other surfaces at ClimbSpots placed in the scene.
/// Now and then, instead of an ordinary wander leg, it walks to the foot of a
/// free ClimbSpot nearby, climbs up along the trunk, perches for a while and
/// climbs back down.
///
/// Staying on the bark: every frame it casts a ray at the trunk from outside, at
/// its current height, and sits just off the surface it hits - so it follows the
/// real collider (a terrain tree's capsule, a rock's mesh...) instead of moving
/// in a straight line through it. No collider found = the spot's trunk radius.
/// With Stay On Camera Side it also creeps around the trunk to the side facing
/// the camera: a billboard sprite on the far side would cut through the trunk.
///
/// - Interrupted on the way there (it notices the player, gets hit...): it just
///   gives up on that climb.
/// - Player comes close while it's up there: climbs down early and reacts as
///   usual (an aggressive Widemouth comes for you) - or stays put, see
///   Descend When Player Near.
/// - Hit while climbing/perched: it falls straight down, then the hit plays out
///   normally (stunned, fleeing or angry).
///
/// It stays targetable while up there, so whether it can be reached is down to
/// how high the spots are and the player's range.
/// </summary>
public class Climber : CreatureBehaviour
{
    [Header("Choosing a spot")]
    [Tooltip("Chance per wander leg that it heads for a climb spot instead.")]
    [SerializeField, Range(0f, 1f)] private float climbChance = 0.35f;
    [Tooltip("How far away a climb spot may be.")]
    [SerializeField] private float searchRadius = 12f;
    [Tooltip("How close to the bark counts as having arrived at the foot.")]
    [SerializeField] private float arriveDistance = 1.2f;

    [Header("Climbing")]
    [SerializeField] private float climbSpeed = 2f;
    [SerializeField] private float descendSpeed = 3f;
    [Tooltip("Falling speed when knocked down by a hit.")]
    [SerializeField] private float fallSpeed = 10f;
    [Tooltip("Seconds it stays up there (random in range).")]
    [SerializeField] private Vector2 perchTimeRange = new Vector2(4f, 10f);
    [Tooltip("Climb down early when the player comes within its detection radius. Off = stays up until its perch time runs out.")]
    [SerializeField] private bool descendWhenPlayerNear = true;

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

    private enum Phase { None, Approaching, Ascending, Perched, Descending, Falling }
    private Phase phase = Phase.None;

    private ClimbSpot spot;
    /// <summary>Horizontal direction from the trunk's centre to the critter - which side of the trunk it is on.</summary>
    private Vector3 side;
    private float height;
    private float groundHeight;
    private float perchTimer;
    private Camera cachedCamera;

    public override bool HasControl => phase >= Phase.Ascending;

    public override bool TryPickWanderTarget(Vector3 center, float radius, out Vector3 target)
    {
        target = default;
        if (phase != Phase.None || Random.value > climbChance) return false;

        var found = FindFreeSpot();
        if (found == null || !found.TryClaim(AI)) return false;

        spot = found;
        phase = Phase.Approaching;
        target = spot.GetFoot(transform.position, arriveDistance * 0.5f);
        return true;
    }

    public override void OnStateEntered(CreatureAI.State state)
    {
        if (phase != Phase.Approaching || state == CreatureAI.State.Wander) return;

        // The wander leg toward the spot ended: start climbing if it got there, otherwise give up.
        if (state == CreatureAI.State.Idle && HorizontalDistance(transform.position, spot.AxisBase) <= spot.TrunkRadius + arriveDistance)
            StartAscending();
        else
            ReleaseSpot();
    }

    public override void OnHit(bool stunned)
    {
        if (phase >= Phase.Ascending && phase != Phase.Falling)
            phase = Phase.Falling;
    }

    public override void ControlTick()
    {
        switch (phase)
        {
            case Phase.Ascending:
                if (Climb(Mathf.Max(groundHeight, spot.TopHeight), climbSpeed))
                {
                    phase = Phase.Perched;
                    perchTimer = Random.Range(perchTimeRange.x, perchTimeRange.y);
                    AI.PlayAnimation(CreatureAnimState.Idle);
                }
                break;

            case Phase.Perched:
                Climb(height, 0f); // keep hugging the bark (and creeping to the camera side)
                perchTimer -= Time.deltaTime;
                if (descendWhenPlayerNear && AI.IsPlayerWithin(AI.EffectiveDetectionRadius)) perchTimer = 0f;
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
                if (Climb(groundHeight, fallSpeed)) Land();
                break;
        }
    }

    private void StartAscending()
    {
        groundHeight = transform.position.y;
        height = groundHeight;

        side = transform.position - spot.AxisBase;
        side.y = 0f;
        side = side.sqrMagnitude > 0.0001f ? side.normalized : Vector3.forward;

        AI.SuspendNavigation();
        phase = Phase.Ascending;
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

    private void Land()
    {
        phase = Phase.None;
        ReleaseSpot();
        AI.ResumeNavigation(); // nearest NavMesh point - right at the foot of the trunk
        AI.ResumeAfterControl(); // whatever state a hit left it in (stunned/fleeing/angry), or idle
    }

    private void PlayClimbAnimation()
    {
        if (!AI.PlayAnimation(CreatureAnimState.Climb)) AI.PlayAnimation(CreatureAnimState.Move);
    }

    private ClimbSpot FindFreeSpot()
    {
        ClimbSpot choice = null;
        int seen = 0;

        // A random free spot in range (reservoir pick), not always the nearest one.
        foreach (var candidate in ClimbSpot.All)
        {
            if (candidate == null || !candidate.IsFree) continue;
            if (HorizontalDistance(transform.position, candidate.AxisBase) > searchRadius) continue;

            seen++;
            if (Random.Range(0, seen) == 0) choice = candidate;
        }

        return choice;
    }

    private void ReleaseSpot()
    {
        if (spot != null) spot.Release(AI);
        spot = null;
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
