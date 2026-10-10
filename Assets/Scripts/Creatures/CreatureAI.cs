using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Drives a single creature's behaviour: idle/wander naturally, react to the
/// player getting close (flee, or - for aggressive / provoked neutral species -
/// chase and attack instead), go stunned when hit enough to zero its health,
/// and resolve capture attempts.
///
/// Temperament (CreatureData.temperament): Passive flees; Aggressive chases and
/// attacks; Neutral flees like Passive until it is HIT, then turns aggressive
/// (this instance only - `provoked` lives here, never on the shared data) until
/// it loses interest or is stunned.
///
/// Species-specific behaviour is NOT subclassed in: it comes from
/// CreatureBehaviour components on the same prefab (RecklessRunner,
/// PrefersHeights, BurrowedStart, Climber, CreatureAudio...), which this class
/// collects in Awake and consults at a handful of hook points - see
/// CreatureBehaviour for the list. Mix and match them per prefab.
///
/// Ground/Swimming creatures use NavMeshAgent (bake a NavMesh in the scene).
/// All ground movement goes through SetGroundTarget/StopGround, so one
/// behaviour can take over HOW the creature moves (RecklessRunner runs in
/// straight lines instead of pathing). Flying creatures use a simple
/// point-to-point mover so they aren't constrained to the mesh's walkable surface.
///
/// Visuals are delegated to CreatureSpriteAnimator - this script only calls
/// Play(state) on transitions, it never touches the SpriteRenderer.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CreatureAI : MonoBehaviour, ICapturable
{
    /// <summary>Aggressive and Attacking are only ever entered while IsAggressive - see CheckPlayerProximity / OnHit.</summary>
    public enum State { Idle, Wander, Flee, Stunned, Captured, Aggressive, Attacking }

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
    [SerializeField] private GameObject hitParticles;
    [SerializeField] private GameObject shinyParticles;
    [SerializeField] private ParticleSystem playerSpottedParticles;

    [Header("Debug (read-only)")]
    [SerializeField] private State currentState = State.Idle;
    [SerializeField] private CreatureData.Rarity rolledRarity = CreatureData.Rarity.Normal;
    [Tooltip("A Neutral creature that has been hit and is now aggressive. Per instance - never stored on the shared CreatureData.")]
    [SerializeField] private bool provoked;

    /// <summary>
    /// This instance's rolled rarity. Rolled once in Awake and kept locally -
    /// NEVER written back onto the shared CreatureData asset (see CreatureData's
    /// class comment for why).
    /// </summary>
    public CreatureData.Rarity Rarity => rolledRarity;

    public State CurrentState => currentState;

    /// <summary>Chases and attacks right now: an Aggressive species, or a Neutral one that has been provoked by a hit.</summary>
    public bool IsAggressive =>
        data.temperament == CreatureData.Temperament.Aggressive
        || (data.temperament == CreatureData.Temperament.Neutral && provoked);

    private Transform player;
    private NavMeshAgent agent;
    private Vector3 spawnPoint;
    private Vector3 currentFlyTarget;
    private float stateTimer;
    private float stunTimer;

    /// <summary>Where the creature is currently heading on the ground, and how fast - see SetGroundTarget. Kept here (not only in the agent) so a behaviour can do the moving itself.</summary>
    private Vector3 groundTarget;
    private float groundSpeed;
    private bool hasGroundTarget;

    private CreatureBehaviour[] behaviours = new CreatureBehaviour[0];
    /// <summary>The behaviour (if any) that moves this creature on the ground instead of NavMeshAgent pathing - see CreatureBehaviour.OverridesGroundMovement.</summary>
    private CreatureBehaviour groundMover;

    /// <summary>True while a capture minigame is running against this creature - the stun timer is suspended so the creature can't recover before the attempt resolves.</summary>
    private bool captureInProgress;

    /// <summary>Counts down after a hit that damages but doesn't stun (non-aggressive creatures only) - see OnHit / EffectiveFleeSpeed.</summary>
    private float dashTimer;
    private float dashSpeedMultiplier = 1f;

    /// <summary>Counts down after an aggressive creature lands an attack - it can't attack again until this reaches 0, regardless of how close it stays to the player.</summary>
    private float attackCooldownTimer;

    /// <summary>Counts down after an aggressive creature is hit without being stunned - it holds still until it reaches 0. See TickAggressive.</summary>
    private float staggerTimer;
    /// <summary>True while the stagger's "frozen + hit animation" visuals are active, so they're undone exactly once when it ends.</summary>
    private bool staggered;

    /// <summary>Counts down after an aggressive creature recovers from a stun - while above 0 it neither chases nor re-notices the player. See CheckPlayerProximity.</summary>
    private float aggroBlockedTimer;

    /// <summary>True while the player is inside the capture minigame - aggressive creatures must not attack then (the player can't react or even move).</summary>
    private static bool PlayerInCaptureMinigame => CaptureMinigameController.Instance != null && CaptureMinigameController.Instance.IsRunning;

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

    /// <summary>False once captured (it's about to be destroyed), or while a behaviour hides it (e.g. a buried Shroom) - no longer a valid target.</summary>
    public bool IsTargetable
    {
        get
        {
            if (currentState == State.Captured) return false;
            foreach (var b in behaviours)
                if (b != null && b.BlocksTargeting) return false;
            return true;
        }
    }

    // ---------------- For behaviours ----------------

    public NavMeshAgent Agent => agent;
    public Transform Player => player;

    /// <summary>The agent exists, is enabled and stands on the NavMesh - every agent call goes through this check, because behaviours may switch the agent off (climbing).</summary>
    private bool AgentReady => agent != null && agent.enabled && agent.isOnNavMesh;

    public bool IsPlayerWithin(float distance) =>
        player != null && (player.position - transform.position).sqrMagnitude <= distance * distance;

    /// <summary>Plays an animation state; returns false if this variant has no clip for it (so the caller can fall back to another).</summary>
    public bool PlayAnimation(CreatureAnimState state) => animator != null && animator.Play(state);

    private void OnEnable()
    {
        active.Add(this);
    }

    private void OnDisable()
    {
        active.Remove(this);
    }

    /// <summary>Floor for the detection radius - a hated scent subtracting distance must not shrink it to nothing, or the creature would never notice the player at all.</summary>
    private const float MinDetectionRadius = 0.5f;

    /// <summary>
    /// data.detectionRadius, shifted by the stew's scent (favorite adds, hated
    /// subtracts - see scentDistanceAtMax) and scaled by Soothing Power (less than 1 = notices
    /// the player from closer, ONLY for creatures of the stew's affected
    /// family - see ExpeditionStewManager.GetSoothingMultiplier). See
    /// CreatureData.GetScentAffinity for how scent strength is scored.
    /// </summary>
    public float EffectiveDetectionRadius
    {
        get
        {
            if (ExpeditionStewManager.Instance == null) return data.detectionRadius;

            float soothing = ExpeditionStewManager.Instance.GetSoothingMultiplier(data.family);

            // Scent is ADDITIVE now: a favorite scent adds distance, a hated one
            // subtracts it (scentDistanceAtMax units at full strength), instead
            // of the old multiplier that only a hated scent used.
            data.GetScentAffinity(ExpeditionStewManager.Instance.GetScents(), ExpeditionStewManager.Instance.ScentMax, out float lovedScore, out float hatedScore);
            float scentShift = (lovedScore - hatedScore) * data.scentDistanceAtMax;

            return Mathf.Max(MinDetectionRadius, (data.detectionRadius + scentShift) * soothing);
        }
    }

    /// <summary>
    /// data.fleeSpeed scaled by Soothing Power (less than 1 = flees slower,
    /// ONLY for creatures of the stew's affected family) and, while dashTimer
    /// is running, by the rarity-scaled dash multiplier (see OnHit /
    /// CaptureMinigameConfig). Also doubles as the AGGRESSIVE chase speed
    /// (TickAggressive) - one "urgent movement" speed either way, fleeing or
    /// charging, rather than a separate per-species chase-speed field.
    /// Behaviours can scale it per state (CreatureBehaviour.GetSpeedMultiplier).
    /// </summary>
    private float EffectiveFleeSpeed =>
        data.fleeSpeed
        * (ExpeditionStewManager.Instance != null ? ExpeditionStewManager.Instance.GetSoothingMultiplier(data.family) : 1f)
        * (dashTimer > 0f ? dashSpeedMultiplier : 1f)
        * BehaviourSpeedMultiplier(currentState);

    private float EffectiveWanderSpeed => data.wanderSpeed * BehaviourSpeedMultiplier(State.Wander);

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
            shinyParticles.SetActive(rolledRarity != CreatureData.Rarity.Normal);
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

        // Every critter gets sounds; a prefab may already carry a tuned CreatureAudio.
        if (GetComponent<CreatureAudio>() == null) gameObject.AddComponent<CreatureAudio>();

        // Bound AFTER the agent exists, so behaviours can grab it in OnBound.
        behaviours = GetComponents<CreatureBehaviour>();
        foreach (var b in behaviours)
        {
            b.Bind(this);
            if (groundMover == null && b.OverridesGroundMovement && data.movementMode != CreatureMovementMode.Flying)
                groundMover = b;
        }
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

        // Tick independently of state - a dash/cooldown shouldn't pause just
        // because e.g. a stun briefly interrupted the chase.
        if (dashTimer > 0f) dashTimer -= Time.deltaTime;
        if (attackCooldownTimer > 0f) attackCooldownTimer -= Time.deltaTime;
        if (staggerTimer > 0f) staggerTimer -= Time.deltaTime;
        if (aggroBlockedTimer > 0f) aggroBlockedTimer -= Time.deltaTime;

        // A behaviour that has taken over (buried, climbing...) runs INSTEAD of
        // the normal detection and state logic until it lets go.
        var controller = ControllingBehaviour();
        if (controller != null)
        {
            controller.ControlTick();
            return;
        }

        CheckPlayerProximity();

        switch (currentState)
        {
            case State.Idle: TickIdle(); break;
            case State.Wander: TickWander(); break;
            case State.Flee: TickFlee(); break;
            case State.Stunned: TickStunned(); break;
            case State.Aggressive: TickAggressive(); break;
            case State.Attacking: TickAttacking(); break;
        }

        if (groundMover != null && hasGroundTarget && AgentReady)
            groundMover.MoveTowards(groundTarget, groundSpeed);
    }

    private CreatureBehaviour ControllingBehaviour()
    {
        foreach (var b in behaviours)
            if (b != null && b.isActiveAndEnabled && b.HasControl) return b;
        return null;
    }

    private float BehaviourSpeedMultiplier(State state)
    {
        float multiplier = 1f;
        foreach (var b in behaviours)
            if (b != null && b.isActiveAndEnabled) multiplier *= b.GetSpeedMultiplier(state);
        return multiplier;
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

            // An aggressive creature that comes out of a stun loses aggro: it
            // goes idle (not "struggles free and flees" - fleeing makes no
            // sense for a species that never flees) and ignores the player for
            // aggroLossDuration, since the player is right next to it and it
            // would otherwise re-aggro the very next frame.
            if (data.temperament == CreatureData.Temperament.Aggressive)
            {
                aggroBlockedTimer = data.aggroLossDuration;
                if (newState == State.Flee) newState = State.Idle;
            }

            // A provoked Neutral calms down again - from here it flees like a passive one.
            provoked = false;
        }

        // The flinch only lives inside the chase; leaving it (stun, lost
        // interest...) cancels it so it can't leak into a later chase.
        if (newState != State.Aggressive && newState != State.Attacking)
        {
            staggerTimer = 0f;
            staggered = false;
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
                StopGround();
                animator?.Play(CreatureAnimState.Idle);
                break;

            case State.Wander:
                stateTimer = Random.Range(data.wanderIntervalRange.x, data.wanderIntervalRange.y);
                // Behaviours hear about the new leg BEFORE the target is picked, so
                // e.g. RecklessRunner's sprint roll already applies to this leg's speed.
                NotifyStateEntered(newState);
                PickNewWanderTarget();
                animator?.Play(CreatureAnimState.Move);
                return;

            case State.Flee:
                animator?.Play(CreatureAnimState.Flee);
                break;

            case State.Stunned:
                stunTimer = data.stunDuration;
                StopGround();
                animator?.Play(CreatureAnimState.Hit);
                StartCoroutine(WaitAnChangeAnimation(State.Captured, 0.5f));
                break;

            case State.Aggressive:
                animator?.Play(CreatureAnimState.Move); // chasing reuses the normal movement animation - only the attack itself gets its own state
                break;

            case State.Attacking:
                // Damage lands immediately on entering this state, not at the
                // end of the windup - TickAttacking only holds the creature
                // here (stateTimer) so the attack animation has time to play
                // and to act as the "recovery" before it can chase/attack
                // again. This is only ever entered from TickAggressive, which
                // has JUST verified range and cooldown, so no need to re-check here.
                stateTimer = data.attackWindupDuration;
                attackCooldownTimer = data.attackCooldown;
                StopGround();
                animator?.Play(CreatureAnimState.Attack);
                if (PlayerHealth.Instance != null) PlayerHealth.Instance.TakeDamage(data.attackDamage);
                break;
        }

        NotifyStateEntered(newState);
    }

    private void NotifyStateEntered(State state)
    {
        foreach (var b in behaviours)
            if (b != null && b.isActiveAndEnabled) b.OnStateEntered(state);
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

        if (IsAggressive)
        {
            bool isChasingOrAttacking = currentState == State.Aggressive || currentState == State.Attacking;

            // Lost aggro (just recovered from a stun): drop any chase and don't
            // re-notice the player until the timer runs out.
            if (aggroBlockedTimer > 0f)
            {
                if (isChasingOrAttacking) EnterState(State.Idle);
                return;
            }

            if (dist <= EffectiveDetectionRadius && !isChasingOrAttacking)
            {
                playerSpottedParticles?.Play();
                EnterState(State.Aggressive);
            }
            else if (isChasingOrAttacking && dist >= data.fleeDistance)
            {
                // Player got far enough away - loses interest, same threshold a
                // fleeing species uses to feel safe. A provoked Neutral calms down.
                provoked = false;
                EnterState(State.Idle);
            }

            return;
        }

        if (dist <= EffectiveDetectionRadius && currentState != State.Flee)
        {
            playerSpottedParticles?.Play();
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
            MoveTowardsFlyTarget(EffectiveWanderSpeed);

        if (stateTimer <= 0f || ReachedDestination())
            EnterState(State.Idle);
    }

    private void TickFlee()
    {
        Vector3 fleeTarget = transform.position + GetFleeDirection() * data.fleeDistance;

        if (data.movementMode == CreatureMovementMode.Flying)
        {
            currentFlyTarget = fleeTarget + Vector3.up * Random.Range(data.flightHeightMin, data.flightHeightMax);
            MoveTowardsFlyTarget(EffectiveFleeSpeed);
        }
        else
        {
            // Re-aimed and re-speeded every tick (not just on EnterState) so a
            // dash kicking in mid-flee takes effect immediately, and drops back
            // down the instant it ends.
            SetGroundTarget(fleeTarget, EffectiveFleeSpeed, data.fleeDistance);
        }
    }

    private void TickStunned()
    {
        if (captureInProgress) return; // stays stunned until TryCapture resolves the attempt

        stunTimer -= Time.deltaTime;
        if (stunTimer <= 0f) EnterState(State.Idle);
    }

    /// <summary>
    /// Aggressive-only: closes in on the player, attacking once in range and
    /// off cooldown. Mirror image of TickFlee (moves TOWARD instead of
    /// away), re-aiming every frame the same way TickFlee does - see its
    /// comment for why that's the existing convention here.
    ///
    /// Once within attackRange, it HOLDS POSITION rather than continuing to
    /// close the last bit of distance or walking through the player - even
    /// while on cooldown and unable to actually attack yet, it just waits at
    /// range instead of drifting closer.
    /// </summary>
    private void TickAggressive()
    {
        if (player == null) return;

        // Flinching after a hit: frozen in place - no chasing, no attacking -
        // until the stagger runs out, then the chase resumes.
        if (staggerTimer > 0f)
        {
            if (!staggered)
            {
                staggered = true;
                animator?.Play(CreatureAnimState.Hit);
            }
            StopGround(); // flying creatures hold by simply not moving below

            return;
        }
        if (staggered)
        {
            staggered = false;
            animator?.Play(CreatureAnimState.Move);
        }

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;

        if (toPlayer.magnitude <= data.attackRange)
        {
            StopGround(); // flying creatures "hold" simply by not calling MoveTowardsFlyTarget below

            // No attacks while the player is in the capture minigame - it holds
            // position at range and strikes once the minigame is over (the
            // cooldown keeps ticking, so it can hit right away).
            if (attackCooldownTimer <= 0f && !PlayerInCaptureMinigame)
                EnterState(State.Attacking);

            return;
        }

        if (data.movementMode == CreatureMovementMode.Flying)
        {
            // A steady height above the player (was a new random height every
            // frame, which made a chasing flyer jitter up and down).
            currentFlyTarget = player.position + Vector3.up * data.flightHeightMin;
            MoveTowardsFlyTarget(EffectiveFleeSpeed);
        }
        else
        {
            SetGroundTarget(player.position, EffectiveFleeSpeed, data.fleeDistance);
        }
    }

    private void TickAttacking()
    {
        stateTimer -= Time.deltaTime;
        UIManager.Instance.ShowHurtScreen(); // flash the red overlay on the player's screen when hit, same as PlayerHealth.TakeDamage
        if (stateTimer <= 0f) EnterState(State.Aggressive); // recovery over - resume the chase (cooldown keeps it from attacking again immediately)
    }

    /// <summary>Horizontal direction AWAY from the player - shared by TickFlee (the ongoing flee target) and BlinkAway (the dash's instant hop), so both agree on "which way is away".</summary>
    private Vector3 GetFleeDirection()
    {
        Vector3 dir = transform.position - player.position;
        dir.y = 0f;
        return dir.sqrMagnitude > 0.01f ? dir.normalized : Random.insideUnitSphere.normalized;
    }

    /// <summary>
    /// The snappy part of a dash: instantly repositions the creature a short
    /// distance away from the player, before the speed-boost part of the
    /// dash takes over - see OnHit. Ground/swimming creatures use
    /// NavMeshAgent.Warp (a direct transform set would fight the agent's own
    /// tracking) and sample the NavMesh first, so it can't blink off it or
    /// into geometry; flying creatures just move transform.position directly,
    /// same as everywhere else they're moved.
    /// </summary>
    private void BlinkAway(float distance)
    {
        if (distance <= 0f || player == null) return;

        Vector3 target = transform.position + GetFleeDirection() * distance;

        if (data.movementMode == CreatureMovementMode.Flying)
        {
            transform.position = target;
        }
        else if (AgentReady)
        {
            if (NavMesh.SamplePosition(target, out NavMeshHit hit, distance, NavMesh.AllAreas))
                agent.Warp(hit.position);
        }
    }

    private void PickNewWanderTarget()
    {
        Vector3 target = default;
        bool picked = false;

        // A behaviour may choose instead (PrefersHeights, Climber...).
        foreach (var b in behaviours)
        {
            if (b != null && b.isActiveAndEnabled && b.TryPickWanderTarget(spawnPoint, data.wanderRadius, out target))
            {
                picked = true;
                break;
            }
        }

        if (!picked)
        {
            Vector3 randomOffset = Random.insideUnitSphere * data.wanderRadius;
            randomOffset.y = 0f;
            target = spawnPoint + randomOffset;
        }

        if (data.movementMode == CreatureMovementMode.Flying)
        {
            target.y = spawnPoint.y + Random.Range(data.flightHeightMin, data.flightHeightMax);
            currentFlyTarget = target;
        }
        else
        {
            SetGroundTarget(target, EffectiveWanderSpeed, data.wanderRadius);
        }
    }

    // ---------------- Ground movement (the one place the agent is driven) ----------------

    /// <summary>
    /// Heads for target (snapped to the NavMesh within sampleRadius) at speed.
    /// Normally the agent paths there; if a behaviour overrides ground movement
    /// (groundMover) it is handed the target every frame instead.
    /// </summary>
    private void SetGroundTarget(Vector3 target, float speed, float sampleRadius)
    {
        if (!AgentReady) return;
        if (!NavMesh.SamplePosition(target, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas)) return;

        groundTarget = hit.position;
        groundSpeed = speed;
        hasGroundTarget = true;
        agent.speed = speed;

        if (groundMover != null) return; // moved in Update by the behaviour

        agent.isStopped = false;
        agent.SetDestination(hit.position);
    }

    private void StopGround()
    {
        hasGroundTarget = false;
        if (AgentReady) agent.isStopped = true;
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

        if (groundMover != null)
        {
            if (!hasGroundTarget) return true;
            Vector3 to = groundTarget - transform.position;
            to.y = 0f;
            return to.magnitude <= Mathf.Max(agent != null ? agent.stoppingDistance : 0f, 0.4f);
        }

        if (AgentReady)
            return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance;

        return true;
    }

    // ---------------- Hooks for behaviours ----------------

    /// <summary>The ground mover got stuck against the NavMesh edge - a wander leg just ends (a chase/flee keeps re-aiming).</summary>
    public void NotifyMovementBlocked()
    {
        if (currentState == State.Wander) EnterState(State.Idle);
    }

    /// <summary>Ran into something solid (RecklessRunner) - exactly like a hit that stuns: same particles, sound and capture window. Doesn't provoke a Neutral (nobody hit it).</summary>
    public void Bump()
    {
        ApplyHit(CurrentHealth, provokes: false);
    }

    /// <summary>Takes the creature off the NavMesh so a behaviour can move the transform freely (climbing). Undo with ResumeNavigation.</summary>
    public void SuspendNavigation()
    {
        StopGround();
        if (agent != null && agent.enabled) agent.enabled = false;
    }

    /// <summary>Back onto the NavMesh at the nearest point to where the creature is now.</summary>
    public void ResumeNavigation()
    {
        if (agent == null || data.movementMode == CreatureMovementMode.Flying) return;

        Vector3 position = transform.position;
        if (!agent.enabled) agent.enabled = true;
        if (NavMesh.SamplePosition(position, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            agent.Warp(hit.position);
    }

    /// <summary>A behaviour handed control back - re-apply the current state so movement/animation match it again (a stun is kept as it is).</summary>
    public void ResumeAfterControl()
    {
        if (currentState == State.Stunned)
        {
            StopGround();
            return;
        }

        EnterState(currentState == State.Attacking ? State.Aggressive : currentState);
    }

    // ---------------- ICapturable ----------------

    /// <summary>
    /// Applies damage; only stuns once accumulated damage brings health to 0
    /// or below (see CaptureMinigameConfig.healthPerRarity - a Regular takes
    /// 1 base-weapon hit, a Radiant takes 5). Already-stunned/captured or
    /// untargetable creatures ignore further hits.
    /// </summary>
    public void OnHit(float damage)
    {
        ApplyHit(damage, provokes: true);
    }

    private void ApplyHit(float damage, bool provokes)
    {
        if (!IsTargetable || currentState == State.Stunned) return;

        CurrentHealth -= damage;
        if (hitParticles != null) Destroy(Instantiate(hitParticles, transform.position, Quaternion.identity), 0.5f);

        bool stunned = CurrentHealth <= 0f;
        foreach (var b in behaviours)
            if (b != null && b.isActiveAndEnabled) b.OnHit(stunned);

        if (stunned)
        {
            EnterState(State.Stunned);
            return;
        }

        // A Neutral creature that gets hit turns aggressive (this instance only).
        if (provokes && data.temperament == CreatureData.Temperament.Neutral && !provoked)
        {
            provoked = true;
            playerSpottedParticles?.Play();
        }

        // Aggressive creatures don't flee or dash when damaged-but-not-stunned
        // - they flinch (hold still for hitStaggerDuration, see TickAggressive)
        // and then keep pressing the attack. Only non-aggressive (fleeing)
        // species dash.
        if (IsAggressive)
        {
            if (currentState != State.Aggressive && currentState != State.Attacking && aggroBlockedTimer <= 0f)
                EnterState(State.Aggressive);
            staggerTimer = data.hitStaggerDuration; // after EnterState - leaving the chase would clear it
            return;
        }

        if (CaptureMinigameController.Instance != null)
        {
            float duration = CaptureMinigameController.Instance.GetDashDuration(rolledRarity);
            if (duration > 0f)
            {
                dashSpeedMultiplier = CaptureMinigameController.Instance.GetDashSpeedMultiplier(rolledRarity);
                dashTimer = duration;

                // The instant "blink" happens FIRST, right here - immediate,
                // snappy feedback - before the eased speed boost takes over
                // for the rest of the dash's duration.
                BlinkAway(CaptureMinigameController.Instance.GetDashBlinkDistance(rolledRarity));
            }
        }

        // Getting hit is reason enough to flee regardless of prior state -
        // the hit range and detection radius aren't necessarily the same, so
        // this isn't always a no-op.
        if (currentState != State.Flee)
            EnterState(State.Flee);
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
            InventoryManager.Instance.AddCreature(data, rolledRarity, 1);

            if (stunParticles != null) stunParticles.SetActive(false);
            StopGround();

            Destroy(gameObject); // swap for a pool-return call if using pooling
        }
        else
        {
            EnterState(State.Flee); // struggled free
        }

        return success;
    }

    public bool TryCapture(float captureChance, float extraDoubleYieldChance = 0f)
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
            // already get there first. The chance is handed in by whoever
            // resolved the capture (CaptureMinigameController remembers the
            // tool from when the wheel started) - it can't be read from the
            // inventory here, the tool may already have been consumed.
            if (!doubleYield && extraDoubleYieldChance > 0f && Random.value <= extraDoubleYieldChance)
                doubleYield = true;

            InventoryManager.Instance.AddCreature(data, rolledRarity, 1, doubleYield);

            // Chrono Power: grants bonus CURRENT-run time immediately if this
            // capture's family matches - a full no-op unless Chrono Power is
            // active AND matches (see ApplyChronoBonusIfMatching).
            ExpeditionStewManager.Instance?.ApplyChronoBonusIfMatching(data.family);

            if (stunParticles != null) stunParticles.SetActive(false);
            StopGround();
            captureParticles.SetActive(true);
            captureParticles.transform.SetParent(null); // detach so it doesn't move with the creature

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

        if (data.temperament != CreatureData.Temperament.Passive)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, data.attackRange);
        }
    }
}
