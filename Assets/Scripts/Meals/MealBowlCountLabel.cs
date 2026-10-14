using UnityEngine;
using TMPro;

/// <summary>
/// Shows how many bowls are taken out of the inventory's total slots ("2/6").
/// Put it on a TMP text anywhere (the brewing screen, the meal inventory); it
/// refreshes whenever it is enabled and whenever a bowl is added or removed.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class MealBowlCountLabel : MonoBehaviour
{
    [Tooltip("{0} = bowls taken, {1} = total slots. E.g. \"Bowls {0}/{1}\".")]
    [SerializeField] private string format = "{0}/{1}";

    private TMP_Text label;
    private MealInventoryManager subscribedTo;

    private void Awake() => label = GetComponent<TMP_Text>();

    private void OnEnable()
    {
        subscribedTo = MealInventoryManager.Instance;
        if (subscribedTo != null) subscribedTo.OnBowlsChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (subscribedTo != null) subscribedTo.OnBowlsChanged -= Refresh;
        subscribedTo = null;
    }

    private void Refresh()
    {
        var manager = MealInventoryManager.Instance;
        if (label == null || manager == null) return;

        label.text = string.Format(format, manager.Count, manager.Capacity);
    }
}
