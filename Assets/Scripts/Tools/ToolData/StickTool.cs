using UnityEngine;

/// <summary>
/// Example ToolBehaviour subclass, showing the pattern for a real tool.
/// This is intentionally a stub - wire in real hit detection here (see
/// PlayerCapture.TrySwingStick for the sphere-cast approach already used
/// in this project), or have PlayerCapture call into whichever tool is
/// currently equipped instead of hardcoding stick logic.
/// </summary>
public class StickTool : ToolBehaviour
{

    [SerializeField] private Animator handAnimator;

    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip swingSound;
    // [SerializeField] private GameObject captureParticles;

    [Header("Stick Hit")]
    [SerializeField] private float hitRange = 2.5f;
    [SerializeField] private float hitRadius = 0.5f;
    [SerializeField] private LayerMask creatureLayer;

    [Header("Capture")]
    [SerializeField] private float captureRange = 3f;
    [SerializeField] private KeyCode captureKey = KeyCode.Mouse1;

    private ICapturable targetedCreature;


    public override void OnEquip()
    {
        Debug.Log("Stick equipped.");
    }

    public override void Update()
    {
        UpdateTargetedCreature();
    }

    private void UpdateTargetedCreature()
    {
        targetedCreature = null;

        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward,
                out RaycastHit hit, captureRange, creatureLayer))
        {
            // print(hit.collider.gameObject.name);
            targetedCreature = hit.collider.GetComponentInParent<ICapturable>();
        }
    }

    private void TryCaptureTargeted()
    {
        // print("Trying Capture");
        if (handAnimator != null)
            handAnimator.SetTrigger("Hit");
        SoundFXManager.instance.PlaySoundFX(swingSound, transform, 0.5f);
        if (targetedCreature == null || !targetedCreature.IsStunned) return;
        CaptureMinigameController.Instance.BeginCapture(targetedCreature);
    }

    public override void UseTool()
    {
        TryCaptureTargeted();
        // TODO: replace with real hit detection (SphereCast in front of the
        // camera against the Creature layer, calling ICapturable.OnHit()).
    }

    // private void TrySwingStick()
    // {
    //     Vector3 origin = Camera.main.transform.position;
    //     Vector3 dir = Camera.main.transform.forward;
    //     handAnimator.SetTrigger("Hit");
    //     SoundFXManager.instance.PlaySoundFX(swingSound, transform, 0.5f);

    //     if (Physics.SphereCast(origin, hitRadius, dir, out RaycastHit hit, hitRange, creatureLayer))
    //     {
    //         var creature = hit.collider.GetComponentInParent<ICapturable>();
    //         creature?.OnHit();
    //         if (creature != null)
    //         {
    //             SoundFXManager.instance.PlaySoundFX(hitSound, transform, 0.5f);
    //         }
    //     }
    // }
}
