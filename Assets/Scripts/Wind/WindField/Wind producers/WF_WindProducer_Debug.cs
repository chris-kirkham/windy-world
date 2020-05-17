using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for visual debug of wind producers (WF_WindProducer-derived classes)
/// </summary>
[RequireComponent(typeof(WF_WindProducer))]
[ExecuteInEditMode]
public class WF_WindProducer_Debug : MonoBehaviour
{
    private WF_WindProducer windProducer;
    private GameObject[] windArrows; //wind arrow visualiser models
    private GameObject windArrowContainer; //empty GameObject to be the parent of wind arrows - just to clean up editor
    private GameObject windArrowModel;
    private Vector3 initArrowScale;

    public bool showWindArrows = true;
    public float updateInterval = 0f;
    private bool resetWindArrows = false; //flagged to reset arrows when showWindArrows is disabled

    private void Start()
    {
        windProducer = GetComponent<WF_WindProducer>();

        windArrowModel = Resources.Load<GameObject>("Debug/Wind/WindArrow");
        WF_WindPoint[] windPoints = windProducer.GetWindFieldPoints();
        windArrows = new GameObject[windPoints.Length];
        windArrowContainer = new GameObject(windProducer.ToString() + " debug wind arrows");
        for(int i = 0; i < windPoints.Length; i++)
        {
            windArrows[i] = Instantiate(windArrowModel, windPoints[i].position, Quaternion.LookRotation(windPoints[i].wind));
            windArrows[i].transform.SetParent(windArrowContainer.transform);
        }

        StartCoroutine(UpdateVis());
    }

    private IEnumerator UpdateVis()
    {
        while(showWindArrows)
        {
            WF_WindPoint[] windPointsUpdated = windProducer.GetWindFieldPoints();
            
            //this should only happen when showWindArrows has been disabled then re-enabled, or if the wind producer changes its number of wind points during gameplay. 
            //If the latter happens often, maybe write a more efficient way of doing this (e.g. using lists instead of arrays) 
            if (resetWindArrows || windPointsUpdated.Length != windArrows.Length)
            {
                foreach (GameObject arrow in windArrows) Destroy(arrow);
                windArrows = new GameObject[windPointsUpdated.Length];
                for (int i = 0; i < windPointsUpdated.Length; i++)
                {
                    windArrows[i] = Instantiate(windArrowModel, windPointsUpdated[i].position, Quaternion.LookRotation(windPointsUpdated[i].wind));
                }

                resetWindArrows = false;
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
        resetWindArrows = true;
        yield return new WaitForSecondsRealtime(updateInterval);
    }

}
