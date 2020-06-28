using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Editor/debug visualiser for WindArea.
/// </summary>
[RequireComponent(typeof(WindArea))]
[ExecuteInEditMode]
public class WindAreaVis : MonoBehaviour
{
    private WindArea windArea;
    private GameObject windArrow;
    private Vector3 initArrowScale;

    void Start()    
    {
        windArea = GetComponent<WindArea>();
        CreateWindArrow();
        initArrowScale = windArrow.transform.localScale;
    }

    // Update is called once per frame
    void Update()
    {
        windArrow.transform.position = windArea.transform.position;
        if(windArea.GetWind() != Vector3.zero) windArrow.transform.rotation = Quaternion.LookRotation(windArea.GetWind(), Vector3.up);
        Vector3 scale = transform.localScale;
        windArrow.transform.localScale = new Vector3(initArrowScale.x * scale.x, initArrowScale.y * scale.y, initArrowScale.z * scale.z);
    }

    private void OnDestroy()
    {
        DestroyImmediate(windArrow);
    }

    private void CreateWindArrow()
    {
        //windArrow = transform.parent.transform.Find("WindArrow").gameObject;
        
        if (windArrow == null)
        {
            Debug.Log("windArrow == null");
            Quaternion rotation = windArea.GetWind() == Vector3.zero ? Quaternion.identity : Quaternion.LookRotation(windArea.GetWind(), Vector3.up);
            windArrow = Instantiate(Resources.Load<GameObject>("Debug/Wind/WindArrow"), windArea.transform.position, rotation);
        }

        windArrow.transform.parent = transform.parent; //WindArea should have an empty holder parent; set the wind arrow's parent to it also 

    }
}
