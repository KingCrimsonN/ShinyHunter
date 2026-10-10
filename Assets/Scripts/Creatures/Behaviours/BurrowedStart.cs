using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Shroom: spawns buried, with only the top of its sprite showing. While buried
/// it does nothing and can't be aimed at, hit or captured. When the player comes
/// close enough it hops out of the ground and from then on is a normal walking
/// critter (it will usually notice the player straight away and react).
///
/// "Buried" is a simple Y offset: the NavMeshAgent's Base Offset is lowered by
/// Buried Offset (same units as the agent's own Base Offset field), so the agent
/// stays on the NavMesh where it is and nothing fights over the position.
/// </summary>
public class BurrowedStart : CreatureBehaviour
{
    [Tooltip("How far it sits below its normal height while buried, in the NavMeshAgent's Base Offset units. Tune until only the top of the sprite shows.")]
    [SerializeField] private float buriedOffset = 3f;
    [Tooltip("Player distance that makes it pop out.")]
    [SerializeField] private float revealRadius = 4f;
    [Tooltip("Seconds the hop out of the ground takes.")]
    [SerializeField] private float emergeDuration = 0.5f;
    [Tooltip("How high above its normal height the hop peaks, in Base Offset units.")]
    [SerializeField] private float emergeHopHeight = 1f;
    [Tooltip("Optional - e.g. a dirt burst, played when it pops out.")]
    [SerializeField] private ParticleSystem emergeParticles;
    [SerializeField] private AudioClip[] audioClips;

    private enum Phase { Buried, Emerging, Done }
    private Phase phase = Phase.Buried;

    private NavMeshAgent agent;
    private float restOffset;
    private float emergeTimer;

    public override bool HasControl => phase != Phase.Done;
    public override bool BlocksTargeting => phase != Phase.Done;

    protected override void OnBound()
    {
        agent = AI.Agent;
        if (agent == null)
        {
            Debug.LogWarning($"{name}: BurrowedStart needs a NavMeshAgent (a ground critter) - skipping the buried start.");
            phase = Phase.Done;
            return;
        }

        restOffset = agent.baseOffset;
        agent.baseOffset = restOffset - buriedOffset;
    }

    public override void ControlTick()
    {
        switch (phase)
        {
            case Phase.Buried:
                if (AI.IsPlayerWithin(revealRadius)) BeginEmerge();
                break;

            case Phase.Emerging:
                emergeTimer += Time.deltaTime;
                float t = Mathf.Clamp01(emergeTimer / Mathf.Max(0.01f, emergeDuration));

                // From buried up past the rest height (the hop) and back down onto it.
                float rise = Mathf.Lerp(-buriedOffset, 0f, Mathf.SmoothStep(0f, 1f, t));
                float hop = Mathf.Sin(t * Mathf.PI) * emergeHopHeight;
                agent.baseOffset = restOffset + rise + hop;

                if (t >= 1f) FinishEmerge();
                break;
        }
    }

    private void BeginEmerge()
    {
        phase = Phase.Emerging;
        emergeTimer = 0f;
        if (emergeParticles != null) emergeParticles.Play();
        if (audioClips.Length > 0) SoundFXManager.instance.PlayRandomSoundFX(audioClips, transform, 0.7f);
        if (!AI.PlayAnimation(CreatureAnimState.Emerge)) AI.PlayAnimation(CreatureAnimState.Idle);
    }

    private void FinishEmerge()
    {
        agent.baseOffset = restOffset;
        phase = Phase.Done;
        AI.ResumeAfterControl();
    }
}
