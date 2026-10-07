using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Put on a Button to switch critter inventories between individual and
/// grouped view (CritterInventoryView). Hooks its own button in code - no
/// OnClick entry needed - and optionally keeps a label in sync. Any number of
/// these can exist (inventory, transform table); they all flip the same setting.
/// </summary>
[RequireComponent(typeof(Button))]
public class CritterViewToggleButton : MonoBehaviour
{
    [Tooltip("Optional label that shows which view a click switches TO.")]
    [SerializeField] private TMP_Text label;
    [SerializeField] private string showGroupedText = "Group";
    [SerializeField] private string showIndividualText = "Ungroup";

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(CritterInventoryView.Toggle);
    }

    private void OnEnable()
    {
        CritterInventoryView.Changed += RefreshLabel;
        RefreshLabel();
    }

    private void OnDisable()
    {
        CritterInventoryView.Changed -= RefreshLabel;
    }

    private void RefreshLabel()
    {
        if (label != null) label.text = CritterInventoryView.Grouped ? showIndividualText : showGroupedText;
    }
}
