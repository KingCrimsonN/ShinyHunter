using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Appy / Snipe: runs around recklessly. Instead of pathing AROUND obstacles it
/// runs in straight lines (wandering, fleeing and chasing alike), often sprints
/// when wandering, and if it runs into something solid fast enough it is
/// stunned - exactly as if it had been hit, capture window included.
///
/// Whether it attacks is the species' temperament (Appy Aggressive, Snipe
/// Passive) - this behaviour only changes how it moves.
///
/// Movement uses NavMeshAgent.Move, so it still can't leave the NavMesh: it
/// slides along edges. "Solid" is found with a short sphere cast ahead against
/// Solid Layers; only steep surfaces count (trunks, rocks, walls - not slopes),
/// and the player and other critters are ignored. If it gets stuck on an edge
/// with nothing solid there, it ends that wander leg (or briefly paths normally
/// while fleeing/chasing).
/// </summary>
public class RecklessRunner : CreatureBehaviour
{
    [Header("Running")]
    [Tooltip("Chance that a wander leg is a sprint instead of a walk.")]
    [SerializeField, Range(0f, 1f)] private float sprintChance = 0.6f;
    [Tooltip("Wander speed multiplier while sprinting.")]
    [SerializeField, Min(1f)] private float sprintSpeedMultiplier = 2.5f;

    [Header("Bumping")]
    [Tooltip("Layers that count as solid (trees, rocks, walls). If the ground is on its own layer, leave it out.")]
    [SerializeField] private LayerMask solidLayers = ~0;
    [Tooltip("Only bumps while moving at least this fast (units/s): walking into a tree does nothing, running into it stuns.")]
    [SerializeField] private float minBumpSpeed = 3f;
    [Tooltip("How far ahead it looks for something solid, on top of this frame's step.")]
    [SerializeField] private float probeDistance = 0.3f;
    [Tooltip("Radius of the look-ahead. 0 = from the critter's collider.")]
    [SerializeField] private float probeRadius = 0f;
    [Tooltip("A surface must be at least this steep (degrees from flat ground) to count - keeps slopes and the ground from stunning it.")]
    [SerializeField, Range(0f, 90f)] private float minWallAngle = 50f;
    [Tooltip("Seconds after it recovers from a stun before it can bump again (so it doesn't re-stun on the same tree).")]
    [SerializeField] private float bumpCooldown = 2f;

    [Header("Getting unstuck")]
    [Tooltip("Seconds of barely moving (pressed against the NavMesh edge) before it counts as stuck.")]
    [SerializeField] private float stuckTime = 0.4f;
    [Tooltip("When stuck while fleeing/chasing, path normally for this long before running straight again.")]
    [SerializeField] private float pathfindFallback = 1.5f;

    private NavMeshAgent agent;
    private Collider ownCollider;

    private bool sprinting;
    private float cooldownTimer;
    private float stuckTimer;
    private float fallbackTimer;

    public override bool OverridesGroundMovement => true;

    protected override void OnBound()
    {
        agent = AI.Agent;
        ownCollider = AI.BodyCollider;
    }

    private void Update()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
        if (fallbackTimer > 0f) fallbackTimer -= Time.deltaTime;
    }

    public override float GetSpeedMultiplier(CreatureAI.State state) =>
        state == CreatureAI.State.Wander && sprinting ? sprintSpeedMultiplier : 1f;

    public override void OnStateEntered(CreatureAI.State state)
    {
        if (state == CreatureAI.State.Wander) sprinting = Random.value < sprintChance;
        if (state == CreatureAI.State.Stunned) cooldownTimer = AI.Data.stunDuration + bumpCooldown;
        stuckTimer = 0f;
    }

    public override void MoveTowards(Vector3 target, float speed)
    {
        if (agent == null) return;

        Vector3 to = target - transform.position;
        to.y = 0f;
        float distance = to.magnitude;
        if (distance < 0.05f) return;

        // Stuck a moment ago: let the agent path around for a bit.
        if (fallbackTimer > 0f)
        {
            agent.isStopped = false;
            agent.speed = speed;
            agent.SetDestination(target);
            return;
        }

        if (agent.hasPath) agent.ResetPath();

        Vector3 direction = to / distance;
        float step = Mathf.Min(speed * Time.deltaTime, distance);

        if (speed >= minBumpSpeed && cooldownTimer <= 0f && HitsSomethingSolid(direction, step))
        {
            AI.Bump();
            return;
        }

        Vector3 before = transform.position;
        agent.Move(direction * step);

        Vector3 moved = transform.position - before;
        moved.y = 0f;
        stuckTimer = moved.magnitude < step * 0.25f ? stuckTimer + Time.deltaTime : 0f;

        if (stuckTimer >= stuckTime)
        {
            stuckTimer = 0f;
            fallbackTimer = pathfindFallback;
            AI.NotifyMovementBlocked();
        }
    }

    private bool HitsSomethingSolid(Vector3 direction, float step)
    {
        if (ownCollider == null) return false;

        Bounds bounds = ownCollider.bounds;
        float radius = probeRadius > 0f ? probeRadius : Mathf.Max(0.05f, Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.8f);

        var hits = Physics.SphereCastAll(bounds.center, radius, direction, step + probeDistance, solidLayers, QueryTriggerInteraction.Ignore);
        foreach (var hit in hits)
        {
            var other = hit.collider;
            if (other == null || other.transform.IsChildOf(transform)) continue;
            if (hit.distance <= 0f && hit.point == Vector3.zero) continue; // already overlapping at the start - not a bump
            if (other.CompareTag("Player")) continue;
            if (other.GetComponentInParent<CreatureAI>() != null) continue;
            if (Vector3.Angle(hit.normal, Vector3.up) < minWallAngle) continue; // ground / slope

            return true;
        }

        return false;
    }
}
