using UnityEngine;

public class BlobShadow : MonoBehaviour
{
    public GameObject shadow;
    public RaycastHit hit;
    public float offset;

    void FixedUpdate()
    {
        Ray downRay = new Ray(new Vector3(transform.position.x, transform.position.y, transform.position.z), -Vector3.up);

        Vector3 hitPosition = hit.point;
        shadow.transform.position = hitPosition;

        if (Physics.Raycast(downRay, out hit))
        {

        }
    }
}
