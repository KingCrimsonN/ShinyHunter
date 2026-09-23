/// <summary>
/// Anything the player's stick/seal can hit and capture implements this.
/// Keeps PlayerCapture decoupled from CreatureAI's internals, so the capture
/// method (stick now, seal-throw QTE later per the GDD) can change without
/// touching creature code.
/// </summary>
public interface ICapturable
{
    CreatureData Data { get; }

    /// <summary>This instance's rolled rarity - drives the capture minigame's barrier count/size/movement and this creature's max health. See CaptureMinigameConfig.</summary>
    CreatureData.Rarity Rarity { get; }

    bool IsStunned { get; }

    /// <summary>
    /// Called when the player's weapon connects, dealing damage. Only stuns
    /// once accumulated damage brings health to 0 or below - see
    /// CaptureMinigameConfig.healthPerRarity. damage is computed by the
    /// caller (base weapon damage, scaled by Capture Power's hit-power bonus
    /// if active) - same "compute it externally" convention as TryCapture's
    /// captureChance.
    /// </summary>
    void OnHit(float damage);

    /// <summary>Attempt to capture. Only meaningful while IsStunned. Returns success.</summary>
    bool TryCapture();

    /// <summary>
    /// Attempt to capture, only meaningful while IsStunned. captureChance
    /// (0-1) is computed by whoever's resolving the attempt - currently
    /// CaptureMinigameController, from the wheel minigame's hit ratio.
    /// Returns whether the capture succeeded.
    /// </summary>
    bool TryCapture(float captureChance);

    /// <summary>
    /// A capture attempt (the wheel minigame) has begun. The creature must
    /// stay stunned until TryCapture(float) resolves the attempt, however
    /// long the minigame takes - it must NOT recover on its own timer, or a
    /// result that arrives after the stun would have expired gets discarded.
    /// </summary>
    void StartCapture();
}
