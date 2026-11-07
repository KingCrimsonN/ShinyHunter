using UnityEngine;

public class GateAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        animator.SetTrigger("Open");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        animator.SetTrigger("Close");
    }
}
