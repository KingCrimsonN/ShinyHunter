using UnityEngine;

public class UIManager : MonoBehaviour
{

    [SerializeField] private GameObject inventoryUI;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (inventoryUI != null)
                inventoryUI.SetActive(!inventoryUI.activeSelf);
            Time.timeScale = inventoryUI.activeSelf ? 0 : 1;
        }
    }
}
