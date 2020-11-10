using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wind
{
    [RequireComponent(typeof(WindProducer))]
    [ExecuteAlways]
    public class WindVis_WindProducer : WindVis
    {
        private WindProducer windProducer;

        void Start()
        {
            windProducer = GetComponent<WindProducer>();
        }

        private void OnDrawGizmos()
        {
            DrawWindPoints(windProducer.GetWindFieldPointsBuffer());
        }
    }
}