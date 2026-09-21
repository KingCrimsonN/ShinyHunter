using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>Draggable ingredient entry in the brewing UI's left-hand inventory grid.</summary>
public class BrewIngredientEntryUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text countText;

    /// <summary>Shared floating ghost icon, set once by BrewingStationUI.Awake().</summary>
    public static Image DragIcon;

    public ResourceData Resource { get; private set; }

    public void Setup(ResourceData resource, int count)
    {
        Resource = resource;

        if (icon != null) icon.sprite = resource.icon;
        if (nameText != null) nameText.text = resource.resourceName;
        if (countText != null) countText.text = "x" + count;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (DragIcon == null || icon == null || icon.sprite == null) return;
        DragIcon.sprite = icon.sprite;
        DragIcon.gameObject.SetActive(true);
        DragIcon.transform.position = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (DragIcon != null && DragIcon.gameObject.activeSelf)
            DragIcon.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (DragIcon != null) DragIcon.gameObject.SetActive(false);
    }
}