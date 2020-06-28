using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for visual debug of wind producers (WF_WindProducer-derived classes)
/// </summary>
[RequireComponent(typeof(WF_WindProducer))]
//[ExecuteInEditMode]
public class WF_WindProducer_Debug : MonoBehaviour
{
    private WF_WindProducer windProducer;
    private WF_WindPoint[] windPoints;

    public bool showWindArrows = true;
    public float updateInterval = 0f;

    private void Start()
    {
        windProducer = GetComponent<WF_WindProducer>();
        Debug.Log("windProducer = " + windProducer);
        windPoints = windProducer.GetWindFieldPoints();
        StartCoroutine(UpdateVis());
    }

    private void Update()
    {
        if(showWindArrows)
        {
            foreach (WF_WindPoint wp in windPoints)
            {
                Debug.DrawRay(wp.position, wp.wind, Color.white);
            }
        }
    }

    private IEnumerator UpdateVis()
    {
        while(showWindArrows)
        {
            windPoints = windProducer.GetWindFieldPoints();
            yield return new WaitForSecondsRealtime(updateInterval);
        }

        yield return new WaitForSecondsRealtime(updateInterval);
    }

}
