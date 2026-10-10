using UnityEngine;

/// <summary>
/// Base for a species-specific trait that plugs into CreatureAI. Add any number
/// of these to a creature prefab next to CreatureAI and they combine: CreatureAI
/// finds them in Awake and asks them at a few fixed points. Every hook has a
/// "do nothing" default, so a behaviour overrides only what it needs.
///
///   HasControl / ControlTick    - take over the creature completely for a while
///                                 (buried, climbing); detection and the normal
///                                 state logic are paused meanwhile
///   BlocksTargeting             - can't be aimed at, hit or captured
///   OverridesGroundMovement /   - move the creature on the ground yourself
///   MoveTowards                   instead of NavMeshAgent pathing (one per creature)
///   TryPickWanderTarget         - choose where the next wander leg goes
///   GetSpeedMultiplier          - scale the creature's speed in a given state
///   OnStateEntered / OnHit      - react to the state machine and to hits
///
/// Traits that are pure DATA (temperament, sounds) live on CreatureData instead.
/// Per-instance runtime values belong on the behaviour itself, never on the
/// shared CreatureData asset (CLAUDE.md #1).
/// </summary>
[RequireComponent(typeof(CreatureAI))]
public abstract class CreatureBehaviour : MonoBehaviour
{
    protected CreatureAI AI { get; private set; }

    /// <summary>Called once by CreatureAI.Awake (after its NavMeshAgent exists) - may run before this component's own Awake, so do setup in OnBound.</summary>
    internal void Bind(CreatureAI ai)
    {
        AI = ai;
        OnBound();
    }

    protected virtual void OnBound() { }

    /// <summary>While true, CreatureAI calls ControlTick every frame instead of its own detection and state ticks.</summary>
    public virtual bool HasControl => false;
    public virtual void ControlTick() { }

    /// <summary>While true the creature can't be aimed at, hit or captured.</summary>
    public virtual bool BlocksTargeting => false;

    /// <summary>True = CreatureAI hands every ground target to MoveTowards (each frame) instead of letting NavMeshAgent path there. Read once, in Awake.</summary>
    public virtual bool OverridesGroundMovement => false;
    /// <summary>Move toward target (already on the NavMesh) at speed, for one frame.</summary>
    public virtual void MoveTowards(Vector3 target, float speed) { }

    /// <summary>Return true with a target to decide where the next wander leg goes (center/radius are the creature's spawn point and wander radius).</summary>
    public virtual bool TryPickWanderTarget(Vector3 center, float radius, out Vector3 target)
    {
        target = default;
        return false;
    }

    public virtual float GetSpeedMultiplier(CreatureAI.State state) => 1f;

    public virtual void OnStateEntered(CreatureAI.State state) { }

    /// <summary>Called on every hit that lands (and on a RecklessRunner bump), before the state reacts. stunned = this hit stunned it.</summary>
    public virtual void OnHit(bool stunned) { }
}
