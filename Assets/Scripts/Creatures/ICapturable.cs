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

    /// <summary>
    /// Attempt to capture, only meaningful while IsStunned. captureChance
    /// (0-1) is computed by whoever's resolving the attempt - currently
    /// CaptureMinigameController, from the wheel minigame's hit ratio.
    /// Returns whether the capture succeeded.
    /// </summary>
    bool TryCapture(float captureChance);
    void StartCapture(float captureTime);
}
