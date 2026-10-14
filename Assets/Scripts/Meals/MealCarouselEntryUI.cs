using UnityEngine;
using UnityEngine.UI;

/// <summary>One meal in the exit-door carousel - icon, scaled/highlighted in proportion to how close to the centre (selected slot) it currently is.</summary>
public class MealCarouselEntryUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [Tooltip("Optional - the topping drawn over the icon; hidden when the meal has no modifier.")]
    [SerializeField] private Image toppingIcon;
    [SerializeField] private GameObject selectedHighlight;
    [SerializeField] private float selectedScale = 1.15f;

    public void Set(MealInstance meal)
    {
        // A meal with no icon (e.g. the default meal before one is assigned on
        // the config) keeps the prefab's own sprite instead of blanking it.
        if (icon != null && meal.icon != null) icon.sprite = meal.icon;
        if (toppingIcon != null)
        {
            toppingIcon.sprite = meal.topping;
            toppingIcon.enabled = meal.topping != null;
        }
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
