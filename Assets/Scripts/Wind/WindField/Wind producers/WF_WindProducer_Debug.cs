using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Abstract base class for visual debug of wind producers (WF_WindProducer-derived classes)
/// </summary>
[RequireComponent(typeof(WF_WindProducer))]
[ExecuteInEditMode]
public abstract class WF_WindProducer_Debug : MonoBehaviour
{
    private WF_WindProducer windProducer;
    private GameObject[] windArrows; //wind arrow visualiser models
    private GameObject windArrowModel;
    private Vector3 initArrowScale;

    public bool showWindArrows = true;
    public float updateInterval = 0f;

    private void Start()
    {
        windProducer = GetComponent<WF_WindProducer>();

        windArrowModel = Resources.Load<GameObject>("Debug/Wind/WindArrow");
        WF_WindPoint[] windPoints = windProducer.GetWindFieldPoints();
        windArrows = new GameObject[windPoints.Length];
        for(int i = 0; i < windPoints.Length; i++)
        {
            windArrows[i] = Instantiate(windArrowModel, windPoints[i].position, Quaternion.LookRotation(windPoints[i].wind));
        }

        StartCoroutine(UpdateVis());
    }

    private IEnumerator UpdateVis()
    {
        while(showWindArrows)
        {
            WF_WindPoint[] windPointsUpdated = windProducer.GetWindFieldPoints();
            
            //this probably won't happen (often), but if it turns out to, maybe write a more efficient way of doing this (e.g. using lists instead of arrays) 
            if (windPointsUpdated.Length != windArrows.Length)
            {
                foreach (GameObject arrow in windArrows) Destroy(arrow);
                windArrows = new GameObject[windPointsUpdated.Length];
                for (int i = 0; i < windPointsUpdated.Length; i++)
                {
                    windArrows[i] = Instantiate(windArrowModel, windPointsUpdated[i].position, Quaternion.LookRotation(windPointsUpdated[i].wind));
                }
            }
            
            for (int i = 0; i < windPointsUpdated.Length; i++)
            {
                WF_WindPoint wp = windPointsUpdated[i];
                windArrows[i].transform.position = wp.position;
                if (wp.wind != Vector3.zero) windArrows[i].transform.rotation = Quaternion.LookRotation(wp.wind);
            }

            yield return new WaitForSecondsRealtime(updateInterval);
        }

        foreach (GameObject arrow in windArrows) Destroy(arrow);
        yield return new WaitForSecondsRealtime(updateInterval);
    }

}
