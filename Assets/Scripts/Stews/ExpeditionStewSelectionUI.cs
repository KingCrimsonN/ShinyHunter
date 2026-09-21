using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Exit-door popup: horizontal carousel of the player's bowls, pick the
/// centered one, see its stats below, confirm to consume it and begin the
/// expedition. Opened externally (call Instance.Open()) from the door's
/// IInteractable.
/// </summary>
public class ExpeditionStewSelectionUI : MonoBehaviour
{
    public static ExpeditionStewSelectionUI Instance { get; private set; }

    [Header("Popup")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Transform carouselParent;
    [SerializeField] private StewCarouselEntryUI entryPrefab;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    [Header("Selected Stew Stats")]
    [SerializeField] private TMP_Text timeText;
    [Tooltip("Exactly 5, in order: Sweet, Fresh, Putrid, Metallic, Marine.")]
    [SerializeField] private TMP_Text[] scentTexts;
    [SerializeField] private TMP_Text modifierText;

    [SerializeField] private string expeditionSceneName = "Forest";

    private readonly List<StewCarouselEntryUI> spawned = new List<StewCarouselEntryUI>();
    private int selectedIndex;

    private void Awake()
    {
        Instance = this;
        if (popupRoot != null) popupRoot.SetActive(false);

        if (previousButton != null) previousButton.onClick.AddListener(() => Move(-1));
        if (nextButton != null) nextButton.onClick.AddListener(() => Move(1));
        if (confirmButton != null) confirmButton.onClick.AddListener(Confirm);
        if (cancelButton != null) cancelButton.onClick.AddListener(Close);
    }

    public void Open()
    {
        BuildCarousel();
        selectedIndex = 0;

        if (popupRoot != null) popupRoot.SetActive(true);
        PlayerStateManager.Instance.Freeze();

        RefreshSelection();
    }

    public void Close()
    {
        if (popupRoot != null) popupRoot.SetActive(false);
        PlayerStateManager.Instance.Unfreeze();
    }

    private void BuildCarousel()
    {
        foreach (var entry in spawned)
            if (entry != null) Destroy(entry.gameObject);
        spawned.Clear();

        foreach (var stew in StewInventoryManager.Instance.Bowls)
        {
            var entry = Instantiate(entryPrefab, carouselParent);
            entry.Set(stew);
            spawned.Add(entry);
        }
    }

    private void Move(int direction)
    {
        if (spawned.Count == 0) return;
        selectedIndex = ((selectedIndex + direction) % spawned.Count + spawned.Count) % spawned.Count;
        RefreshSelection();
    }

    private void RefreshSelection()
    {
        for (int i = 0; i < spawned.Count; i++)
            spawned[i].SetSelected(i == selectedIndex);

        if (confirmButton != null) confirmButton.interactable = spawned.Count > 0;

        if (spawned.Count == 0)
        {
            if (timeText != null) timeText.text = "No stews available";
            if (modifierText != null) modifierText.text = string.Empty;
            StewDisplayUtil.SetScentTexts(scentTexts, new float[] { 1f, 1f, 1f, 1f, 1f });
            return;
        }

        var stew = StewInventoryManager.Instance.Bowls[selectedIndex];
        if (timeText != null) timeText.text = $"{stew.timeSeconds / 60f:0.#} min";
        StewDisplayUtil.SetScentTexts(scentTexts, stew.scents);
        if (modifierText != null) modifierText.text = StewDisplayUtil.FormatModifier(stew);
    }

    private void Confirm()
    {
        if (spawned.Count == 0) return;

        var stew = StewInventoryManager.Instance.Bowls[selectedIndex];
        StewInventoryManager.Instance.RemoveStew(stew);
        ExpeditionStewManager.Instance.SetActiveStew(stew);

        Close();
        SceneTransitionManager.Instance.TransitionToScene(expeditionSceneName);
    }
}