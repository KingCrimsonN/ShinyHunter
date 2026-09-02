using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>One read-only grid cell in the resource inventory: icon, name, count.</summary>
public class ResourceInventoryEntryUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text countText;
    public TMP_Text descriptionText;

    private string description;

    public void Set(ResourceData data, int count)
    {
        if (icon != null) icon.sprite = data.icon;
        if (nameText != null) nameText.text = data.resourceName;
        if (countText != null) countText.text = "x" + count;
        description = data.description;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (descriptionText != null)
        {
            descriptionText.text = description;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (descriptionText != null)
        {
            descriptionText.text = "";
        }
    }
}
