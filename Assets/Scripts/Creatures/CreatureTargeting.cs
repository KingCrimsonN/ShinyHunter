using UnityEngine;

/// <summary>What the player is currently aiming at - see CreatureTargeting.TryFind.</summary>
public struct CreatureTarget
{
    public CreatureAI creature;

    /// <summary>World-space centre of the creature's collider bounds - what the throw flies to and the marker is drawn around.</summary>
    public Vector3 center;

    /// <summary>Rough world-space radius of the creature - sizes the marker.</summary>
    public float bodyRadius;

    /// <summary>Distance from the aim origin to the NEAREST point of the creature's bounds (0 if the origin is inside them). This is the number every range check compares against.</summary>
    public float distance;

    public bool IsValid => creature != null;
}

/// <summary>
/// Single place that decides "which creature is the player aiming at". Used
/// by both the stick (PlayerCapture) and the voodoo doll so they always agree.
///
/// Replaces a thin Physics.Raycast / SphereCast against an "Everything" layer
/// mask, which reported the FIRST collider of any kind - terrain, trees,
/// grass, props - and treated "not a creature" as a miss, so looking straight
/// at a creature could still fail. This works from the creatures themselves
/// (CreatureAI.Active) and an aim cone, so other colliders can't get in the
/// way. It deliberately has no line-of-sight check for the same reason; add
/// one with a dedicated occluder layer mask if creatures behind walls become
/// a problem.
/// </summary>
public static class CreatureTargeting
{
    /// <summary>Never let a tiny creature be harder to hit than this many world units of body radius.</summary>
    private const float MinBodyRadius = 0.25f;

    /// <summary>
    /// Finds the creature closest to the centre of the aim, within
    /// searchRange (measured to the nearest point of its bounds).
    /// aimForgiveness is how many extra world units off-centre still count
    /// as "aiming at it", on top of the creature's own size - so it stays
    /// forgiving at any distance.
    /// </summary>
    public static bool TryFind(Transform aim, float searchRange, float aimForgiveness, out CreatureTarget best)
    {
        best = default;
        if (aim == null) return false;

        Vector3 origin = aim.position;
        Vector3 forward = aim.forward;
        float bestScore = float.MaxValue;

        var creatures = CreatureAI.Active;
        for (int i = 0; i < creatures.Count; i++)
        {
            CreatureAI creature = creatures[i];
            if (creature == null || !creature.IsTargetable || creature.BodyCollider == null) continue;

            Bounds bounds = creature.BodyCollider.bounds;
            float distance = (bounds.ClosestPoint(origin) - origin).magnitude;
            if (distance > searchRange) continue;

            Vector3 toCenter = bounds.center - origin;
            float along = Vector3.Dot(forward, toCenter);
            if (along <= 0f && distance > 0.01f) continue; // behind the player

            // How far the creature's centre sits from the line the player is looking along.
            float offAxis = (toCenter - forward * along).magnitude;

            float bodyRadius = Mathf.Max(MinBodyRadius, Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z) * 0.75f);
            float allowed = bodyRadius + aimForgiveness;
            if (offAxis > allowed) continue;

            // Best = most centred on the crosshair; nearer wins a tie.
            float score = offAxis / allowed + distance * 0.01f;
            if (score >= bestScore) continue;

            bestScore = score;
            best = new CreatureTarget
            {
                creature = creature,
                center = bounds.center,
                bodyRadius = bodyRadius,
                distance = distance,
            };
        }

        return best.IsValid;
    }
}
