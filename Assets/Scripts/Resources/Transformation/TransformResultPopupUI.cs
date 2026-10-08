using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The pop-up shown when a transformation finishes: a grid of the ingredients
/// you just got (one tile per ingredient with the total amount, double-yield
/// critters already counted) and a close button at the bottom. Closing it only
/// closes THIS pop-up - the transform table underneath stays open
/// (CreatureTransformStationUI also lets Escape close it before the table).
///
/// Put this on the pop-up's root and leave it inactive in the scene, or assign
/// a different Popup Root. Add a full-screen backdrop Image inside the root if
/// the table behind it shouldn't be clickable while it's up.
/// </summary>
public class TransformResultPopupUI : MonoBehaviour
{
    [Tooltip("The object toggled on/off. Defaults to this component's own GameObject.")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Transform gridParent;
    [SerializeField] private TransformResultEntryUI entryPrefab;
    [SerializeField] private Button closeButton;

    private GameObject Root => popupRoot != null ? popupRoot : gameObject;

    public bool IsOpen => Root.activeSelf;

    private void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
    }

    /// <summary>Shows what was granted. Rarest ingredients first, then by name, so the order doesn't depend on which critter happened to be staged first.</summary>
    public void Show(IReadOnlyDictionary<ResourceData, int> granted)
    {
        if (granted == null || granted.Count == 0)
        {
            Hide();
            return;
        }

        if (gridParent != null)
        {
            foreach (Transform child in gridParent)
                Destroy(child.gameObject);

            if (entryPrefab != null)
            {
                foreach (var grant in granted.OrderByDescending(g => (int)g.Key.rarity).ThenBy(g => g.Key.resourceName))
                    Instantiate(entryPrefab, gridParent).Set(grant.Key, grant.Value);
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
