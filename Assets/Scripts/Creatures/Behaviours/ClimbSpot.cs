using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A tree/rock a Climber critter (Widemouth) can climb. Place it at the CENTRE
/// of the trunk at ground level - not in front of it. The critter walks to the
/// trunk's foot from whichever side it comes from, then climbs along the trunk's
/// collider (see Climber), so the spot only needs the trunk's rough radius (used
/// where no collider is found) and how high to climb. One critter at a time.
///
/// Terrain trees can't carry components, so Tools > ShinyHunt > Generate Climb
/// Spots creates these from a terrain's trees automatically.
/// </summary>
public class ClimbSpot : MonoBehaviour
{
    [Tooltip("The trunk's radius. The climber follows the real collider wherever it finds one - this is the fallback, and where it stands at the foot.")]
    [SerializeField, Min(0.05f)] private float trunkRadius = 0.4f;
    [Tooltip("How high above this point (the trunk's base) the critter perches.")]
    [SerializeField, Min(0.5f)] private float climbHeight = 3f;

    private static readonly List<ClimbSpot> all = new List<ClimbSpot>();
    public static IReadOnlyList<ClimbSpot> All => all;

    /// <summary>The critter currently using this spot (runtime only).</summary>
    public CreatureAI Occupant { get; private set; }
    public bool IsFree => Occupant == null;

    /// <summary>The centre of the trunk at ground level.</summary>
    public Vector3 AxisBase => transform.position;
    public float TrunkRadius => trunkRadius;
    public float ClimbHeight => climbHeight;
    public float TopHeight => transform.position.y + climbHeight;

    private void OnEnable() => all.Add(this);
    private void OnDisable() => all.Remove(this);

    /// <summary>Used by the generator.</summary>
    public void Configure(float radius, float height)
    {
        trunkRadius = Mathf.Max(0.05f, radius);
        climbHeight = Mathf.Max(0.5f, height);
    }

    /// <summary>A point just outside the trunk on the side facing `from` - where a critter coming from there stands before climbing.</summary>
    public Vector3 GetFoot(Vector3 from, float distanceFromBark)
    {
        Vector3 side = from - AxisBase;
        side.y = 0f;
        side = side.sqrMagnitude > 0.0001f ? side.normalized : transform.forward;
        return AxisBase + side * (trunkRadius + distanceFromBark);
    }

    public bool TryClaim(CreatureAI creature)
    {
        if (Occupant != null && Occupant != creature) return false;
        Occupant = creature;
        return true;
    }

    public void Release(CreatureAI creature)
    {
        if (Occupant == creature) Occupant = null;
    }

    // Selected only - a generated forest can have thousands of these. Selecting
    // the generated parent object shows all of them.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.9f, 0.4f);
        Vector3 top = AxisBase + Vector3.up * climbHeight;
        DrawCircle(AxisBase, trunkRadius);
        DrawCircle(top, trunkRadius);
        Gizmos.DrawLine(AxisBase, top);
    }

    private static void DrawCircle(Vector3 centre, float radius)
    {
        const int segments = 16;
        Vector3 previous = centre + Vector3.forward * radius;
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector3 next = centre + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * radius;
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }
}
