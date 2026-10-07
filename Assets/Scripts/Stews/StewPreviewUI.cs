using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// The "what you'd end up with" readout in the brewing panel: time, the five
/// scents, the modifier and the stew's icon, redrawn by BrewingStationUI every
/// time the cauldron contents change. Display only - it never stores or
/// brews anything. With nothing in the cauldron it shows an empty state
/// (a hint text, or hides itself - see Root Object).
///
/// The modifier line reads "Power: ???" for the affected family on purpose:
/// that family is rolled at the moment of brewing (StewCalculator), so a
/// preview that named one would often be wrong.
/// </summary>
public class StewPreviewUI : MonoBehaviour
{
    [Tooltip("Optional. Shown while there's a recipe to preview and hidden while the cauldron is empty. Leave empty to keep everything always visible (it then just shows the empty state).")]
    [SerializeField] private GameObject rootObject;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text timeText;
    [Tooltip("Exactly 5, in order: Sweet, Fresh, Putrid, Metallic, Marine.")]
    [SerializeField] private TMP_Text[] scentTexts;
    [SerializeField] private TMP_Text modifierText;
    [Tooltip("Optional hint shown while the cauldron is empty. Keep it OUTSIDE Root Object, or it is hidden along with it.")]
    [SerializeField] private TMP_Text emptyHintText;
    [SerializeField] private string emptyHint = "Add ingredients to preview the stew";

    public void Show(StewInstance stew, Sprite stewIcon)
    {
        if (rootObject != null) rootObject.SetActive(true);
        if (emptyHintText != null) emptyHintText.gameObject.SetActive(false);

        if (icon != null)
        {
            icon.sprite = stewIcon;
            icon.enabled = stewIcon != null;
        }

        if (timeText != null) timeText.text = $"{stew.timeSeconds / 60f:0.#} min";
        StewDisplayUtil.SetScentTexts(scentTexts, stew.scents);
        if (modifierText != null) modifierText.text = StewDisplayUtil.FormatModifier(stew);
    }

    /// <summary>Nothing in the cauldron.</summary>
    public void ShowEmpty()
    {
        if (rootObject != null) rootObject.SetActive(false);

        if (emptyHintText != null)
        {
            emptyHintText.gameObject.SetActive(true);
            emptyHintText.text = emptyHint;
        }

        if (icon != null) icon.enabled = false;
        if (timeText != null) timeText.text = string.Empty;
        if (modifierText != null) modifierText.text = string.Empty;
        StewDisplayUtil.SetScentTexts(scentTexts, new float[] { 0f, 0f, 0f, 0f, 0f });
    }
}
