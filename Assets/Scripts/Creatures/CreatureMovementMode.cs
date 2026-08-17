/// <summary>
/// How a creature moves through the world. Drives which movement logic
/// CreatureAI uses (NavMeshAgent vs custom flight).
/// </summary>
public enum CreatureMovementMode
{
    Ground,
    Flying,
    Swimming
}
