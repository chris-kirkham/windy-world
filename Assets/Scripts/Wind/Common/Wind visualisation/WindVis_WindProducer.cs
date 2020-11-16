using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Wind
{
    [RequireComponent(typeof(WindProducer))]
    [ExecuteAlways]
    public class WindVis_WindProducer : WindVis
    {
        private WindProducer windProducer;

        void Awake()
        {
            windProducer = GetComponent<WindProducer>();
        }

        private void OnDrawGizmos()
        {
            if(displayWindArrows) DrawWindPoints(windProducer.GetWindFieldPointsBuffer());
        }
    }
}