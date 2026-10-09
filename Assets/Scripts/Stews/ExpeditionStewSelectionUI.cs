using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Exit-door popup: a carousel of the player's bowls - or, only if they have
/// none, the default stew (Auntie's Stew) as the single choice. Pick the
/// centred one, see its stats below, confirm to consume it (the default is
/// never consumed) and begin the expedition. Escape closes it. Opened
/// externally (Instance.Open()) from the door.
///
/// There is NO layout group involved. Each bowl's position is computed by code
/// from one number - `position`, the (fractional) index currently in the
/// middle: x = (index - position) * spacing. Next/Previous just change the
/// target index and `position` eases toward it, so the row slides and each
/// bowl's focus (scale/highlight) follows its distance from the centre.
/// The previous version built a runtime layout "track" and MEASURED where the
/// layout put each bowl; since Destroy() only takes effect at the end of the
/// frame, reopening the popup measured the new bowls alongside the old,
/// not-yet-destroyed ones and everything came out offset. Nothing here
/// measures anything, so it can't go stale. See decision log.
/// </summary>
public class ExpeditionStewSelectionUI : MonoBehaviour
{
    public static ExpeditionStewSelectionUI Instance { get; private set; }

    [Header("Popup")]
    [SerializeField] private GameObject popupRoot;
    [Tooltip("The carousel panel - bowls are spawned straight into it and positioned around its centre. Any StewCarouselEntryUI placed here at design time is only a preview and is hidden at runtime.")]
    [SerializeField] private Transform carouselParent;
    [SerializeField] private StewCarouselEntryUI entryPrefab;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    [Header("Carousel")]
    [Tooltip("Distance in pixels between neighbouring bowls' centres. 0 = automatic: one bowl's width plus the panel's HorizontalLayoutGroup spacing (if it still has one).")]
    [SerializeField] private float entrySpacing = 0f;
    [Tooltip("Roughly how many seconds the row takes to slide one step.")]
    [SerializeField] private float shiftTime = 0.2f;

    [Header("Selected Stew Stats")]
    [Tooltip("The shared stew read-out for the centred stew (name, time, scent chart, modifiers).")]
    [SerializeField] private StewDetailsUI details;

    [SerializeField] private string expeditionSceneName = "Forest";

    /// <summary>What's on offer, in carousel order (the player's bowls, or just the default stew if they have none). Parallel to <see cref="spawned"/>.</summary>
    private readonly List<StewInstance> choices = new List<StewInstance>();
    private readonly List<StewCarouselEntryUI> spawned = new List<StewCarouselEntryUI>();

    private int selectedIndex;
    private float position;          // fractional index currently in the middle of the panel
    private float positionVelocity;
    private float spacing;

    private void Awake()
    {
        Instance = this;
        if (popupRoot != null) popupRoot.SetActive(false);

        HideDesignTimeEntries();

        if (previousButton != null) previousButton.onClick.AddListener(() => Move(-1));
        if (nextButton != null) nextButton.onClick.AddListener(() => Move(1));
        if (confirmButton != null) confirmButton.onClick.AddListener(Confirm);
        if (cancelButton != null) cancelButton.onClick.AddListener(Close);
    }

    public void Open()
    {
        if (popupRoot != null) popupRoot.SetActive(true);
        // Close is registered as the Escape callback (UIManager's centralized
        // Escape handling -> PlayerStateManager.TryCloseCurrentPopup).
        PlayerStateManager.Instance.Freeze(Close);

        BuildCarousel();
        selectedIndex = 0;
        position = 0f;
        positionVelocity = 0f;

        RefreshSelection();
        PositionEntries();
    }

    public void Close()
    {
        if (popupRoot != null) popupRoot.SetActive(false);
        PlayerStateManager.Instance.Unfreeze();
    }

    private void Update()
    {
        if (spawned.Count == 0) return;

        // Unscaled time so it still animates if something pauses the game while this is open.
        position = Mathf.SmoothDamp(position, selectedIndex, ref positionVelocity, shiftTime, Mathf.Infinity, Time.unscaledDeltaTime);
        PositionEntries();
    }

    /// <summary>Entries dropped into the panel in the editor are only there to preview the look - hide them, the real ones are spawned.</summary>
    private void HideDesignTimeEntries()
    {
        if (carouselParent == null) return;

        foreach (Transform child in carouselParent)
        {
            if (child.GetComponent<StewCarouselEntryUI>() != null)
                child.gameObject.SetActive(false);
        }
    }

