using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Small pop-up on a critter page showing one ingredient's name, description
/// and its five scent stats (opened by clicking the ingredient box - see
/// CritterDexDetailUI). Closed by its own close button, and also whenever the
/// book turns a page. Place it wherever it should appear on the page; it isn't
/// positioned by code.
///
/// Put this component on the pop-up's root object and leave it INACTIVE in the
/// scene (or assign a different Popup Root) - Hide() deactivates that root.
/// </summary>
public class CritterDexIngredientPopupUI : MonoBehaviour
{
    [Tooltip("The object toggled on/off. Defaults to this component's own GameObject.")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [Tooltip("Exactly 5, in ScentType order: Sweet, Fresh, Putrid, Metallic, Marine. Each shows that scent's value - color the boxes in the prefab.")]
    [SerializeField] private TMP_Text[] scentTexts = new TMP_Text[5];

    private GameObject Root => popupRoot != null ? popupRoot : gameObject;

    public bool IsOpen => Root.activeSelf;

    private void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
    }

    public void Show(ResourceData resource)
    {
        if (resource == null)
        {
            Hide();
            return;
        }

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
        Root.SetActive(false);
    }
}
