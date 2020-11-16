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
        private ComputeBuffer windPointsBuffer;

        public bool showWindArrows = true;
        public float updateInterval = 0f;

        private void Start()
        {
            windProducer = GetComponent<WindProducer>();
            Debug.Log("windProducer = " + windProducer);
            windPointsBuffer = windProducer.GetWindFieldPointsBuffer();
            StartCoroutine(UpdateVis());
        }

        private void Update()
        {
            if (showWindArrows)
            {
                //TODO: Graphics.DrawInstancedIndirect wind points
            }
        }

        //Updates wind data for visualisation. Does not draw it (visualisation must be drawn every frame or will get flickering)
        private IEnumerator UpdateVis()
        {
            while (showWindArrows)
            {
                windPointsBuffer = windProducer.GetWindFieldPointsBuffer();
                yield return new WaitForSecondsRealtime(updateInterval);
            }

            yield return new WaitForSecondsRealtime(updateInterval);
        }

    }
}