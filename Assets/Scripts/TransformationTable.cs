using UnityEngine;

public class TransformationTable : MonoBehaviour, IInteractable
{


    public void Interact()
    {
        CreatureTransformStationUI.Instance.Open();
    }
}
