using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The scent radar chart: a filled polygon with one corner per scent, each
/// corner pushed out from the centre in proportion to that scent's value.
///
/// HOW IT WORKS - this is a custom UI Graphic. A UI Image can only draw a
/// sprite, but a Graphic can build its own mesh: Unity calls OnPopulateMesh
/// whenever the graphic is "dirty" and we hand it vertices and triangles
/// (here a "fan": one vertex in the middle, one per axis, and one triangle
/// from the middle to each pair of neighbouring corners). Everything else
/// (canvas, masking, sorting, tinting with the Color field) is the normal UI
/// pipeline, so it behaves like any other Image. The pentagon outline, the
/// coloured spokes and the labels are NOT drawn here - they're ordinary art /
/// text placed behind and around it; this draws only the filled shape.
///
/// Sizing: the chart's centre is the RectTransform's centre and a value at the
/// maximum reaches `radius = min(width, height) / 2 * Radius Scale`. Size the
/// RectTransform to the art's pentagon and use Radius Scale to match the
/// polygon's corners to the outline's corners exactly.
///
/// Angles follow the project convention (CLAUDE.md #7): degrees, clockwise,
/// 0 = top. Axis 0 is at the top, the rest follow clockwise, evenly spaced.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class ScentRadarChart : MaskableGraphic
{
    [Tooltip("Which scent sits at each corner, starting at the TOP and going CLOCKWISE. Defaults to the layout of the art: Sweet, Fresh, Metallic, Marine, Putrid. Any number of axes >= 3 works.")]
    [SerializeField] private ScentType[] axisOrder =
    {
        ScentType.Sweet, ScentType.Fresh, ScentType.Metallic, ScentType.Marine, ScentType.Putrid,
    };

    [Tooltip("The value that fills an axis completely. Set at runtime by SetScents (the stew scent cap), so this only matters as a fallback.")]
    [SerializeField] private float maxValue = 500f;

    [Tooltip("How far a full value reaches, as a fraction of half the rect's smaller side. Tune until the corners meet the outline in your art.")]
    [Range(0.1f, 1.5f)]
    [SerializeField] private float radiusScale = 1f;

    [Tooltip("How quickly the shape eases to new values (higher = snappier). 0 = jumps instantly.")]
    [SerializeField] private float smoothing = 14f;

    [Tooltip("Only used OUTSIDE play mode, so you can see and size the chart in the editor. Fractions 0-1, one per axis, in axis order.")]
    [SerializeField] private float[] editorPreview = { 1f, 0.6f, 0.3f, 0.5f, 0.8f };

    private float[] target = new float[0];  // where each axis is heading, 0-1
    private float[] shown = new float[0];   // where it is now (eased toward target), 0-1

    /// <summary>
    /// Sets the chart from a stew's scents. `scentsByType` is indexed by
    /// (int)ScentType - the same Sweet, Fresh, Putrid, Metallic, Marine order
    /// as StewInstance.scents - NOT by the chart's own axis order; the chart
    /// looks each axis's scent up itself. `max` is the value that fills an
    /// axis (ExpeditionStewManager.ScentMax).
    /// </summary>
    public void SetScents(float[] scentsByType, float max)
    {
        if (max > 0f) maxValue = max;
        EnsureArrays();

        for (int i = 0; i < axisOrder.Length; i++)
        {
            int index = (int)axisOrder[i];
            float value = scentsByType != null && index >= 0 && index < scentsByType.Length ? scentsByType[index] : 0f;
            target[i] = Mathf.Clamp01(value / maxValue);
        }

        if (smoothing <= 0f || !Application.isPlaying || !isActiveAndEnabled)
            System.Array.Copy(target, shown, target.Length);

        SetVerticesDirty();
    }

    /// <summary>All axes to zero (an empty cauldron, no stew).</summary>
    public void Clear()
    {
        SetScents(null, maxValue);
    }

    private void Update()
    {
        if (!Application.isPlaying || smoothing <= 0f) return;
        EnsureArrays();

        // Frame-rate independent ease; unscaled so it still moves while the game is paused (the tablet sets timeScale 0).
        float t = 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);
        bool changed = false;

        for (int i = 0; i < shown.Length; i++)
        {
            if (Mathf.Approximately(shown[i], target[i])) continue;

            shown[i] = Mathf.Abs(shown[i] - target[i]) < 0.0005f ? target[i] : Mathf.Lerp(shown[i], target[i], t);
            changed = true;
        }

        if (changed) SetVerticesDirty();
    }

    private void EnsureArrays()
    {
        int n = axisOrder != null ? axisOrder.Length : 0;
        if (target.Length != n) target = new float[n];
        if (shown.Length != n) shown = new float[n];
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        int n = axisOrder != null ? axisOrder.Length : 0;
        if (n < 3) return;
        EnsureArrays();

        Rect rect = GetPixelAdjustedRect();
        Vector2 center = rect.center;
        float radius = Mathf.Min(rect.width, rect.height) * 0.5f * radiusScale;

        // Vertex 0 = the middle, vertices 1..n = the corners.
        AddVertex(vh, center);

        for (int i = 0; i < n; i++)
        {
            float fraction = Application.isPlaying ? shown[i] : (editorPreview != null && i < editorPreview.Length ? Mathf.Clamp01(editorPreview[i]) : 0f);

            float angle = 2f * Mathf.PI * i / n; // clockwise from the top
            Vector2 direction = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
            AddVertex(vh, center + direction * (radius * fraction));
        }

        // One triangle per pair of neighbouring corners, all sharing the middle vertex.
        for (int i = 0; i < n; i++)
            vh.AddTriangle(0, 1 + i, 1 + (i + 1) % n);
    }

    private void AddVertex(VertexHelper vh, Vector2 position)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.position = position;
        vertex.color = color; // Graphic.color - the tint, so the fill colour is just this component's Color
        vh.AddVert(vertex);
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        EnsureArrays();
        SetVerticesDirty();
    }
#endif
}
