using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Drives a single creature's behaviour: idle/wander naturally, flee when the
/// player gets close, go stunned when hit, and resolve capture attempts.
///
/// Ground/Swimming creatures use NavMeshAgent (bake a NavMesh in the scene).
/// Flying creatures use a simple point-to-point mover so they aren't
/// constrained to the mesh's walkable surface.
///
/// Visuals are delegated to CreatureSpriteAnimator - this script only calls
/// Play(state) on transitions, it never touches the SpriteRenderer.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CreatureAI : MonoBehaviour, ICapturable
{
    public enum State { Idle, Wander, Flee, Stunned, Captured }

    [Header("Config")]
    [SerializeField] private CreatureData data;
    public CreatureData Data => data;

    [Header("Rarity odds (rolled once per instance)")]
    [SerializeField, Range(0f, 1f)] private float legendaryChance = 0.05f;
    [SerializeField, Range(0f, 1f)] private float rareChance = 0.15f;
    [SerializeField, Range(0f, 1f)] private float uncommonChance = 0.35f;

    [Header("Effects")]
    [SerializeField] private GameObject stunParticles;
    [SerializeField] private GameObject captureParticles;

    [Header("Debug (read-only)")]
    [SerializeField] private State currentState = State.Idle;
    [SerializeField] private CreatureData.Rarity rolledRarity = CreatureData.Rarity.Normal;

    /// <summary>
    /// This instance's rolled rarity. Rolled once in Awake and kept locally -
    /// NEVER written back onto the shared CreatureData asset (see CreatureData's
    /// class comment for why).
    /// </summary>
    public CreatureData.Rarity Rarity => rolledRarity;

    private Transform player;
    private NavMeshAgent agent;
    private Vector3 spawnPoint;
    private Vector3 currentFlyTarget;
    private float stateTimer;
    private float stunTimer;

    private CreatureSpriteAnimator animator;

    public bool IsStunned => currentState == State.Stunned;

    private void Awake()
    {
        animator = GetComponent<CreatureSpriteAnimator>();
        spawnPoint = transform.position;
        transform.localScale = data.size;

        if (data != null)
        {
            rolledRarity = RollRarity();
            if (animator != null)
                animator.Initialize(data.GetVariant(rolledRarity));
        }

        if (data.movementMode != CreatureMovementMode.Flying)
        {
            agent = GetComponent<NavMeshAgent>();
            if (agent == null) agent = gameObject.AddComponent<NavMeshAgent>();
            agent.speed = data.wanderSpeed;
        }
        else
        {
            // Flying creatures don't path with NavMeshAgent (see MoveTowardsFlyTarget),
            // but if one happens to be present on the prefab, align its render offset.
            agent = GetComponent<NavMeshAgent>();
            if (agent != null) agent.baseOffset = data.flightHeightMin;
        }

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
        else Debug.LogWarning($"{name}: no GameObject tagged 'Player' found in scene.");
    }

    private CreatureData.Rarity RollRarity()
    {
        float roll = Random.value;
        if (roll < legendaryChance) return CreatureData.Rarity.Legendary;
        if (roll < legendaryChance + rareChance) return CreatureData.Rarity.Rare;
        if (roll < legendaryChance + rareChance + uncommonChance) return CreatureData.Rarity.Uncommon;
        return CreatureData.Rarity.Normal;
    }

    private void Start()
    {
        EnterState(State.Idle);
    }

    private void Update()
    {
        if (currentState == State.Captured) return;

        CheckPlayerProximity();

        switch (currentState)
        {
            case State.Idle: TickIdle(); break;
            case State.Wander: TickWander(); break;
            case State.Flee: TickFlee(); break;
            case State.Stunned: TickStunned(); break;
        }
    }

    // ---------------- State machine ----------------

    private void EnterState(State newState)
    {
        currentState = newState;

        // Stun particles only ever belong to the Stunned state - set this
        // generically here rather than per-case, so any transition OUT of
        // Stunned (Idle, Wander, Flee, Captured) clears it. Previously this
        // only cleared in the Idle case, which left particles running if a
        // failed capture sent the creature straight into Flee.
        if (stunParticles != null)
            stunParticles.SetActive(newState == State.Stunned);

        switch (newState)
        {
            case State.Idle:
                stateTimer = Random.Range(data.idleTimeRange.x, data.idleTimeRange.y);
                if (agent != null) agent.isStopped = true;
                animator?.Play(CreatureAnimState.Idle);
                break;

            case State.Wander:
                stateTimer = Random.Range(data.wanderIntervalRange.x, data.wanderIntervalRange.y);
                if (agent != null) { agent.isStopped = false; agent.speed = data.wanderSpeed; }
                PickNewWanderTarget();
                animator?.Play(CreatureAnimState.Move);
                break;

            case State.Flee:
                if (agent != null) { agent.isStopped = false; agent.speed = data.fleeSpeed; }
                animator?.Play(CreatureAnimState.Flee);
                break;

            case State.Stunned:
                stunTimer = data.stunDuration;
                if (agent != null) agent.isStopped = true;
                animator?.Play(CreatureAnimState.Hit);
                break;
        }
    }

    private void CheckPlayerProximity()
    {
        if (player == null || currentState == State.Stunned) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= data.detectionRadius && currentState != State.Flee)
        {
            EnterState(State.Flee);
        }
        else if (currentState == State.Flee && dist >= data.fleeDistance)
        {
            EnterState(State.Idle);
        }
    }

    private void TickIdle()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f) EnterState(State.Wander);
    }

    private void TickWander()
    {
        stateTimer -= Time.deltaTime;

        if (data.movementMode == CreatureMovementMode.Flying)
            MoveTowardsFlyTarget(data.wanderSpeed);

        if (stateTimer <= 0f || ReachedDestination())
            EnterState(State.Idle);
    }

    private void TickFlee()
    {
        Vector3 fleeDir = (transform.position - player.position);
        fleeDir.y = 0f;
        fleeDir = fleeDir.sqrMagnitude > 0.01f ? fleeDir.normalized : Random.insideUnitSphere.normalized;
        Vector3 fleeTarget = transform.position + fleeDir * data.fleeDistance;

        if (data.movementMode == CreatureMovementMode.Flying)
        {
            currentFlyTarget = fleeTarget + Vector3.up * Random.Range(data.flightHeightMin, data.flightHeightMax);
            MoveTowardsFlyTarget(data.fleeSpeed);
        }
        else if (agent != null)
        {
            if (NavMesh.SamplePosition(fleeTarget, out NavMeshHit hit, data.fleeDistance, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
        }
    }

    private void TickStunned()
    {
        stunTimer -= Time.deltaTime;
        if (stunTimer <= 0f) EnterState(State.Idle);
    }

    private void PickNewWanderTarget()
    {
        Vector3 randomOffset = Random.insideUnitSphere * data.wanderRadius;
        randomOffset.y = 0f;
        Vector3 target = spawnPoint + randomOffset;

        if (data.movementMode == CreatureMovementMode.Flying)
        {
            target.y = spawnPoint.y + Random.Range(data.flightHeightMin, data.flightHeightMax);
            currentFlyTarget = target;
        }
        else if (agent != null)
        {
            if (NavMesh.SamplePosition(target, out NavMeshHit hit, data.wanderRadius, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
        }
    }

    private void MoveTowardsFlyTarget(float speed)
    {
        transform.position = Vector3.MoveTowards(transform.position, currentFlyTarget, speed * Time.deltaTime);

        Vector3 dir = currentFlyTarget - transform.position;
        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 3f);
    }

    private bool ReachedDestination()
    {
        if (data.movementMode == CreatureMovementMode.Flying)
            return Vector3.Distance(transform.position, currentFlyTarget) < 0.3f;

        if (agent != null)
            return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance;

        return true;
    }

    // ---------------- ICapturable ----------------

    public void OnHit()
    {
        if (currentState == State.Captured) return;
        EnterState(State.Stunned);
    }

    public void StartCapture(float captureTime)
    {
        if (currentState != State.Stunned) return;
        stunTimer = captureTime;
    }

    public bool TryCapture()
    {
        if (currentState != State.Stunned) return false;

        bool success = Random.value <= data.baseCaptureChance;

        if (success)
        {
            currentState = State.Captured;
            InventoryManager.Instance.AddCreature(data, rolledRarity, 1);

            if (stunParticles != null) stunParticles.SetActive(false);
            if (agent != null) agent.isStopped = true;

            // Play the capture reaction if this variant has one, and only
            // destroy once it finishes. Falls back to destroying immediately
            // if no Captured clip is authored for this rarity yet.
            bool playingCaptureAnim = animator != null &&
                animator.Play(CreatureAnimState.Captured, () => Destroy(gameObject));

            if (!playingCaptureAnim)
                Destroy(gameObject); // swap for a pool-return call if using pooling
        }
        else
        {
            EnterState(State.Flee); // struggled free
        }

        return success;
    }

    public bool TryCapture(float captureChance)
    {
        if (currentState != State.Stunned) return false;

        bool success = Random.value <= Mathf.Clamp01(captureChance);

        if (success)
        {
            currentState = State.Captured;
            InventoryManager.Instance.AddCreature(data, rolledRarity, 1);

            if (stunParticles != null) stunParticles.SetActive(false);
            if (agent != null) agent.isStopped = true;
            captureParticles.SetActive(true);
            captureParticles.transform.SetParent(null); // detach so it doesn't move with the creature

            // Play the capture reaction if this variant has one, and only
            // destroy once it finishes. Falls back to destroying immediately
            // if no Captured clip is authored for this rarity yet.
            bool playingCaptureAnim = animator != null &&
                animator.Play(CreatureAnimState.Captured, () => Destroy(gameObject));

            if (!playingCaptureAnim)
                Destroy(gameObject); // swap for a pool-return call if using pooling
        }
        else
        {
            EnterState(State.Flee); // struggled free
        }

        return success;
    }

    private void OnDrawGizmosSelected()
    {
        if (data == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, data.detectionRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, data.wanderRadius);
    }
}