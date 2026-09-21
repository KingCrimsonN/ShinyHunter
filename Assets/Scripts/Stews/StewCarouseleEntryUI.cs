using UnityEngine;
using UnityEngine.UI;

/// <summary>One stew in the exit-door carousel - icon, scaled/highlighted in proportion to how close to the centre (selected slot) it currently is.</summary>
public class StewCarouselEntryUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private GameObject selectedHighlight;
    [SerializeField] private float selectedScale = 1.15f;

    public void Set(StewInstance stew)
    {
        if (icon != null) icon.sprite = stew.icon;
    }

    /// <summary>
    /// 0 = a normal entry off to the side, 1 = the selected one in the
    /// middle; values in between while the carousel is sliding, so the scale
    /// changes smoothly with the shift instead of popping.
    /// </summary>
    public void SetFocus(float focus)
    {
        focus = Mathf.Clamp01(focus);

        if (selectedHighlight != null) selectedHighlight.SetActive(focus > 0.5f);
        transform.localScale = Vector3.one * Mathf.Lerp(1f, selectedScale, focus);
    }

    public void SetSelected(bool selected)
    {
        SetFocus(selected ? 1f : 0f);
    }
}
