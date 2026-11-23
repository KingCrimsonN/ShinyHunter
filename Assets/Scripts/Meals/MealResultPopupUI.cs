using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>Shown right after brewing - the meal's read-out + Accept (into a bowl) / Dump (discard the meal, bowl stays free).</summary>
public class MealResultPopupUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [Tooltip("The shared meal read-out (icon, name, time, scent chart, modifiers).")]
    [SerializeField] private MealDetailsUI details;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button dumpButton;

    [Header("Audio")]
    [SerializeField] private AudioClip mealReadySound;
    [SerializeField] private AudioClip acceptSound;
    [SerializeField] private AudioClip dumpSound;

    private MealInstance currentMeal;

    private void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (acceptButton != null) acceptButton.onClick.AddListener(Accept);
        if (dumpButton != null) dumpButton.onClick.AddListener(Dump);
    }

    public void Show(MealInstance meal)
    {
        currentMeal = meal;
        if (details != null) details.Show(meal);
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
            panelRoot.transform.DOScale(Vector3.one, 0.25f).From(Vector3.zero).SetEase(Ease.OutBack);
        }
        SoundFXManager.instance?.PlaySoundFX(mealReadySound, transform, 1f);
    }

    private void Accept()
    {
        MealInventoryManager.Instance.TryAddMeal(currentMeal);
        SoundFXManager.instance?.PlaySoundFX(acceptSound, transform, 1f);
        Hide();
    }

    private void Dump()
    {
        // Ingredients were already spent when brewing started - dumping just discards the meal, the bowl stays free.
        Hide();
        SoundFXManager.instance?.PlaySoundFX(dumpSound, transform, 1f);
    }

    private void Hide()
    {
        if (panelRoot != null) panelRoot.transform.DOScale(Vector3.zero, 0.25f).From(Vector3.one).SetEase(Ease.InBack).OnComplete(() => panelRoot.SetActive(false));
        currentMeal = null;
        BrewingStationUI.Instance?.RefreshAfterResult();
    }
}
