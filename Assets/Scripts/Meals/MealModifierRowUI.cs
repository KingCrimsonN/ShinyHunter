using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One modifier line in a meal read-out (MealDetailsUI spawns one per modifier):
/// its own icon, a label that starts with the affected family ("Bug Encounter
/// Power"), and how strong it is. Every field is optional.
/// </summary>
public class MealModifierRowUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [Tooltip("e.g. \"Bug Encounter Power\" - the family comes first. A brewing preview shows \"??? Encounter Power\" because the family is only rolled at brew time.")]
    [SerializeField] private TMP_Text nameText;
    [Tooltip("The strength as its tier - just the number, 1-4.")]
    [SerializeField] private TMP_Text valueText;

    public void Set(MealModifierInfo modifier, bool isPreview, Sprite iconSprite)
    {
        if (icon != null)
        {
            icon.sprite = iconSprite;
            icon.enabled = iconSprite != null;
        }

        if (nameText != null) nameText.text = MealDisplayUtil.FormatModifierLabel(modifier, isPreview);
        if (valueText != null) valueText.text = modifier.tier.ToString();
    }
}
