using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Read-only display of one bowl in the stew inventory panel.</summary>
public class StewBowlEntryUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text timeText;
    [Tooltip("Exactly 5, in order: Sweet, Fresh, Putrid, Metallic, Marine.")]
    [SerializeField] private TMP_Text[] scentTexts;
    [SerializeField] private TMP_Text modifierText;

    public void Set(StewInstance stew)
    {
        if (icon != null) icon.sprite = stew.icon;
        if (timeText != null) timeText.text = $"{stew.timeSeconds / 60f:0.#} min";
        StewDisplayUtil.SetScentTexts(scentTexts, stew.scents);
        if (modifierText != null) modifierText.text = StewDisplayUtil.FormatModifier(stew);
    }
}