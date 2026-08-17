/// <summary>
/// Anything the player's stick/seal can hit and capture implements this.
/// Keeps PlayerCapture decoupled from CreatureAI's internals, so the capture
/// method (stick now, seal-throw QTE later per the GDD) can change without
/// touching creature code.
/// </summary>
public interface ICapturable
{
    CreatureData Data { get; }
    bool IsStunned { get; }

    /// <summary>Called when the player's stick connects. Should stun the creature.</summary>
    void OnHit();

    /// <summary>Attempt to capture. Only meaningful while IsStunned. Returns success.</summary>
    bool TryCapture();
}
