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
        windArrow = transform.Find("WindArrow").gameObject;
        if (windArrow == null)
        {
            Debug.Log("WindPointVis: WindArrow child cannot be found. Did you delete it?");
            windArrow = Instantiate(Resources.Load<GameObject>("Debug/Wind/WindArrow"), windPoint.transform.position, Quaternion.LookRotation(windPoint.wind, Vector3.up));
            windArrow.transform.parent = transform.parent;
        }

        initArrowScale = windArrow.transform.localScale;
    }

    void Update()
    {
        windArrow.transform.position = windPoint.transform.position;
        if(windPoint.wind != Vector3.zero) windArrow.transform.rotation = Quaternion.LookRotation(windPoint.wind, Vector3.up);
        float cellSize = windPoint.windField.GetCellSize() / Mathf.Pow(2, windPoint.depth);
        //windArrow.transform.localScale = Vector3.Min(initArrowScale * cellSize, initArrowScale * windPoint.wind.magnitude);
    }

    private void OnDestroy()
    {
        DestroyImmediate(windArrow);
    }
}
