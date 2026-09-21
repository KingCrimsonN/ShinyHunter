using UnityEngine;

/// <summary>What the marker is telling the player about the creature they're aiming at.</summary>
public enum CaptureTargetState
{
    /// <summary>Nothing aimed at - no marker.</summary>
    None,
    /// <summary>Aimed at, but out of reach - red. Step closer.</summary>
    TooFar,
    /// <summary>In reach but not stunned yet - yellow. A click swings the stick at it (stunning it) and throws the doll.</summary>
    NeedsStun,
    /// <summary>In reach and stunned - green, pulsing. A click captures it.</summary>
    Ready,
}

/// <summary>
/// A camera-facing ring drawn around the creature being aimed at, coloured by
/// CaptureTargetState. Built entirely in code (LineRenderer + a built-in
/// sprite shader) so it needs no prefab or scene setup: the voodoo doll
/// creates one while it's held and destroys it on unequip.
///
/// Call Show() every frame the marker should stay up. If a frame passes
/// without one (tool unusable, player frozen, creature gone) it hides itself,
/// so it can never get stuck on screen.
/// </summary>
public class CaptureTargetIndicator : MonoBehaviour
{
    private const int Segments = 48;

    private static readonly Color TooFarColor = new Color(1f, 0.30f, 0.25f, 0.55f);
    private static readonly Color NeedsStunColor = new Color(1f, 0.85f, 0.20f, 0.90f);
    private static readonly Color ReadyColor = new Color(0.35f, 1f, 0.45f, 1f);

    private LineRenderer line;
    private Material material;
    private readonly Vector3[] points = new Vector3[Segments];

    private CreatureAI creature;
    private float bodyRadius;
    private CaptureTargetState state;
    private int lastShownFrame = -1;

    public static CaptureTargetIndicator Create()
    {
        var go = new GameObject("CaptureTargetIndicator");
        return go.AddComponent<CaptureTargetIndicator>();
    }

    private void Awake()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Debug.LogWarning("CaptureTargetIndicator: 'Sprites/Default' shader not found - the capture marker won't be drawn.");
            enabled = false;
            return;
        }

        material = new Material(shader);

        line = gameObject.AddComponent<LineRenderer>();
        line.material = material;
        line.loop = true;
        line.useWorldSpace = true;
        line.positionCount = Segments;
        line.numCornerVertices = 2;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.sortingOrder = 100;
        line.enabled = false;
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
    }

    /// <summary>Keeps the marker on this creature for the current frame.</summary>
    public void Show(CreatureTarget target, CaptureTargetState newState)
    {
        if (!target.IsValid || newState == CaptureTargetState.None)
        {
            Hide();
            return;
        }

        creature = target.creature;
        bodyRadius = target.bodyRadius;
        state = newState;
        lastShownFrame = Time.frameCount;
    }

    public void Hide()
    {
        lastShownFrame = -1;
        if (line != null) line.enabled = false;
    }

    // The ring is placed in LateUpdate, after the player camera and the
    // creature have moved for this frame, so it doesn't trail behind either.
    private void LateUpdate()
    {
        Camera cam = Camera.main;
        bool visible = line != null && cam != null && creature != null
                       && creature.BodyCollider != null && lastShownFrame == Time.frameCount;
        if (line == null) return;

        line.enabled = visible;
        if (!visible) return;

        Vector3 center = creature.BodyCollider.bounds.center;
        float distance = Vector3.Distance(cam.transform.position, center);

        Color color;
        float radius = bodyRadius * 1.1f;
        float width;

        switch (state)
        {
            case CaptureTargetState.Ready:
                color = ReadyColor;
                radius *= 1f + 0.06f * Mathf.Sin(Time.time * 7f); // pulse: "click now"
                width = 0.028f + distance * 0.0075f;
                break;
            case CaptureTargetState.NeedsStun:
                color = NeedsStunColor;
                width = 0.022f + distance * 0.006f;
                break;
            default: // TooFar
                color = TooFarColor;
                width = 0.014f + distance * 0.004f;
                break;
        }

        line.startColor = color;
        line.endColor = color;
        line.widthMultiplier = width;

        // A circle in the camera's plane, so it always reads as a ring.
        Vector3 right = cam.transform.right;
        Vector3 up = cam.transform.up;
        for (int i = 0; i < Segments; i++)
        {
            float angle = (i / (float)Segments) * Mathf.PI * 2f;
            points[i] = center + (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * radius;
        }
        line.SetPositions(points);
    }
}
