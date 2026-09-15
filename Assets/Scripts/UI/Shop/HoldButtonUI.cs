using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Put on a +/- style button to make holding it repeatedly fire an action,
/// accelerating the longer it's held. Coexists fine with a Button component
/// on the same object (keep that for visual press feedback if you want it)
/// - just don't ALSO wire the Button's onClick, since OnPointerDown here
/// already fires once immediately, same as a normal click would.
/// </summary>
public class HoldButtonUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Tooltip("Seconds between repeats at the START of a hold.")]
    [SerializeField] private float startInterval = 0.4f;
    [Tooltip("Seconds between repeats once fully accelerated.")]
    [SerializeField] private float minInterval = 0.05f;
    [Tooltip("How many seconds of holding it takes to reach minInterval.")]
    [SerializeField] private float accelerationDuration = 1.5f;

    public event Action OnRepeat;

    private bool isHeld;
    private float holdTime;
    private float timeSinceLastRepeat;

    public void OnPointerDown(PointerEventData eventData)
    {
        isHeld = true;
        holdTime = 0f;
        timeSinceLastRepeat = 0f;
        OnRepeat?.Invoke(); // fires once immediately, like a normal click
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isHeld = false;
    }

    private void OnDisable()
    {
        isHeld = false;
    }

    private void Update()
    {
        if (!isHeld) return;

        holdTime += Time.deltaTime;
        timeSinceLastRepeat += Time.deltaTime;

        float progress = accelerationDuration > 0f ? Mathf.Clamp01(holdTime / accelerationDuration) : 1f;
        float currentInterval = Mathf.Lerp(startInterval, minInterval, progress);

        if (timeSinceLastRepeat >= currentInterval)
        {
            timeSinceLastRepeat = 0f;
            OnRepeat?.Invoke();
        }
    }
}