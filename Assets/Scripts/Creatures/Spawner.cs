using UnityEngine;

public class Spawner : MonoBehaviour
{

    public GameObject[] creatureDatas;

    public float areaRadius;
    public float population;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for (int i = 0; i < population; i++)
        {
            int index = Random.Range(0, creatureDatas.Length);
            GameObject creature = Instantiate(creatureDatas[index], transform);

            creature.transform.position = new Vector3(Random.Range(-areaRadius, areaRadius), transform.position.y, Random.Range(-areaRadius, areaRadius));
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
