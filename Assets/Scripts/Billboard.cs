using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera mainCamera;

    [Tooltip("If true, the sprite locks vertically and only rotates around the Y axis.")]
    [SerializeField] private bool lockYAxis = true;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (mainCamera == null) return;

        if (lockYAxis)
        {
            // Only rotate horizontally (classic Doom style)
            Vector3 targetPosition = mainCamera.transform.position;
            targetPosition.y = transform.position.y;
            transform.LookAt(targetPosition);
        }
        else
        {
            // Always face camera directly (floating items/effects)
            transform.rotation = mainCamera.transform.rotation;
        }
    }
}