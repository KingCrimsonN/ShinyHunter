using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shared visual for a single inventory slot: icon, stack count, keybind
/// label, and a selection highlight. Used directly by the hotbar, and
/// alongside DraggableToolSlot in the popup.
/// </summary>
public class ToolSlotUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text countText;
    // [SerializeField] private TMP_Text keybindText;
    [SerializeField] private GameObject selectedHighlight;

    public bool HasItem { get; private set; }
    public Sprite CurrentIcon => icon != null ? icon.sprite : null;

    public void SetKeybindLabel(string label)
    {
        // if (keybindText != null) keybindText.text = label;
    }

    public void SetEmpty()
    {
        HasItem = false;
        if (icon != null) { icon.sprite = null; icon.enabled = false; }
        if (countText != null) countText.text = string.Empty;
    }

    public void SetItem(ToolData data, int count)
    {
        HasItem = true;
        if (icon != null) { icon.sprite = data.icon; icon.enabled = true; }
        if (countText != null) countText.text = count > 1 ? count.ToString() : string.Empty;
    }

    public void SetSelected(bool selected)
    {
        if (selectedHighlight != null) selectedHighlight.SetActive(selected);
    }
}
