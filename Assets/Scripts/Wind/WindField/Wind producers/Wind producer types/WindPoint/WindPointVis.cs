using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Editor/debug visualisation for the WindPoint class
/// </summary>
[RequireComponent(typeof(WindPoint))]
[ExecuteInEditMode]
public class WindPointVis : MonoBehaviour
{
    private WindPoint windPoint;
    //private GameObject pointVisSphere;
    private GameObject windArrow;
    private Vector3 initArrowScale;

    void Start()
    {
        windPoint = GetComponent<WindPoint>();
        if (windArrow == null)
        {
            windArrow = Instantiate(Resources.Load<GameObject>("Debug/Wind/WindArrow"), windPoint.transform.position, Quaternion.LookRotation(windPoint.wind, Vector3.up));
            windArrow.transform.parent = transform.parent; //WindArea should have an empty holder parent; set the wind arrow's parent to it also 
        }

        initArrowScale = windArrow.transform.localScale;
    }

    void Update()
    {
        windArrow.transform.position = windPoint.transform.position;
        if(windPoint.wind != Vector3.zero) windArrow.transform.rotation = Quaternion.LookRotation(windPoint.wind, Vector3.up);
        float cellSize = windPoint.windField.rootCellSize / Mathf.Pow(2, windPoint.depth);
        windArrow.transform.localScale = Vector3.Min(initArrowScale * cellSize, initArrowScale * windPoint.wind.magnitude);
    }

    private void OnDestroy()
    {
        DestroyImmediate(windArrow);
    }
}
