using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Exit-door popup: horizontal carousel of the player's bowls plus the
/// always-available default stew (Auntie's Stew, last in the row), pick the
/// centered one, see its stats below, confirm to consume it (the default is
/// never consumed) and begin the expedition. Opened externally (call
/// Instance.Open()) from the door's IInteractable.
///
/// The selected bowl always sits in the middle of the carousel panel. Next /
/// Previous slide the whole row of bowls one step (eased, not looped: the
/// buttons disable at either end). To do that the bowls live inside a
/// runtime-created "track" under carouselParent, which copies the panel's
/// HorizontalLayoutGroup settings (spacing, top/bottom padding, alignment) so
/// the look stays whatever is set up on the panel - and the panel itself (with
/// its background image) never moves, only the track does.
/// </summary>
public class ExpeditionStewSelectionUI : MonoBehaviour
{
    public static ExpeditionStewSelectionUI Instance { get; private set; }

    [Header("Popup")]
    [SerializeField] private GameObject popupRoot;
    [Tooltip("The carousel panel. Any StewCarouselEntryUI placed here at design time is treated as a layout preview and hidden at runtime; bowls are spawned into a track inside it.")]
    [SerializeField] private Transform carouselParent;
    [SerializeField] private StewCarouselEntryUI entryPrefab;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    [Header("Carousel Motion")]
    [Tooltip("Roughly how many seconds the row takes to slide one step.")]
    [SerializeField] private float shiftTime = 0.2f;

