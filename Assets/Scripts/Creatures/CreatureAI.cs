using System.Collections;
using System.Collections.Generic;
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

    /// <summary>True while a capture minigame is running against this creature - the stun timer is suspended so the creature can't recover before the attempt resolves.</summary>
    private bool captureInProgress;

    private CreatureSpriteAnimator animator;

    public bool IsStunned => currentState == State.Stunned;

    // ---------------- Health (no visual representation - see CaptureMinigameConfig) ----------------

    /// <summary>Lazily initialized (not in Awake) so it doesn't matter whether CaptureMinigameController.Instance exists yet at spawn time - by the time anything can actually hit this creature, the scene has long since finished loading.</summary>
    private float? currentHealth;

    /// <summary>Max health for this instance's rolled rarity - see CaptureMinigameConfig.healthPerRarity. Falls back to 10 if no config is wired up.</summary>
    private float MaxHealth => CaptureMinigameController.Instance != null
        ? CaptureMinigameController.Instance.GetMaxHealthForRarity(rolledRarity)
        : 10f;

    private float CurrentHealth
    {
        get
        {
            if (currentHealth == null) currentHealth = MaxHealth;
            return currentHealth.Value;
        }
        set => currentHealth = value;
    }

    // ---------------- Targeting support (see CreatureTargeting) ----------------

    private static readonly List<CreatureAI> active = new List<CreatureAI>();

    /// <summary>Every enabled creature in the scene - lets CreatureTargeting find candidates without physics queries (whose layer mask / first-hit rules made "looking straight at it" miss).</summary>
    public static IReadOnlyList<CreatureAI> Active => active;

    private Collider bodyCollider;

    /// <summary>The creature's own collider - its bounds are what aiming is measured against.</summary>
    public Collider BodyCollider => bodyCollider;

    /// <summary>False once captured (it's about to be destroyed) - no longer a valid target.</summary>
    public bool IsTargetable => currentState != State.Captured;

    private void OnEnable()
    {
        active.Add(this);
    }

    private void OnDisable()
    {
        active.Remove(this);
    }

    /// <summary>
    /// data.detectionRadius scaled by Soothing Power (less than 1 = notices
    /// the player from closer, ONLY for creatures of the stew's affected
    /// family - see ExpeditionStewManager.GetSoothingMultiplier) AND by how
    /// strongly this species hates the active stew's scent (greater than 1 =
    /// notices/flees from farther away the more it hates the smell - see
    /// CreatureData.GetScentAffinity).
    /// </summary>
    private float EffectiveDetectionRadius
    {
        get
        {
            if (ExpeditionStewManager.Instance == null) return data.detectionRadius;

            float soothing = ExpeditionStewManager.Instance.GetSoothingMultiplier(data.family);

            data.GetScentAffinity(ExpeditionStewManager.Instance.GetScents(), out _, out float hatedScore);
            float aversion = 1f + hatedScore * data.detectionAversionScale;

            return data.detectionRadius * soothing * aversion;
        }
    }

    /// <summary>data.fleeSpeed scaled by Soothing Power (less than 1 = flees slower, ONLY for creatures of the stew's affected family).</summary>
    private float EffectiveFleeSpeed =>
        data.fleeSpeed * (ExpeditionStewManager.Instance != null ? ExpeditionStewManager.Instance.GetSoothingMultiplier(data.family) : 1f);

    private void Awake()
    {
        bodyCollider = GetComponent<Collider>();
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
        // Shiny Power only benefits creatures of the active stew's dominant
        // family (see ExpeditionStewManager.GetRarityChanceMultiplier) - data
        // is guaranteed non-null here, this is only ever called from Awake
        // inside its own "if (data != null)" guard.
        float multiplier = ExpeditionStewManager.Instance != null
            ? ExpeditionStewManager.Instance.GetRarityChanceMultiplier(data.family)
            : 1f;

        float legendary = Mathf.Clamp01(legendaryChance * multiplier);
        float rare = Mathf.Clamp01(rareChance * multiplier);
        float uncommon = Mathf.Clamp01(uncommonChance * multiplier);

        float roll = Random.value;
        if (roll < legendary) return CreatureData.Rarity.Legendary;
        if (roll < legendary + rare) return CreatureData.Rarity.Rare;
        if (roll < legendary + rare + uncommon) return CreatureData.Rarity.Uncommon;
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
        // Stun ending WITHOUT a capture (the stun timer running out because
        // nobody engaged the wheel, or TryCapture's failure branch sending it
        // to Flee) restores health to a FRACTION of base, not a full heal -
        // see CaptureMinigameConfig.failedCaptureHealthRestoreFraction. This
        // has to happen here, reading the OLD currentState, before it's
        // overwritten below - repeated attempts wear the creature down across
        // multiple encounters instead of resetting to full each time.
        if (currentState == State.Stunned && newState != State.Captured)
        {
            float restoreFraction = CaptureMinigameController.Instance != null
                ? CaptureMinigameController.Instance.GetFailedCaptureHealthRestoreFraction()
                : 0.5f;
            CurrentHealth = MaxHealth * restoreFraction;
        }

        currentState = newState;

        // Only the Stunned state can be mid-capture; any other transition
        // (fleeing after a failed attempt, etc.) ends it.
        if (newState != State.Stunned) captureInProgress = false;

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
                if (agent != null) { agent.isStopped = false; agent.speed = EffectiveFleeSpeed; }
                animator?.Play(CreatureAnimState.Flee);
                break;

            case State.Stunned:
                stunTimer = data.stunDuration;
                if (agent != null) agent.isStopped = true;
                animator?.Play(CreatureAnimState.Hit);
                StartCoroutine(WaitAnChangeAnimation(State.Captured, 0.5f));
                break;

                // case State.Captured:
                //     if (agent != null) agent.isStopped = true;
                //     animator?.Play(CreatureAnimState.Captured);
                //     break;
        }
    }

    private IEnumerator WaitAnChangeAnimation(State newState, float delay)
    {
        if (newState == State.Captured)
        {
            yield return new WaitForSeconds(delay);
            animator?.Play(CreatureAnimState.Captured);
        }
        else
            yield return null;

    }

    private void CheckPlayerProximity()
    {
        if (player == null || currentState == State.Stunned) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= EffectiveDetectionRadius && currentState != State.Flee)
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
            MoveTowardsFlyTarget(EffectiveFleeSpeed);
        }
        else if (agent != null)
        {
            if (NavMesh.SamplePosition(fleeTarget, out NavMeshHit hit, data.fleeDistance, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
        }
    }

    private void TickStunned()
    {
        if (captureInProgress) return; // stays stunned until TryCapture resolves the attempt

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

    /// <summary>
    /// Applies damage; only stuns once accumulated damage brings health to 0
    /// or below (see CaptureMinigameConfig.healthPerRarity - a Regular takes
    /// 1 base-weapon hit, a Radiant takes 5). Already-stunned/captured
    /// creatures ignore further hits - there's nothing more for another hit
    /// to do once the capture window is already open.
    /// </summary>
    public void OnHit(float damage)
    {
        if (currentState == State.Captured || currentState == State.Stunned) return;

        CurrentHealth -= damage;
        if (CurrentHealth <= 0f)
            EnterState(State.Stunned);
    }

    public void StartCapture()
    {
        if (currentState != State.Stunned) return;
        captureInProgress = true;
    }

    public bool TryCapture()
    {
        if (currentState != State.Stunned) return false;

        bool success = Random.value <= data.baseCaptureChance;

        if (success)
        {
            // currentState = State.Captured;
            InventoryManager.Instance.AddCreature(data, rolledRarity, 1);

            if (stunParticles != null) stunParticles.SetActive(false);
            if (agent != null) agent.isStopped = true;

            // Play the capture reaction if this variant has one, and only
            // destroy once it finishes. Falls back to destroying immediately
            // if no Captured clip is authored for this rarity yet.
            // bool playingCaptureAnim = animator != null &&
            //     animator.Play(CreatureAnimState.Captured, () => Destroy(gameObject));

            // if (!playingCaptureAnim)
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

            // Ingredient Power now resolves HERE, at capture, not at transform
            // time (see decision log) - one roll per captured unit, tagged
            // straight onto the InventoryManager stack so the sparkle badge
            // (inventory / transform station UI) reflects it immediately,
            // long before the player ever visits the transform table.
            bool doubleYield = ExpeditionStewManager.Instance != null
                && ExpeditionStewManager.Instance.TryRollDoubleIngredients(data.family);

            // A tool can ALSO grant an independent chance at the same flag
            // (ToolData.extraIngredientChance) - either roll succeeding is
            // enough, so only bother rolling this one if the stew didn't
            // already get there first.
            if (!doubleYield)
            {
                var equippedTool = ToolInventoryManager.Instance != null ? ToolInventoryManager.Instance.EquippedSlot?.data : null;
                if (equippedTool != null && Random.value <= equippedTool.extraIngredientChance)
                    doubleYield = true;
            }

            InventoryManager.Instance.AddCreature(data, rolledRarity, 1, doubleYield);

            // Chrono Power: grants bonus CURRENT-run time immediately if this
            // capture's family matches - a full no-op unless Chrono Power is
            // active AND matches (see ApplyChronoBonusIfMatching).
            ExpeditionStewManager.Instance?.ApplyChronoBonusIfMatching(data.family);

            if (stunParticles != null) stunParticles.SetActive(false);
            if (agent != null) agent.isStopped = true;
            captureParticles.SetActive(true);
            captureParticles.transform.SetParent(null); // detach so it doesn't move with the creature

            // Play the capture reaction if this variant has one, and only
            // destroy once it finishes. Falls back to destroying immediately
            // if no Captured clip is authored for this rarity yet.
            // bool playingCaptureAnim = animator != null &&
            //     animator.Play(CreatureAnimState.Captured, () => Destroy(gameObject));

            // if (!playingCaptureAnim)
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