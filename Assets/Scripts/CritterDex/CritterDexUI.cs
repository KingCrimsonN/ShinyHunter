using UnityEngine;

/// <summary>
/// Top-level CritterDex controller: toggles the popup and wires grid
/// selection into the detail panel. Place this on an object that stays
/// active (NOT inside popupRoot itself) - popupRoot is what gets shown/hidden.
/// </summary>
public class CritterDexUI : MonoBehaviour
{
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private CritterDexGridUI gridUI;
    [SerializeField] private CritterDexDetailUI detailUI;
    [SerializeField] private KeyCode toggleKey = KeyCode.J;

    private void Awake()
    {
        if (popupRoot != null) popupRoot.SetActive(false);
    }

    private void OnEnable()
    {
        if (gridUI != null) gridUI.OnSpeciesSelected += detailUI.Show;
    }

    private void OnDisable()
    {
        if (gridUI != null) gridUI.OnSpeciesSelected -= detailUI.Show;
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            TogglePopup();
    }

    private void TogglePopup()
    {
        bool opening = !popupRoot.activeSelf;
        popupRoot.SetActive(opening);
        Time.timeScale = opening ? 0 : 1;

        Cursor.lockState = opening ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = opening;
    }
}