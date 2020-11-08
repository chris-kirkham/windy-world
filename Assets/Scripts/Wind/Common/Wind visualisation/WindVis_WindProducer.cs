using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wind
{
    [RequireComponent(typeof(WindProducer))]
    public class WindVis_WindProducer : WindVis
    {
        WindProducer windProducer;

        ComputeBuffer windProducerPointsBuffer;

        void Start()
        {
            windProducer = GetComponent<WindProducer>();
        }

        void Update()
        {
            DrawWindPoints(windProducer.GetProducerPointsAsBuffer());
        }
    }
}