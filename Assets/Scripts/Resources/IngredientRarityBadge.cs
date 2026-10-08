using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Put on the small Image that sits on an ingredient's icon (usually a corner).
/// A view calls Set(resource) and it shows that ingredient's rarity icon - or
/// hides itself for a common ingredient / no ingredient. It only toggles the
/// Image on its own object (never the GameObject), so give it no children.
/// Every ingredient view uses this same component so they can't disagree.
/// </summary>
[RequireComponent(typeof(Image))]
public class IngredientRarityBadge : MonoBehaviour
{
    [SerializeField] private IngredientRarityIcons icons;

    private Image image;

    private Image Image
    {
        get
        {
            if (image == null) image = GetComponent<Image>();
            return image;
        }
    }

    public void Set(ResourceData resource)
    {
        Sprite sprite = resource != null && icons != null ? icons.GetIcon(resource.rarity) : null;

        Image.sprite = sprite;
        Image.enabled = sprite != null;
    }

    public void Clear()
    {
        Image.enabled = false;
    }
}
