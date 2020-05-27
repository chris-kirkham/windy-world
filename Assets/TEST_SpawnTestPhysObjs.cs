using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TEST_SpawnTestPhysObjs : MonoBehaviour
{
    public int num = 0;
    public float spawnRadius = 10;
    
    void Start()
    {
        //create a list of initial children so the next loop doesn't take from objects it initialised when picking from children
        List<GameObject> testObjs = new List<GameObject>(transform.childCount);
        for(int i = 0; i < transform.childCount; i++)
        {
            testObjs.Add(transform.GetChild(i).gameObject);
        }
        
        for(int i = 0; i < num; i++)
        {
            GameObject obj = Instantiate(testObjs[Random.Range(0, testObjs.Count - 1)], transform.position + (Random.insideUnitSphere * spawnRadius), Quaternion.identity);
            obj.GetComponent<Renderer>().material.SetColor("_Color", Color.HSVToRGB(Random.value, 1, 1));
            obj.transform.SetParent(transform);
        }
    
    }

}
