using UnityEngine;

public class WelcomeScreen : MonoBehaviour
{

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PlayerStateManager.Instance.Freeze();
    }

    public void HideWelcomeScreen()
    {
        gameObject.SetActive(false);
        PlayerStateManager.Instance.Unfreeze();
    }

    // Update is called once per frame
    void Update()
    {

    }
}
