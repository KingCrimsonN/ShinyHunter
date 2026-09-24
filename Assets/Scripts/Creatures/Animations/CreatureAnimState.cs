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
    /// <summary>Only ever played by aggressive creatures (CreatureData.isAggressive) landing an attack - see CreatureAI.State.Attacking. Chasing itself reuses Move.</summary>
    Attack
}