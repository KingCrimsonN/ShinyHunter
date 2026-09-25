using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Lives on the Overlay prefab's root and is persistent (DontDestroyOnLoad),
/// which makes the WHOLE Overlay persistent - every scene contains its own
/// Overlay instance, but only the first one survives (see Awake); later ones
/// destroy themselves. Anything under the Overlay must therefore not assume
/// it is re-created per scene: subscribe in OnEnable/OnDisable rather than
/// Start, and don't overwrite statics/singletons from a duplicate's Awake.
///
/// Because the Overlay is shared, it shows/hides its scene-dependent parts
/// itself on every scene load: expedition-only elements (the time/health
/// dial and the tool hotbar) are hidden in the hub, hub-only elements the
/// other way round. Add extra roots to the two arrays in the inspector.
///
/// Player freezing goes through PlayerStateManager like every other popup,
/// so no player references are held here (the player object isn't persistent).
/// </summary>
public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject tabletUI;
    [SerializeField] private GameObject creatureInventoryUI;
    [SerializeField] private GameObject inventoryPage;
    [SerializeField] private GameObject mapPage;
    [SerializeField] private GameObject settingsPage;
    [SerializeField] private GameObject critterDexPage;
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private Image hurtScreen;

    [SerializeField] private TMP_Text interactionText;

    [Header("Scene-dependent visibility")]
    [Tooltip("Extra HUD roots visible ONLY on expeditions. The expedition time/health dial and the tool hotbar are found automatically - no need to list them.")]
    [SerializeField] private GameObject[] expeditionOnlyElements;
    [Tooltip("HUD roots visible ONLY in the hub.")]
    [SerializeField] private GameObject[] hubOnlyElements;

    public bool extraOpened;

    public static UIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public void ShowHurtScreen()
    {
        if (hurtScreen != null)
        {

            DOTween.Kill(hurtScreen); // kill any existing tweens on the hurtScreen to prevent overlapping animations
            hurtScreen.DOFade(1f, 0f).OnComplete(() =>
            {
                hurtScreen.DOFade(0f, 0.75f).SetEase(Ease.OutCirc);
            });
            // hurtScreen.DOColor(new Color(hurtScreen.color.r, hurtScreen.color.g, hurtScreen.color.b, 1f), 0f).OnComplete(() =>
            // {
            // hurtScreen.DOColor(new Color(hurtScreen.color.r, hurtScreen.color.g, hurtScreen.color.b, 0f), 0.05f);
            // });

        }

    }



    private void OnDestroy()
    {
        if (Instance != this) return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
    }

    private void Start()
    {
        if (tabletUI != null) tabletUI.SetActive(false);
        extraOpened = false;

        // Start only runs once for this persistent object, i.e. for whichever
        // scene loaded first - every later scene goes through OnSceneLoaded.
        ApplySceneVisibility(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        extraOpened = false;
        ApplySceneVisibility(scene.name);
    }

    private void ApplySceneVisibility(string sceneName)
    {
        bool inHub = SceneNames.IsHub(sceneName);

        var timeIndicator = GetComponentInChildren<ExpeditionTimeIndicator>(true);
        if (timeIndicator != null) timeIndicator.gameObject.SetActive(!inHub);

        var hotbar = GetComponentInChildren<ToolHotbarUI>(true);
        if (hotbar != null) hotbar.gameObject.SetActive(!inHub);

        SetAllActive(expeditionOnlyElements, !inHub);
        SetAllActive(hubOnlyElements, inHub);
    }

    private static void SetAllActive(GameObject[] elements, bool active)
    {
        if (elements == null) return;

        foreach (var element in elements)
            if (element != null) element.SetActive(active);
    }

    // Update is called once per frame
    private void Update()
    {
        if (extraOpened)
            return;

        // Each tab has its own shortcut key straight to that page - see
        // HandleTabletShortcut. Escape is the odd one out: it ALWAYS closes
        // the tablet first if it's open (universal "back", not just when
        // Settings happens to be the visible page), only falling through to
        // "open to Settings" once it's already closed.
        if (Input.GetKeyDown(KeyCode.I)) HandleTabletShortcut(inventoryPage, ShowInventoryPage);
        if (Input.GetKeyDown(KeyCode.M)) HandleTabletShortcut(mapPage, ShowMapPage);
        if (Input.GetKeyDown(KeyCode.J)) HandleTabletShortcut(critterDexPage, ShowCritterDexPage);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (tabletUI != null && tabletUI.activeSelf)
            {
                HideTabletUI();
                return;
            }
            HandleTabletShortcut(settingsPage, ShowSettingsPage);
        }
    }

    /// <summary>
    /// Shared behaviour for every tablet-tab shortcut key (I/M/J, and Escape
    /// once it's confirmed the tablet is closed): if the tablet is closed,
    /// opens it straight to this page (respecting the same "don't open over
    /// another frozen popup" guard ToggleTabletUI uses). If it's already open
    /// showing THIS SAME page, pressing the key again closes it - so I/M/J
    /// each double as their own close shortcut. If it's open on a DIFFERENT
    /// page, just switches to this one without closing.
    /// </summary>
    private void HandleTabletShortcut(GameObject page, System.Action showPage)
    {
        bool tabletOpen = tabletUI != null && tabletUI.activeSelf;

        if (tabletOpen)
        {
            if (page != null && page.activeSelf)
                HideTabletUI();
            else
                showPage();
            return;
        }

        if (!CanOpenTablet()) return;

        SetTabletOpen(true);
        showPage();
    }

    public void ShowDialogue()
    {
        dialogPanel.SetActive(true);
    }

    public void HideDialogue()
    {
        dialogPanel.SetActive(false);
    }

    public void ToggleTabletUI()
    {
        if (tabletUI == null) return;

        bool opening = !tabletUI.activeSelf;
        if (opening && !CanOpenTablet()) return;

        SetTabletOpen(opening);
    }

    public void HideTabletUI()
    {
        if (tabletUI != null && tabletUI.activeSelf)
            SetTabletOpen(false);
    }

    /// <summary>Another popup (dialogue, brewing, run summary...) already owns the player - opening the tablet on top of it would unfreeze/relock the cursor underneath that popup when the tablet closes again.</summary>
    private bool CanOpenTablet()
    {
        return PlayerStateManager.Instance == null || !PlayerStateManager.Instance.IsFrozen;
    }

    private void SetTabletOpen(bool open)
    {
        tabletUI.SetActive(open);
        if (creatureInventoryUI != null)
            creatureInventoryUI.SetActive(open);

        if (open)
            LockPlayer();
        else
            UnlockPlayer();
    }

    public void LockPlayer()
    {
        Time.timeScale = 0;
        if (PlayerStateManager.Instance != null)
            PlayerStateManager.Instance.Freeze();
    }

    public void UnlockPlayer()
    {
        Time.timeScale = 1;
        if (PlayerStateManager.Instance != null)
            PlayerStateManager.Instance.Unfreeze();
    }

    public void ShowCreatures()
    {
        if (creatureInventoryUI != null)
            creatureInventoryUI.SetActive(true);
    }

    public void HideCreatures()
    {
        if (creatureInventoryUI != null)
        {
            creatureInventoryUI.SetActive(false);
        }
    }

    public void ShowInteractionText(string text)
    {
        if (interactionText != null)
        {
            interactionText.text = text;
            interactionText.gameObject.SetActive(true);
        }
    }

    public void HideInteractionText()
    {
        if (interactionText != null)
        {
            interactionText.gameObject.SetActive(false);
        }
    }

    public void ShowInventoryPage() => SetActivePage(inventoryPage);
    public void ShowMapPage() => SetActivePage(mapPage);
    public void ShowSettingsPage() => SetActivePage(settingsPage);
    public void ShowCritterDexPage() => SetActivePage(critterDexPage);

    private void SetActivePage(GameObject page)
    {
        if (inventoryPage != null) inventoryPage.SetActive(page == inventoryPage);
        if (mapPage != null) mapPage.SetActive(page == mapPage);
        if (settingsPage != null) settingsPage.SetActive(page == settingsPage);
        if (critterDexPage != null) critterDexPage.SetActive(page == critterDexPage);
    }
}
