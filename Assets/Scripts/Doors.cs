using UnityEngine;


public class Doors : MonoBehaviour, IInteractable
{
    [SerializeField] private string sceneName;


    public void UseDoor()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }

    public void Interact()
    {
        UseDoor();
    }
}
