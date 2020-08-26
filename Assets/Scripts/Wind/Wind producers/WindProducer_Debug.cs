using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wind
{
    /// <summary>
    /// Base class for visual debug of wind producers (WF_WindProducer-derived classes)
    /// </summary>
    [RequireComponent(typeof(WindProducer))]
    //[ExecuteInEditMode]
    public class WindProducer_Debug : MonoBehaviour
    {
        private WindProducer windProducer;
        private WindFieldPoint[] windPoints;

        public bool showWindArrows = true;
        public float updateInterval = 0f;

        private void Start()
        {
            windProducer = GetComponent<WindProducer>();
            Debug.Log("windProducer = " + windProducer);
            windPoints = windProducer.GetWindFieldPoints();
            StartCoroutine(UpdateVis());
        }

        private void Update()
        {
            if (showWindArrows)
            {
                foreach (WindFieldPoint wp in windPoints)
                {
                    Debug.DrawRay(wp.position, wp.wind, Color.white);
                }
            }
        }

        private IEnumerator UpdateVis()
        {
            while (showWindArrows)
            {
                windPoints = windProducer.GetWindFieldPoints();
                yield return new WaitForSecondsRealtime(updateInterval);
            }

            yield return new WaitForSecondsRealtime(updateInterval);
        }

    }
}