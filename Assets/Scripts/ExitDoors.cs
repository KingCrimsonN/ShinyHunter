using UnityEngine;


public class ExitDoors : MonoBehaviour, IInteractable
{
    [SerializeField] private string sceneName;



    public void UseDoor()
    {
        RunSummaryUI.Instance.ShowSummary();
        // UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }

    public void Interact()
    {
        UseDoor();
    }
}