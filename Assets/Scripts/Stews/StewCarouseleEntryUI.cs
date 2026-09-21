using UnityEngine;
using UnityEngine.UI;

/// <summary>One stew in the exit-door carousel - icon, scaled/highlighted when it's the centered/selected one.</summary>
public class StewCarouselEntryUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private GameObject selectedHighlight;
    [SerializeField] private float selectedScale = 1.15f;

    public void Set(StewInstance stew)
    {
        if (icon != null) icon.sprite = stew.icon;
    }

    public void SetSelected(bool selected)
    {
        if (selectedHighlight != null) selectedHighlight.SetActive(selected);
        transform.localScale = Vector3.one * (selected ? selectedScale : 1f);
    }
}