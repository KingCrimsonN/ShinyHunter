using UnityEngine;

public class CauldronInteractable : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        BrewingStationUI.Instance.Open();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
