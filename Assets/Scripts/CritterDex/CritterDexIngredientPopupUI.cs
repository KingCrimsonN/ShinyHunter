using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Small pop-up showing one ingredient's icon (with its rarity icon), name,
/// description and its five scent stats. Used by the critter dex (clicking the
/// ingredient box - see CritterDexDetailUI, closed on a page turn) AND by the
/// transform table (clicking a critter - see CreatureTransformStationUI); each
/// scene has its own instance. Closed by its own close button. (Kept under its
/// original name so existing scene references don't break.)
///
/// Two ways to show it: Show(resource) leaves it wherever it sits in the scene
/// (the dex); ShowAt(resource, tile) puts it next to a tile on screen (the
/// transform table) and ties it to that tile - it closes if the tile moves
/// (the view was scrolled), is destroyed, or - when SetCloseOnOutsideClick is
/// on - the player clicks anywhere outside the pop-up.
///
/// Put this component on the pop-up's root object and leave it INACTIVE in the
/// scene (or assign a different Popup Root) - Hide() deactivates that root.
/// </summary>
public class CritterDexIngredientPopupUI : MonoBehaviour
{
    [Tooltip("The object toggled on/off. Defaults to this component's own GameObject.")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Button closeButton;
    [Tooltip("Optional - the ingredient's icon.")]
    [SerializeField] private Image icon;
    [Tooltip("Optional - small rarity icon on the ingredient (hidden for common ones). Usually sits on the icon's corner.")]
    [SerializeField] private IngredientRarityBadge rarityBadge;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [Tooltip("Exactly 5, in ScentType order: Sweet, Fresh, Putrid, Metallic, Marine. Each shows that scent's value - color the boxes in the prefab.")]
    [SerializeField] private TMP_Text[] scentTexts = new TMP_Text[5];

    [Tooltip("Only used by ShowAt: gap, in canvas units, between the tile and the pop-up beside it.")]
    [SerializeField] private float anchorGap = 12f;

    private GameObject Root => popupRoot != null ? popupRoot : gameObject;

    // ShowAt state: the tile this pop-up is attached to, and where it was when shown.
    private RectTransform anchor;
    private Vector3 anchorStartPosition;
    private bool anchored;
    private bool closeOnOutsideClick;

    public bool IsOpen => Root.activeSelf;

    private void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
    }

    /// <summary>Turn on to close the pop-up on any mouse click outside it. Off by default, so existing uses (the dex) are unchanged; the transform table switches it on.</summary>
    public void SetCloseOnOutsideClick(bool value) => closeOnOutsideClick = value;

    private void Update()
    {
        if (!IsOpen) return;

        // Attached to a tile: close when it scrolls away, is rebuilt, or is hidden.
        if (anchored)
        {
            if (anchor == null || !anchor.gameObject.activeInHierarchy || (anchor.position - anchorStartPosition).sqrMagnitude > 0.25f)
            {
                Hide();
                return;
            }
        }

        // Compared on mouse DOWN: a tile is clicked on mouse UP, so clicking
        // another tile first closes this pop-up and then opens that tile's.
        if (closeOnOutsideClick && (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) && !PointerInsidePopup())
            Hide();
    }

    private bool PointerInsidePopup()
    {
        var rect = Root.transform as RectTransform;
        if (rect == null) return true;

        var canvas = rect.GetComponentInParent<Canvas>();
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        return RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, cam);
    }

    /// <summary>Shows the pop-up beside `tile` (see PositionBeside) and attaches it to that tile.</summary>
    public void ShowAt(ResourceData resource, RectTransform tile)
    {
        Show(resource);
        if (resource == null || tile == null) return;

        PositionBeside(tile);
        anchor = tile;
        anchorStartPosition = tile.position;
        anchored = true;
    }

    /// <summary>
    /// Puts the pop-up just to the right of the tile (top edges aligned), or to
    /// its left if there's no room, and keeps it inside the screen. BESIDE
    /// rather than on top so it never covers the tile - the player still needs
    /// to double-click / drag it.
    /// </summary>
    private void PositionBeside(RectTransform tile)
    {
        var rect = Root.transform as RectTransform;
        var canvas = rect != null ? rect.GetComponentInParent<Canvas>() : null;
        if (rect == null || canvas == null) return;

        // The text was just replaced - size the pop-up for it before measuring.
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);

        var screen = new Vector3[4];
        var tileCorners = new Vector3[4];
        var popupCorners = new Vector3[4];
        ((RectTransform)canvas.rootCanvas.transform).GetWorldCorners(screen);
        tile.GetWorldCorners(tileCorners);
        rect.GetWorldCorners(popupCorners);

        // Corners: [0] bottom-left, [1] top-left, [2] top-right, [3] bottom-right.
        float width = popupCorners[2].x - popupCorners[0].x;
        float height = popupCorners[2].y - popupCorners[0].y;
        float gap = anchorGap * rect.lossyScale.x;

        float left = tileCorners[2].x + gap;                                   // right of the tile
        if (left + width > screen[2].x) left = tileCorners[0].x - gap - width; // no room: left of it
        left = Mathf.Clamp(left, screen[0].x, Mathf.Max(screen[0].x, screen[2].x - width));

        float top = Mathf.Clamp(tileCorners[1].y, screen[0].y + height, screen[2].y); // top-aligned, kept on screen

        rect.position += new Vector3(left - popupCorners[0].x, (top - height) - popupCorners[0].y, 0f);
    }

    public void Show(ResourceData resource)
    {
        if (resource == null)
        {
            Hide();
            return;
        }

        anchor = null; // a plain Show isn't attached to anything - ShowAt sets it after
        anchored = false;

        if (icon != null)
        {
            icon.sprite = resource.icon;
            icon.enabled = resource.icon != null;
        }
        if (rarityBadge != null) rarityBadge.Set(resource);
        if (nameText != null) nameText.text = resource.resourceName;
        if (descriptionText != null) descriptionText.text = resource.description;

        if (scentTexts != null)
        {
            for (int i = 0; i < scentTexts.Length; i++)
            {
                if (scentTexts[i] == null) continue;
                scentTexts[i].text = resource.GetScent((ScentType)i).ToString("0.#");
            }
        }

        Root.SetActive(true);
    }

    /// <summary>Also wired to the close button automatically (Awake) - no inspector OnClick needed.</summary>
    public void Hide()
    {
        anchor = null;
        anchored = false;
        Root.SetActive(false);
    }
}
