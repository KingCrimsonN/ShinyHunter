using DG.Tweening;
using UnityEngine;

/// <summary>
/// Slow, endless "breathing" drift for the main menu camera: it eases to a random
/// nearby offset (position and a slight tilt) around where it was placed, holds
/// for a moment, then picks the next one - like a handheld camera that never
/// quite settles. Put it on the menu camera (or a parent of it).
///
/// Everything is relative to the camera's starting local position/rotation, so
/// frame the shot in the editor as usual and this only adds the motion. Runs on
/// unscaled time, so it keeps moving even if something set Time.timeScale to 0.
/// </summary>
public class MenuCameraDrift : MonoBehaviour
{
    [Header("How far it wanders")]
    [Tooltip("Largest offset from the starting position on each axis (local units).")]
    [SerializeField] private Vector3 positionRange = new Vector3(0.3f, 0.15f, 0.2f);
    [Tooltip("Largest tilt from the starting rotation on each axis, in degrees (X = look up/down, Y = turn, Z = roll).")]
    [SerializeField] private Vector3 rotationRange = new Vector3(1f, 2f, 0.5f);

    [Header("Timing")]
    [Tooltip("Seconds each move takes (random in range) - longer = calmer.")]
    [SerializeField] private Vector2 moveDuration = new Vector2(6f, 10f);
    [Tooltip("Seconds it rests between moves (random in range). 0 = keeps flowing.")]
    [SerializeField] private Vector2 pauseDuration = new Vector2(0f, 1f);
    [SerializeField] private Ease ease = Ease.InOutSine;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private Sequence sequence;

    private void Awake()
    {
        startPosition = transform.localPosition;
        startRotation = transform.localRotation;
    }

    private void OnEnable() => NextMove();

    private void OnDisable()
    {
        sequence?.Kill();
        sequence = null;
    }

    private void NextMove()
    {
        Vector3 offset = new Vector3(
            Random.Range(-positionRange.x, positionRange.x),
            Random.Range(-positionRange.y, positionRange.y),
            Random.Range(-positionRange.z, positionRange.z));

        Quaternion tilt = Quaternion.Euler(
            Random.Range(-rotationRange.x, rotationRange.x),
            Random.Range(-rotationRange.y, rotationRange.y),
            Random.Range(-rotationRange.z, rotationRange.z));

        float duration = Random.Range(moveDuration.x, moveDuration.y);

        sequence = DOTween.Sequence()
            .Append(transform.DOLocalMove(startPosition + offset, duration).SetEase(ease))
            .Join(transform.DOLocalRotateQuaternion(startRotation * tilt, duration).SetEase(ease))
            .AppendInterval(Random.Range(pauseDuration.x, pauseDuration.y))
            .SetUpdate(true) // unscaled time
            .SetLink(gameObject)
            .OnComplete(NextMove);
    }
}