    [Header("Selected Stew Stats")]
    [Tooltip("Optional - the selected stew's name (\"Auntie's Stew\" for the default one, \"Stew\" for brewed ones).")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text timeText;
    [Tooltip("Exactly 5, in order: Sweet, Fresh, Putrid, Metallic, Marine.")]
    [SerializeField] private TMP_Text[] scentTexts;
    [SerializeField] private TMP_Text modifierText;

    [SerializeField] private string expeditionSceneName = "Forest";

    /// <summary>What's on offer, in carousel order: the player's bowls, then the always-available default stew. Parallel to <see cref="spawned"/>.</summary>
    private readonly List<StewInstance> choices = new List<StewInstance>();
    private readonly List<StewCarouselEntryUI> spawned = new List<StewCarouselEntryUI>();
    private readonly List<float> entryCenters = new List<float>(); // each entry's centre X in track space, measured once per build
    private int selectedIndex;

    private RectTransform carouselRect;
    private RectTransform track;
    private float targetTrackX;
    private float trackVelocity;
    private float focusDistance = 1f; // how far (px) an entry can be from the centre and still count as partly focused

    private void Awake()
    {
        Instance = this;
        if (popupRoot != null) popupRoot.SetActive(false);

        carouselRect = carouselParent as RectTransform;
        HideDesignTimeEntries();

        if (previousButton != null) previousButton.onClick.AddListener(() => Move(-1));
        if (nextButton != null) nextButton.onClick.AddListener(() => Move(1));
        if (confirmButton != null) confirmButton.onClick.AddListener(Confirm);
        if (cancelButton != null) cancelButton.onClick.AddListener(Close);
    }

    public void Open()
    {
        // Active BEFORE building: the layout only computes for active objects,
        // and the carousel measures where each entry ended up.
        if (popupRoot != null) popupRoot.SetActive(true);
        PlayerStateManager.Instance.Freeze();

        BuildCarousel();
        selectedIndex = 0;

        RefreshSelection(snap: true);
    }

    public void Close()
    {
        if (popupRoot != null) popupRoot.SetActive(false);
        PlayerStateManager.Instance.Unfreeze();
    }

    private void Update()
    {
        if (track == null || spawned.Count == 0) return;

        // Ease the whole track toward the selected entry; unscaled time so it
        // still animates if something ever pauses the game while this is open.
        float x = Mathf.SmoothDamp(track.anchoredPosition.x, targetTrackX, ref trackVelocity, shiftTime, Mathf.Infinity, Time.unscaledDeltaTime);
        track.anchoredPosition = new Vector2(x, track.anchoredPosition.y);

        UpdateFocus();
    }

    /// <summary>Entries dropped into the panel in the editor are only there to preview the layout - hide them, the real ones live in the track.</summary>
    private void HideDesignTimeEntries()
    {
        if (carouselParent == null) return;

        foreach (Transform child in carouselParent)
        {
            if (child.GetComponent<StewCarouselEntryUI>() != null)
                child.gameObject.SetActive(false);
        }
    }

    private RectTransform EnsureTrack()
    {
        if (track != null) return track;

        var go = new GameObject("CarouselTrack", typeof(RectTransform), typeof(LayoutElement), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        track = (RectTransform)go.transform;
        track.SetParent(carouselParent, false);

        // Centred horizontally on the panel, full height. Its own width is set
        // by the size fitter, so the entries fill it exactly.
        track.anchorMin = new Vector2(0.5f, 0f);
        track.anchorMax = new Vector2(0.5f, 1f);
        track.pivot = new Vector2(0.5f, 0.5f);
        track.anchoredPosition = Vector2.zero;
        track.sizeDelta = Vector2.zero;

        // The panel has its own layout group - keep it from also arranging the track.
        go.GetComponent<LayoutElement>().ignoreLayout = true;

        var layout = go.GetComponent<HorizontalLayoutGroup>();
        var panelLayout = carouselParent.GetComponent<HorizontalLayoutGroup>();
        if (panelLayout != null)
        {
            layout.spacing = panelLayout.spacing;
            // Left/right padding is dropped on purpose: it would offset the row's centre.
            layout.padding = new RectOffset(0, 0, panelLayout.padding.top, panelLayout.padding.bottom);
            layout.childAlignment = panelLayout.childAlignment;
            layout.childControlWidth = panelLayout.childControlWidth;
            layout.childControlHeight = panelLayout.childControlHeight;
            layout.childForceExpandWidth = panelLayout.childForceExpandWidth;
            layout.childForceExpandHeight = panelLayout.childForceExpandHeight;
            layout.childScaleWidth = panelLayout.childScaleWidth;
            layout.childScaleHeight = panelLayout.childScaleHeight;
        }
        else
        {
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        go.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        return track;
    }

    private void BuildCarousel()
    {
        foreach (var entry in spawned)
            if (entry != null) Destroy(entry.gameObject);
        spawned.Clear();
        entryCenters.Clear();

        // The player's bowls first, then the default stew LAST - so with any
        // bowls the first thing selected is a real one, and with none the
        // default is the only (and so selected) choice. The default isn't a
        // bowl: it takes no bowl capacity and is never used up.
        choices.Clear();
        choices.AddRange(StewInventoryManager.Instance.Bowls);
        if (ExpeditionStewManager.Instance != null)
            choices.Add(ExpeditionStewManager.Instance.GetDefaultStew());

        RectTransform trackRect = EnsureTrack();

        foreach (var stew in choices)
        {
            var entry = Instantiate(entryPrefab, trackRect);
            entry.Set(stew);
            spawned.Add(entry);
        }

        if (spawned.Count == 0) return;

        // Let the layout place the entries now, then remember where each one is.
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(trackRect);

        foreach (var entry in spawned)
        {
            var rt = (RectTransform)entry.transform;
            entryCenters.Add(trackRect.InverseTransformPoint(rt.TransformPoint(rt.rect.center)).x);
        }

        // "One step" = the distance between neighbouring entries (or an entry's width if there's just one).
        var firstRect = (RectTransform)spawned[0].transform;
        focusDistance = entryCenters.Count > 1
            ? Mathf.Abs(entryCenters[1] - entryCenters[0])
            : firstRect.rect.width;
        focusDistance = Mathf.Max(1f, focusDistance);
    }

    /// <summary>Steps the selection by one. Not looped - stops at either end.</summary>
    private void Move(int direction)
    {
        if (spawned.Count == 0) return;

        int newIndex = Mathf.Clamp(selectedIndex + direction, 0, spawned.Count - 1);
        if (newIndex == selectedIndex) return;

        selectedIndex = newIndex;
        RefreshSelection(snap: false);
    }

    private void RefreshSelection(bool snap)
    {
        if (confirmButton != null) confirmButton.interactable = spawned.Count > 0;
        if (previousButton != null) previousButton.interactable = selectedIndex > 0;
        if (nextButton != null) nextButton.interactable = selectedIndex < spawned.Count - 1;

        if (spawned.Count == 0)
        {
            if (nameText != null) nameText.text = string.Empty;
            if (timeText != null) timeText.text = "No stews available";
            if (modifierText != null) modifierText.text = string.Empty;
            StewDisplayUtil.SetScentTexts(scentTexts, new float[] { 0f, 0f, 0f, 0f, 0f });
            return;
        }

        // The track is pivoted on the panel's centre, so moving it by minus an
        // entry's X puts that entry in the middle.
        targetTrackX = -entryCenters[selectedIndex];
        if (snap)
        {
            trackVelocity = 0f;
            track.anchoredPosition = new Vector2(targetTrackX, track.anchoredPosition.y);
            UpdateFocus();
        }

        var stew = choices[selectedIndex];
        if (nameText != null) nameText.text = StewDisplayUtil.FormatName(stew);
        if (timeText != null) timeText.text = $"{stew.timeSeconds / 60f:0.#} min";
        StewDisplayUtil.SetScentTexts(scentTexts, stew.scents);
        if (modifierText != null) modifierText.text = StewDisplayUtil.FormatModifier(stew);
    }

    /// <summary>Scale/highlight each entry by how close it currently is to the centre of the panel, so it eases with the slide.</summary>
    private void UpdateFocus()
    {
        var panel = carouselRect != null ? carouselRect : (RectTransform)track.parent;

        foreach (var entry in spawned)
        {
            if (entry == null) continue;

            var rt = (RectTransform)entry.transform;
            float fromCentre = Mathf.Abs(panel.InverseTransformPoint(rt.TransformPoint(rt.rect.center)).x);
            entry.SetFocus(1f - Mathf.Clamp01(fromCentre / focusDistance));
        }
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
        if (popupRoot != null) popupRoot.SetActive(false);
    }
}
