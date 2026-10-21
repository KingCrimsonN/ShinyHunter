using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// The ONE meal read-out used everywhere a meal's stats are shown: the brewing
/// panel's live preview, the meal inventory entries, the exit-door selection,
/// the post-brew result and the mid-run panel. Time, the scent radar chart with
/// its five numbers, and one row per modifier (icon + name + strength).
/// Display only - it never stores or changes a meal.
///
/// (This is the old MealPreviewUI, renamed WITH its .meta so existing scene
/// references kept working.) Screens hold ONE of these and no stat fields of
/// their own - there is no second copy of the icon / name / time / scent / modifier
/// plumbing anywhere else.
///
/// Every field is optional - wire only what that screen has.
/// </summary>
public class MealDetailsUI : MonoBehaviour
{
    [Header("Basics")]
    [Tooltip("Optional - the meal's icon.")]
    [SerializeField] private Image icon;
    [Tooltip("Optional - the topping drawn over the icon; hidden when the meal has no modifier.")]
    [SerializeField] private Image modifierTopping;
    [Tooltip("Optional - the fork showing which power the meal has; hidden when it has no modifier.")]
    [SerializeField] private Image modifierFork;
    [Tooltip("Optional - the meal's name (\"Auntie's Meal\" / \"Meal\").")]
    [SerializeField] private TMP_Text nameText;
    [Tooltip("Optional - the meal's description.")]
    [SerializeField] private TMP_Text descriptionText;
    [Tooltip("Optional - the meal's expedition time as m:ss. Leave empty on the mid-run panel, which shows the time LEFT instead.")]
    [SerializeField] private TMP_Text timeText;

    [Header("Scents")]
    [Tooltip("The radar chart. Optional.")]
    [SerializeField] private ScentRadarChart scentChart;
    [Tooltip("Exactly 5, in order: Sweet, Fresh, Putrid, Metallic, Marine - the number only (the names are static labels in your art). Independent of the chart's own corner order.")]
    [SerializeField] private TMP_Text[] scentTexts;

    [Header("Modifiers")]
    [Tooltip("Gives each modifier its own icon (MealVisualConfig.modifierIcons).")]
    [SerializeField] private MealVisualConfig visualConfig;
    [Tooltip("One of these is spawned per modifier the meal has.")]
    [SerializeField] private MealModifierRowUI modifierRowPrefab;
    [Tooltip("Where the rows are spawned - give it a Vertical Layout Group so any number of modifiers stacks.")]
    [SerializeField] private Transform modifierParent;
    [Tooltip("Optional - shown instead of the rows when the meal has no modifier (e.g. a \"None\" label).")]
    [SerializeField] private GameObject noModifiersObject;

    [Header("Empty state")]
    [Tooltip("Optional hint shown while there's nothing to show (e.g. an empty cauldron).")]
    [SerializeField] private TMP_Text emptyHintText;
    [SerializeField] private string emptyHint = "Add ingredients to preview the meal";

    private readonly List<MealModifierRowUI> rows = new List<MealModifierRowUI>();

    public void Show(MealInstance meal)
    {
        if (meal == null)
        {
            ShowEmpty();
            return;
        }

        if (emptyHintText != null) emptyHintText.gameObject.SetActive(false);

        // The sprites were picked once, at brew time (BrewingStationUI.ApplyVisuals).
        SetImage(icon, meal.icon);
        SetImage(modifierTopping, meal.topping);
        SetImage(modifierFork, meal.fork);

        if (nameText != null) nameText.text = MealDisplayUtil.FormatName(meal);
        // if (descriptionText != null) descriptionText.text = meal.description;
        if (timeText != null) timeText.text = MealDisplayUtil.FormatTime(meal.timeSeconds);

        ShowScents(meal.scents);
        ShowModifiers(meal);
    }

    /// <summary>Nothing to show: chart flat, numbers 0, no modifiers, the hint (if any) visible. Pass a hint to override the default one (e.g. "No meals available").</summary>
    public void ShowEmpty(string hint = null)
    {
        if (emptyHintText != null)
        {
            emptyHintText.gameObject.SetActive(true);
            emptyHintText.text = hint ?? emptyHint;
        }

        SetImage(icon, null);
        SetImage(modifierTopping, null);
        SetImage(modifierFork, null);
        if (nameText != null) nameText.text = string.Empty;
        if (timeText != null) timeText.text = string.Empty;

        ShowScents(null);
        ClearRows();
        if (noModifiersObject != null) noModifiersObject.SetActive(false);
    }

    private void ShowScents(float[] scents)
    {
        float max = ExpeditionMealManager.Instance != null ? ExpeditionMealManager.Instance.ScentMax / 5 : 0f; // 0 = chart keeps its own fallback

        if (scentChart != null) scentChart.SetScents(scents, max);
        MealDisplayUtil.SetScentNumbers(scentTexts, scents ?? new float[] { 0f, 0f, 0f, 0f, 0f });
    }

    private void ShowModifiers(MealInstance meal)
    {
        ClearRows();

        var modifiers = meal.GetModifiers();
        if (noModifiersObject != null) noModifiersObject.SetActive(modifiers.Count == 0);

        if (modifierRowPrefab == null || modifierParent == null) return;

        foreach (var modifier in modifiers)
        {
            var row = Instantiate(modifierRowPrefab, modifierParent);
            row.Set(modifier, meal.isPreview, visualConfig != null ? visualConfig.GetModifierIcon(modifier.type) : null);
            rows.Add(row);
        }
    }

    private static void SetImage(Image image, Sprite sprite)
    {
        if (image == null) return;
        image.sprite = sprite;
        image.enabled = sprite != null;
    }

    private void ClearRows()
    {
        foreach (var row in rows)
            if (row != null) Destroy(row.gameObject);
        rows.Clear();
    }
}
