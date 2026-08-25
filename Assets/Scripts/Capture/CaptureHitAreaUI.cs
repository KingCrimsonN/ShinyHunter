using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One arc-shaped hit zone on the capture wheel. Uses Image's built-in
/// Radial360 fill to draw the arc - no custom mesh code needed. Assign a
/// ring/annulus-shaped sprite in the inspector so the fill traces a band
/// around the wheel rather than a pie slice.
///
/// Angle convention used everywhere in this system: degrees, clockwise,
/// 0 = top (12 o'clock). CaptureMinigameController's needle uses the same
/// convention so ContainsAngle() comparisons line up directly.
/// </summary>
[RequireComponent(typeof(Image))]
public class CaptureHitAreaUI : MonoBehaviour
{
    [SerializeField] private Color unhitColor = new Color(1f, 0.3f, 0.3f, 0.9f);
    [SerializeField] private Color hitColor = new Color(0.3f, 1f, 0.4f, 0.9f);

    private Image image;
    private RectTransform rect;

    public float StartAngle { get; private set; }
    public float WidthDegrees { get; private set; }
    public bool IsHit { get; private set; }

    private void Awake()
    {
        image = GetComponent<Image>();
        rect = GetComponent<RectTransform>();

        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Radial360;
        image.fillOrigin = (int)Image.Origin360.Top;
        image.fillClockwise = true;
    }

    /// <summary>Places and sizes this arc on the wheel. Resets hit state.</summary>
    public void SetArc(float startAngle, float widthDegrees)
    {
        StartAngle = startAngle;
        WidthDegrees = widthDegrees;
        IsHit = false;

        // Negated because Unity's positive Z rotation is counter-clockwise,
        // while our angle convention is clockwise-from-top. Flip both signs
        // here and in the needle's rotation together if your art reads backwards.
        rect.localRotation = Quaternion.Euler(0f, 0f, -startAngle);
        image.fillAmount = Mathf.Clamp01(widthDegrees / 360f);
        image.color = unhitColor;
    }

    /// <summary>Is the needle angle (same clockwise-from-top convention) currently inside this arc?</summary>
    public bool ContainsAngle(float angle)
    {
        float delta = ((angle - StartAngle) % 360f + 360f) % 360f;
        return delta <= WidthDegrees;
    }

    public void MarkHit()
    {
        IsHit = true;
        image.color = hitColor;
    }
}