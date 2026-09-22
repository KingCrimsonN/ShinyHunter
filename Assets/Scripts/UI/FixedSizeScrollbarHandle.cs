using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gives a Scrollbar a handle of constant size that travels the full length of
/// its track, instead of Unity's default "handle length = viewport / content".
///
/// Why it's needed: a ScrollRect writes Scrollbar.size every time the content or
/// viewport changes, and the Scrollbar stretches the handle's anchors to that
/// fraction - so the handle grows and shrinks with how much content there is
/// (and fills the whole track when nothing scrolls). That's wrong for a handle
/// with fixed art (a knob, a sprite that shouldn't stretch).
///
/// How it works: the ScrollRect still drives the scroll POSITION (Scrollbar.value)
/// as normal; this just puts Scrollbar.size back to a fixed value after the
/// ScrollRect has written it. It runs late (execution order 1000, after the
/// ScrollRect's own LateUpdate) so the wrong size is never rendered, and Unity's
/// own drag handling uses the fixed size too, so dragging tracks the cursor
/// correctly. With the handle's anchors driven by Unity (sized 0 in the
/// stretch direction), it slides from the very start to the very end of the
/// track - i.e. exactly the min/max of the Handle Slide Area.
///
/// Setup: the handle's RectTransform should stay stretched (as Unity's default
/// Scrollbar creates it) with 0 size delta along the scroll direction. Set the
/// scrollbar's Handle Slide Area padding to control where the handle stops at
/// each end. Add this next to any Scrollbar - or use Tools > Scrollbars > Add
/// Fixed Handle To All Scrollbars to do every scrollbar in the project.
/// </summary>
[ExecuteAlways]
[DefaultExecutionOrder(1000)]
[RequireComponent(typeof(Scrollbar))]
[AddComponentMenu("UI/Fixed Size Scrollbar Handle")]
public class FixedSizeScrollbarHandle : MonoBehaviour
{
    public enum SizeMode
    {
        /// <summary>Handle is always this many canvas units long, however long the track is. Best for fixed art.</summary>
        Pixels,
        /// <summary>Handle is always this fraction of the track's length.</summary>
        Fraction,
    }

    [SerializeField] private SizeMode mode = SizeMode.Pixels;

    [Tooltip("Handle length along the scroll direction, in canvas units (what the RectTransform inspector shows).")]
    [Min(1f)]
    [SerializeField] private float handleLengthPixels = 40f;

    [Tooltip("Handle length as a fraction of the track. Only used in Fraction mode.")]
    [Range(0.01f, 1f)]
    [SerializeField] private float handleFraction = 0.1f;

    private Scrollbar scrollbar;

    private void OnEnable()
    {
        Apply();
    }

    private void LateUpdate()
    {
        Apply();
    }

    private void OnRectTransformDimensionsChange()
    {
        Apply(); // the track's length changed, so a pixel length maps to a different fraction
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Preview inspector changes immediately in edit mode.
        Apply();
    }
#endif

    private void Apply()
    {
        if (scrollbar == null) scrollbar = GetComponent<Scrollbar>();
        if (scrollbar == null || scrollbar.handleRect == null) return;

        float size = mode == SizeMode.Fraction ? handleFraction : PixelsAsFractionOfTrack();
        if (size < 0f) return; // track not laid out yet - try again next frame

        size = Mathf.Clamp(size, 0.01f, 1f);
        if (!Mathf.Approximately(scrollbar.size, size))
            scrollbar.size = size;
    }

    /// <summary>The configured pixel length as a 0-1 fraction of the track, or -1 if the track has no length yet.</summary>
    private float PixelsAsFractionOfTrack()
    {
        // The track is the handle's parent (the Sliding Area) - the rect the handle travels in.
        var track = scrollbar.handleRect.parent as RectTransform;
        if (track == null) return -1f;

        bool vertical = scrollbar.direction == Scrollbar.Direction.BottomToTop
                        || scrollbar.direction == Scrollbar.Direction.TopToBottom;
        float trackLength = vertical ? track.rect.height : track.rect.width;

        return trackLength > 0.01f ? handleLengthPixels / trackLength : -1f;
    }
}
