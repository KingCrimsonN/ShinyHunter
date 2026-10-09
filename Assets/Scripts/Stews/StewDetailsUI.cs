using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// The ONE stew read-out used everywhere a stew's stats are shown: the brewing
/// panel's live preview, the stew inventory entries, the exit-door selection,
/// the post-brew result and the mid-run panel. Time, the scent radar chart with
/// its five numbers, and one row per modifier (icon + name + strength).
/// Display only - it never stores or changes a stew.
///
/// (This is the old StewPreviewUI, renamed WITH its .meta so existing scene
/// references kept working.) Screens hold ONE of these and no stat fields of
/// their own - there is no second copy of the icon / name / time / scent / modifier
/// plumbing anywhere else.
///
/// Every field is optional - wire only what that screen has.
/// </summary>
public class StewDetailsUI : MonoBehaviour
{
    [Header("Basics")]
    [Tooltip("Optional - the stew's icon.")]
    [SerializeField] private Image icon;
    [Tooltip("Optional - the stew's name (\"Auntie's Stew\" / \"Stew\").")]
    [SerializeField] private TMP_Text nameText;
    [Tooltip("Optional - the stew's expedition time as m:ss. Leave empty on the mid-run panel, which shows the time LEFT instead.")]
    [SerializeField] private TMP_Text timeText;

    [Header("Scents")]
    [Tooltip("The radar chart. Optional.")]
    [SerializeField] private ScentRadarChart scentChart;
    [Tooltip("Exactly 5, in order: Sweet, Fresh, Putrid, Metallic, Marine - the number only (the names are static labels in your art). Independent of the chart's own corner order.")]
    [SerializeField] private TMP_Text[] scentTexts;

    [Header("Modifiers")]
    [Tooltip("Gives each modifier its own icon (StewVisualConfig.modifierIcons).")]
    [SerializeField] private StewVisualConfig visualConfig;
    [Tooltip("One of these is spawned per modifier the stew has.")]
    [SerializeField] private StewModifierRowUI modifierRowPrefab;
    [Tooltip("Where the rows are spawned - give it a Vertical Layout Group so any number of modifiers stacks.")]
    [SerializeField] private Transform modifierParent;
    [Tooltip("Optional - shown instead of the rows when the stew has no modifier (e.g. a \"None\" label).")]
    [SerializeField] private GameObject noModifiersObject;

    [Header("Empty state")]
    [Tooltip("Optional hint shown while there's nothing to show (e.g. an empty cauldron).")]
    [SerializeField] private TMP_Text emptyHintText;
    [SerializeField] private string emptyHint = "Add ingredients to preview the stew";

    private readonly List<StewModifierRowUI> rows = new List<StewModifierRowUI>();

    /// <summary>Shows a stew, using its own icon.</summary>
    public void Show(StewInstance stew) => Show(stew, stew != null ? stew.icon : null);

    public void Show(StewInstance stew, Sprite stewIcon)
    {
        if (stew == null)
        {
            ShowEmpty();
            return;
        }

        if (emptyHintText != null) emptyHintText.gameObject.SetActive(false);

        if (icon != null)
        {
            icon.sprite = stewIcon;
            icon.enabled = stewIcon != null;
        }

        if (nameText != null) nameText.text = StewDisplayUtil.FormatName(stew);
        if (timeText != null) timeText.text = StewDisplayUtil.FormatTime(stew.timeSeconds);

        ShowScents(stew.scents);
        ShowModifiers(stew);
    }

    /// <summary>Nothing to show: chart flat, numbers 0, no modifiers, the hint (if any) visible. Pass a hint to override the default one (e.g. "No stews available").</summary>
    public void ShowEmpty(string hint = null)
    {
        if (emptyHintText != null)
        {
            emptyHintText.gameObject.SetActive(true);
            emptyHintText.text = hint ?? emptyHint;
        }

        if (icon != null) icon.enabled = false;
        if (nameText != null) nameText.text = string.Empty;
        if (timeText != null) timeText.text = string.Empty;

        ShowScents(null);
        ClearRows();
        if (noModifiersObject != null) noModifiersObject.SetActive(false);
    }

    private void ShowScents(float[] scents)
    {
        float max = ExpeditionStewManager.Instance != null ? ExpeditionStewManager.Instance.ScentMax / 5 : 0f; // 0 = chart keeps its own fallback

        if (scentChart != null) scentChart.SetScents(scents, max);
        StewDisplayUtil.SetScentNumbers(scentTexts, scents ?? new float[] { 0f, 0f, 0f, 0f, 0f });
    }

    private void ShowModifiers(StewInstance stew)
    {
        ClearRows();

        var modifiers = stew.GetModifiers();
        if (noModifiersObject != null) noModifiersObject.SetActive(modifiers.Count == 0);

        if (modifierRowPrefab == null || modifierParent == null) return;

        foreach (var modifier in modifiers)
        {
            var row = Instantiate(modifierRowPrefab, modifierParent);
            row.Set(modifier, stew.isPreview, visualConfig != null ? visualConfig.GetModifierIcon(modifier.type) : null);
            rows.Add(row);
        }
    }

    private void ClearRows()
    {
        foreach (var row in rows)
            if (row != null) Destroy(row.gameObject);
        rows.Clear();
    }
}
