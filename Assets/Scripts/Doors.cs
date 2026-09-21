using UnityEngine;


public class Doors : MonoBehaviour, IInteractable
{
    [SerializeField] private string sceneName;


    public void UseDoor()
    {
        // UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        ExpeditionStewSelectionUI.Instance.Open();
    }

    public void Interact()
    {
        UseDoor();
    }
}
