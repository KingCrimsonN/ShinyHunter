using UnityEngine;

public interface IInteractable
{
    void Interact();
}

public class Interactor : MonoBehaviour
{

    public Transform interactionSource;
    public float interactionRange = 2f;

    [SerializeField] private GameObject interactionPrompt;

    // Update is called once per frame
    void Update()
    {
        Ray r = new Ray(interactionSource.position, interactionSource.forward);
        RaycastHit hit;
        bool hitSomething = false;

        if (Physics.Raycast(r, out hit, interactionRange))
        {
            if (hit.collider.TryGetComponent<IInteractable>(out var interactable))
            {
                hitSomething = true;
                if (Input.GetKeyDown(KeyCode.E))
                {
                    interactable.Interact();
                }
            }

        }
        interactionPrompt.SetActive(hitSomething);
    }
}
