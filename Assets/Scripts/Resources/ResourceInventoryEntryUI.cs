using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>One read-only grid cell in the resource inventory: icon, name, count.</summary>
public class ResourceInventoryEntryUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text countText;

    public void Set(ResourceData data, int count)
    {
        if (icon != null) icon.sprite = data.icon;
        if (nameText != null) nameText.text = data.resourceName;
        if (countText != null) countText.text = "x" + count;
    }
}
