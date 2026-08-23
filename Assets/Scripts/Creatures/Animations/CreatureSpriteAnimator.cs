using UnityEngine;

/// <summary>
/// Steps through the frames of whichever CreatureAnimationClip is currently
/// playing, at that clip's frame rate, and writes to a SpriteRenderer.
/// Purely visual - CreatureAI calls Play() on state transitions and never
/// touches SpriteRenderer directly.
///
/// Lives on the same object as (or a child of) the creature, wherever its
/// SpriteRenderer is.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class CreatureSpriteAnimator : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private CreatureVariantVisuals variantVisuals;

    private CreatureAnimationClip currentClip;
    private int frameIndex;
    private float frameTimer;
    private System.Action onOneShotComplete;

    public CreatureAnimState CurrentState { get; private set; }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>Call once after the creature's rarity is known (e.g. from CreatureAI.Awake).</summary>
    public void Initialize(CreatureVariantVisuals visuals)
    {
        variantVisuals = visuals;
    }

    /// <summary>
    /// Starts playing the named state's clip for this creature's variant.
    /// onComplete fires once, only for non-looping clips, when they finish.
    /// Returns false (and does nothing) if this variant has no clip for that state yet.
    /// </summary>
    public bool Play(CreatureAnimState state, System.Action onComplete = null)
    {
        if (variantVisuals == null) return false;

        var clip = variantVisuals.GetClip(state);
        if (clip == null || clip.frames == null || clip.frames.Length == 0) return false;

        currentClip = clip;
        CurrentState = state;
        frameIndex = 0;
        frameTimer = 0f;
        onOneShotComplete = onComplete;

        spriteRenderer.sprite = currentClip.frames[0];
        return true;
    }

    private void Update()
    {
        if (currentClip == null) return;
        if (currentClip.frames.Length <= 1 && currentClip.loop) return; // static, nothing to animate

        frameTimer += Time.deltaTime;
        float frameDuration = 1f / Mathf.Max(currentClip.frameRate, 0.01f);
        if (frameTimer < frameDuration) return;

        frameTimer -= frameDuration;
        frameIndex++;

        if (frameIndex >= currentClip.frames.Length)
        {
            if (currentClip.loop)
            {
                frameIndex = 0;
                spriteRenderer.sprite = currentClip.frames[frameIndex];
            }
            else
            {
                frameIndex = currentClip.frames.Length - 1;
                spriteRenderer.sprite = currentClip.frames[frameIndex];

                var callback = onOneShotComplete;
                currentClip = null; // stop ticking - clip finished
                callback?.Invoke();
            }
        }
        else
        {
            spriteRenderer.sprite = currentClip.frames[frameIndex];
        }
    }
}