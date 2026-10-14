using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>Draggable ingredient entry in the brewing UI's left-hand inventory grid.</summary>
public class BrewIngredientEntryUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private GameObject selection;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text countText;
    [Tooltip("Optional - small rarity icon on the ingredient (hidden for common ones).")]
    [SerializeField] private IngredientRarityBadge rarityBadge;

    [Tooltip("Exactly 5, in ScentType order: Sweet, Fresh, Putrid, Metallic, Marine. Each shows that scent's value - color the boxes in the prefab.")]
    [SerializeField] private TMP_Text[] scentTexts = new TMP_Text[5];

    /// <summary>Shared floating ghost icon, set once by BrewingStationUI.Awake().</summary>
    public static Image DragIcon;

    public ResourceData Resource { get; private set; }

    public void Setup(ResourceData resource, int count)
    {
        Resource = resource;

        if (icon != null) icon.sprite = resource.icon;
        if (nameText != null) nameText.text = resource.resourceName;
        if (countText != null) countText.text = "x" + count;
        if (rarityBadge != null) rarityBadge.Set(resource);

        for (int i = 0; i < scentTexts.Length; i++)
        {
            if (scentTexts[i] != null)
                scentTexts[i].text = resource.GetScent((ScentType)i).ToString();
        }
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

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (selection != null) selection.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (selection != null) selection.SetActive(false);
    }
}