    private void BuildCarousel()
    {
        foreach (var entry in spawned)
            if (entry != null) Destroy(entry.gameObject); // deferred - harmless now, nothing below depends on the old ones being gone
        spawned.Clear();

        // The player's bowls - and ONLY when they have none, the default stew
        // as the single fallback so an expedition is always possible. Once
        // they own any real stew the default isn't offered at all. The
        // default isn't a bowl: it takes no bowl capacity and is never used up.
        choices.Clear();
        choices.AddRange(StewInventoryManager.Instance.Bowls);
        if (choices.Count == 0 && ExpeditionStewManager.Instance != null)
            choices.Add(ExpeditionStewManager.Instance.GetDefaultStew());

        foreach (var stew in choices)
        {
            var entry = Instantiate(entryPrefab, carouselParent);
            // entryPrefab is wired to the design-time preview sitting under
            // carouselParent, which HideDesignTimeEntries disabled - and
            // Instantiate clones that disabled state. A spawned entry must
            // always be visible, so force it on.
            entry.gameObject.SetActive(true);
            entry.Set(stew);

            var rt = (RectTransform)entry.transform;

            // We position every entry ourselves, so any layout group still on
            // the panel must leave them alone.
            if (!entry.TryGetComponent(out LayoutElement layoutElement))
                layoutElement = entry.gameObject.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;

            // Centre-anchored at the panel's middle, keeping whatever size the
            // entry has now - from here on only anchoredPosition changes.
            Vector2 size = rt.rect.size;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;

            spawned.Add(entry);
        }

        spacing = ResolveSpacing();
    }

    private float ResolveSpacing()
    {
        if (entrySpacing > 0f) return entrySpacing;
        if (spawned.Count == 0) return 1f;

        float width = ((RectTransform)spawned[0].transform).rect.width;
        if (width <= 0f) width = 200f;

        var panelLayout = carouselParent != null ? carouselParent.GetComponent<HorizontalLayoutGroup>() : null;
        return width + (panelLayout != null ? panelLayout.spacing : 0f);
    }

    /// <summary>Slide each bowl to (index - position) * spacing, and focus (scale/highlight) it by how close that is to the centre.</summary>
    private void PositionEntries()
    {
        for (int i = 0; i < spawned.Count; i++)
        {
            if (spawned[i] == null) continue;

            float offset = i - position;
            ((RectTransform)spawned[i].transform).anchoredPosition = new Vector2(offset * spacing, 0f);
            spawned[i].SetFocus(1f - Mathf.Clamp01(Mathf.Abs(offset)));
        }
    }

    /// <summary>Steps the selection by one. Not looped - stops at either end.</summary>
    private void Move(int direction)
    {
        if (spawned.Count == 0) return;

        int newIndex = Mathf.Clamp(selectedIndex + direction, 0, spawned.Count - 1);
        if (newIndex == selectedIndex) return;

        selectedIndex = newIndex;
        RefreshSelection();
    }

    private void RefreshSelection()
    {
        if (confirmButton != null) confirmButton.interactable = spawned.Count > 0;
        if (previousButton != null) previousButton.interactable = selectedIndex > 0;
        if (nextButton != null) nextButton.interactable = selectedIndex < spawned.Count - 1;

        if (details == null) return;

        if (spawned.Count == 0) details.ShowEmpty("No stews available");
        else details.Show(choices[selectedIndex]);
    }

    private void Confirm()
    {
        if (spawned.Count == 0) return;

        // Start the transition FIRST: if it can't happen (scene missing, a
        // transition already running) the stew must not be consumed.
        if (SceneTransitionManager.Instance == null || !SceneTransitionManager.Instance.TransitionToScene(expeditionSceneName))
        {
            Debug.LogError($"ExpeditionStewSelectionUI: couldn't start the transition to '{expeditionSceneName}' - stew not consumed.");
            return;
        }

        var stew = choices[selectedIndex];

        // The default stew isn't in a bowl - there's nothing to remove, so it
        // can be taken every single time.
        if (!stew.isDefault)
            StewInventoryManager.Instance.RemoveStew(stew);

        ExpeditionStewManager.Instance.SetActiveStew(stew);

        // Hide the popup but do NOT unfreeze: SceneTransitionManager keeps
        // the player frozen through the fade (so the door can't be used a
        // second time) and releases it once the expedition scene has loaded.
        // Re-freezing with NO close callback drops the one Open registered -
        // otherwise Escape during the fade would call Close() and unfreeze
        // the player mid-transition.
        if (popupRoot != null) popupRoot.SetActive(false);
        PlayerStateManager.Instance.Freeze();
    }
}
