/// <summary>
/// Named animation states a creature can play. Add new entries here as more
/// animations come in (e.g. Notice, ItemUnused, ItemUsed) - nothing else in
/// the animation system needs to change structurally when you do.
/// </summary>
public enum CreatureAnimState
{
    Idle,
    Move,
    Flee,
    Hit,
    Captured,
    /// <summary>Only ever played by aggressive creatures (CreatureData.temperament) landing an attack - see CreatureAI.State.Attacking. Chasing plays Angry (or Move).</summary>
    Attack,
    /// <summary>BurrowedStart: popping out of the ground. Falls back to Idle if not authored.</summary>
    Emerge,
    /// <summary>Climber: going up/down a ClimbSpot. Falls back to Move if not authored.</summary>
    Climb,
    /// <summary>While aggressive (chasing / waiting to strike), e.g. the Bee's angry flight. Falls back to Move if not authored.</summary>
    Angry,
    /// <summary>Climber: resting up a tree. Falls back to Idle if not authored (Idle is then the GROUND idle).</summary>
    Perch
}