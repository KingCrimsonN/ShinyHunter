using UnityEngine;

/// <summary>
/// Wires the CritterDex grid's selection into the detail panel. Lives on the
/// "Critters" sub-panel of the CritterDex tablet page (see
/// CritterDexTabController) - it no longer owns any popup/freeze/cursor
/// logic of its own (that used to duplicate what the tablet already does via
/// UIManager.SetTabletOpen -> PlayerStateManager, and the old standalone J-key
/// toggle this component used is retired now that the dex lives in the
/// tablet - see decision log). OnEnable/OnDisable fire whenever this panel
/// itself is shown/hidden by CritterDexTabController, which is exactly when
/// the wiring should be live.
/// </summary>
public class CritterDexUI : MonoBehaviour
{
    [SerializeField] private CritterDexGridUI gridUI;
    [SerializeField] private CritterDexDetailUI detailUI;

    private void OnEnable()
    {
        if (gridUI != null && detailUI != null) gridUI.OnSpeciesSelected += detailUI.Show;
    }

    private void OnDisable()
    {
        if (gridUI != null && detailUI != null) gridUI.OnSpeciesSelected -= detailUI.Show;
    }
}
