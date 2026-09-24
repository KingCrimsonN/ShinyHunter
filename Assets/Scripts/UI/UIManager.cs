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
            hurtScreen.color = new Color(hurtScreen.color.r, hurtScreen.color.g, hurtScreen.color.b, 1f);
            hurtScreen.DOColor(new Color(hurtScreen.color.r, hurtScreen.color.g, hurtScreen.color.b, 0f), 0.5f);
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
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleTabletUI();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (tabletUI != null && tabletUI.activeSelf)
            {
                HideTabletUI();
                return;
            }
            if (CreatureTransformStationUI.Instance != null)
                ToggleTabletUI();
            ShowSettingsPage();
        }
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

        // Another popup (dialogue, brewing, run summary...) already owns the
        // player - opening the tablet on top of it would unfreeze/relock the
        // cursor underneath that popup when the tablet closes again.
        if (opening && PlayerStateManager.Instance != null && PlayerStateManager.Instance.IsFrozen)
            return;

        SetTabletOpen(opening);
    }

    public void HideTabletUI()
    {
        if (tabletUI != null && tabletUI.activeSelf)
            SetTabletOpen(false);
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

    public void ShowInventoryPage()
    {
        if (inventoryPage != null)
            inventoryPage.SetActive(true);
        if (mapPage != null)
            mapPage.SetActive(false);
        if (settingsPage != null)
            settingsPage.SetActive(false);
    }

    public void ShowMapPage()
    {
        if (inventoryPage != null)
            inventoryPage.SetActive(false);
        if (mapPage != null)
            mapPage.SetActive(true);
        if (settingsPage != null)
            settingsPage.SetActive(false);
    }

    public void ShowSettingsPage()
    {
        if (inventoryPage != null)
            inventoryPage.SetActive(false);
        if (mapPage != null)
            mapPage.SetActive(false);
        if (settingsPage != null)
            settingsPage.SetActive(true);
    }
}
