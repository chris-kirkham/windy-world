using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Debug visualiser for WindArea.
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
        windArrow = Instantiate<GameObject>(Resources.Load<GameObject>("Debug/Wind/WindArrow"), windArea.transform.position, Quaternion.LookRotation(windArea.GetWind(), Vector3.up));
        initArrowScale = windArrow.transform.localScale;
    }

    // Update is called once per frame
    void Update()
    {

        windArrow.transform.position = windArea.transform.position;
        windArrow.transform.rotation = Quaternion.LookRotation(windArea.GetWind(), Vector3.up);
        Vector3 scale = transform.localScale;
        windArrow.transform.localScale = new Vector3(initArrowScale.x * scale.x, initArrowScale.y * scale.y, initArrowScale.z * scale.z);
    }

    private void OnDestroy()
    {
        DestroyImmediate(windArrow);
    }
}
