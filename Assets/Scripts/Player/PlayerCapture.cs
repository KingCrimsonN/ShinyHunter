using UnityEngine;

/// <summary>
/// Handles the "hit with a stick, then capture" loop.
/// Swing stuns any ICapturable it connects with; capture only succeeds
/// against a currently-stunned creature.
///
/// Aiming goes through CreatureTargeting (the same code the voodoo doll uses),
/// so what the stick can hit and what the doll considers "in reach" agree.
///
/// This is intentionally decoupled from CreatureAI via ICapturable so it can
/// later be replaced/extended with the GDD's seal-throw + QTE system without
/// touching creature code.
/// </summary>
public class PlayerCapture : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Animator handAnimator;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip[] swingSounds;

    [Header("Stick Hit")]
    [Tooltip("How far the stick reaches, measured to the nearest point of the creature.")]
    [SerializeField] private float hitRange = 2.5f;
    [Tooltip("How far off the crosshair (world units, on top of the creature's own size) still counts as aiming at it.")]
    [SerializeField] private float hitRadius = 0.5f;
    [SerializeField] private KeyCode hitKey = KeyCode.Mouse0;
    [Tooltip("Damage dealt per hit, before Capture Power's bonus - see CreatureAI/CaptureMinigameConfig.healthPerRarity for what this means in hits-to-stun per rarity.")]
    [SerializeField] private float baseDamage = 10f;
    [Tooltip("Seconds between stick swings - stops it being spammed. The swing animation/sound are gated too, not just the damage.")]
    [SerializeField] private float hitCooldown = 0.5f;

    [Header("Target Marker (lock-on)")]
    [Tooltip("Show a small marker on the critter the stick would hit right now - in reach and on the crosshair.")]
    [SerializeField] private bool showTargetMarker = true;
    [Tooltip("The marker sprite. Empty = a small soft dot.")]
    [SerializeField] private Sprite targetMarkerSprite;
    [SerializeField] private Color targetMarkerColor = new Color(1f, 1f, 1f, 0.85f);
    [Tooltip("Marker height as a fraction of the screen height - stays this size at any distance.")]
    [SerializeField, Range(0.005f, 0.2f)] private float targetMarkerScreenSize = 0.025f;
    [Tooltip("Seconds it takes to shrink from Pop Scale onto the critter when a new critter comes into reach.")]
    [SerializeField, Min(0f)] private float targetMarkerPopDuration = 0.12f;
    [SerializeField, Min(1f)] private float targetMarkerPopScale = 1.6f;
    [Tooltip("Seconds to fade in/out.")]
    [SerializeField, Min(0f)] private float targetMarkerFade = 0.1f;

    private float nextHitTime;
    private TargetLockMarker marker;

    public bool isActive;

    /// <summary>How far the stick reaches - the voodoo doll uses this to tell whether a click will also stun the creature.</summary>
    public float HitRange => hitRange;

    /// <summary>Extra off-centre aim forgiveness, shared with the doll's targeting.</summary>
    public float HitRadius => hitRadius;

    private void Update()
    {
        if (!isActive)
        {
            if (marker != null) marker.Clear();
            return;
        }

        UpdateTargetMarker();

        if (Input.GetKeyDown(hitKey))
            TrySwingStick();
    }

    private void OnDisable()
    {
        if (marker != null) marker.Clear();
    }

    private void OnDestroy()
    {
        if (marker != null) Destroy(marker.gameObject);
    }

    /// <summary>Same targeting as the swing itself, so the marker is exactly "a click now hits this".</summary>
    private void UpdateTargetMarker()
    {
        if (!showTargetMarker) return;

        if (marker == null)
            marker = TargetLockMarker.Create(targetMarkerSprite, targetMarkerColor, targetMarkerScreenSize,
                targetMarkerPopDuration, targetMarkerPopScale, targetMarkerFade);

        Camera cam = playerCamera != null ? playerCamera : Camera.main;
        if (cam != null && CreatureTargeting.TryFind(cam.transform, hitRange, hitRadius, out CreatureTarget target))
            marker.SetTarget(target);
    }

    private void TrySwingStick()
    {
        if (Time.time < nextHitTime) return;
        nextHitTime = Time.time + hitCooldown;

        Camera cam = playerCamera != null ? playerCamera : Camera.main;

        if (handAnimator != null) handAnimator.SetTrigger("Hit");
        SoundFXManager.instance.PlayRandomSoundFX(swingSounds, transform, 0.5f);

        if (cam == null) return;

        if (CreatureTargeting.TryFind(cam.transform, hitRange, hitRadius, out CreatureTarget target))
        {
            // Capture Power can raise hit power - family-scoped like every
            // other modifier (see ExpeditionMealManager.GetHitPowerBonus).
            float hitPowerBonus = ExpeditionMealManager.Instance != null
                ? ExpeditionMealManager.Instance.GetHitPowerBonus(target.creature.Data.family)
                : 0f;
            float damage = baseDamage * (1f + hitPowerBonus);

            target.creature.OnHit(damage);
            SoundFXManager.instance.PlaySoundFX(hitSound, transform, 0.5f);
        }
    }
}
