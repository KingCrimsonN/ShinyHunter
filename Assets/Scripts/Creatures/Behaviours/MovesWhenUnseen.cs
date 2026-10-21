using UnityEngine;

/// <summary>
/// A "weeping angel": it only moves while the player is NOT looking at it. While
/// it is on screen and not hidden behind something, it is frozen in place - no
/// moving, no noticing the player, no attacking, and (optionally) even its sprite
/// animation stops. Look away, or let it slip behind a tree, and it carries on
/// where it left off - for an Aggressive species, that means closing in.
///
/// Give the species the Aggressive temperament; this behaviour only decides WHEN
/// it may act. A stunned critter is never frozen, so its stun runs out normally
/// and it can still be captured while you stare at it.
///
/// "Seen" = inside the camera's view (shrunk by View Margin) AND within Watch
/// Distance AND at least one of a few points on its body (centre, top, bottom) has
/// a clear line from the camera - so cover hides it from you as well.
/// </summary>
public class MovesWhenUnseen : CreatureBehaviour
{
    [Tooltip("Farther than this, it counts as unseen even if it is on screen.")]
    [SerializeField] private float watchDistance = 60f;
    [Tooltip("How far inside the screen edges it must be to count as seen (0 = anywhere on screen, 0.1 = not in the outer 10% of each edge). Negative = a little OFF screen still counts.")]
    [SerializeField, Range(-0.2f, 0.4f)] private float viewMargin = 0f;
    [Tooltip("What blocks the player's view of it (terrain, trees, rocks...). The player and the critter itself are always ignored.")]
    [SerializeField] private LayerMask occluders = ~0;
    [Tooltip("Seconds it stays frozen after you look away, before it starts moving.")]
    [SerializeField, Min(0f)] private float unfreezeDelay = 0.1f;
    [Tooltip("Also stop its sprite animation while watched, so it freezes like a statue.")]
    [SerializeField] private bool freezeAnimation = true;

    private CreatureSpriteAnimator spriteAnimator;
    private Camera cachedCamera;
    private bool frozen;
    private float unseenTimer;

    /// <summary>True while the player is looking at it (see class summary).</summary>
    public bool IsSeen { get; private set; }

    public override bool HasControl => frozen;

    protected override void OnBound()
    {
        spriteAnimator = GetComponentInChildren<CreatureSpriteAnimator>();
    }

    private void Update()
    {
        if (Time.deltaTime <= 0f) return; // paused - nothing changes

        IsSeen = CheckSeen();

        // Never hold a stunned or captured critter: its stun must be able to run out.
        var state = AI.CurrentState;
        bool mayFreeze = state != CreatureAI.State.Stunned && state != CreatureAI.State.Captured;

        if (IsSeen && mayFreeze)
        {
            unseenTimer = 0f;
            if (!frozen) Freeze();
        }
        else if (frozen)
        {
            unseenTimer += Time.deltaTime;
            if (!mayFreeze || unseenTimer >= unfreezeDelay) Unfreeze();
        }
    }

    public override void ControlTick()
    {
        AI.HoldStill(); // every frame, so nothing (leftover path, momentum) can nudge it
    }

    private void Freeze()
    {
        frozen = true;
        AI.HoldStill();
        if (freezeAnimation && spriteAnimator != null) spriteAnimator.enabled = false;
    }

    private void Unfreeze()
    {
        frozen = false;
        if (spriteAnimator != null) spriteAnimator.enabled = true;
        AI.ResumeAfterControl(); // picks the chase (or wander) back up from the current state
    }

    private void OnDisable()
    {
        if (spriteAnimator != null) spriteAnimator.enabled = true;
    }

    private bool CheckSeen()
    {
        if (cachedCamera == null) cachedCamera = Camera.main;
        if (cachedCamera == null || AI.BodyCollider == null) return false;

        Bounds bounds = AI.BodyCollider.bounds;
        Vector3 eye = cachedCamera.transform.position;
        if ((bounds.center - eye).sqrMagnitude > watchDistance * watchDistance) return false;

        // Any of these on screen with a clear line = seen.
        Vector3 up = Vector3.up * (bounds.extents.y * 0.8f);
        return Visible(bounds.center, eye) || Visible(bounds.center + up, eye) || Visible(bounds.center - up, eye);
    }

    private bool Visible(Vector3 point, Vector3 eye)
    {
        Vector3 viewport = cachedCamera.WorldToViewportPoint(point);
        if (viewport.z <= 0f) return false; // behind the camera
        if (viewport.x < viewMargin || viewport.x > 1f - viewMargin || viewport.y < viewMargin || viewport.y > 1f - viewMargin) return false;

        Vector3 toPoint = point - eye;
        float distance = toPoint.magnitude;
        foreach (var hit in Physics.RaycastAll(eye, toPoint / distance, distance, occluders, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider == null || hit.collider.transform.IsChildOf(transform)) continue;
            if (hit.collider.CompareTag("Player") || hit.collider.transform.IsChildOf(cachedCamera.transform.root)) continue;
            return false; // something is in the way
        }

        return true;
    }
}
