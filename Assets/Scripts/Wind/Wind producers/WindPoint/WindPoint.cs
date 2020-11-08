using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wind
{

    /// <summary>
    /// Represents a single point of wind in space, which will be added to the wind field at the given depth
    /// </summary>
    [ExecuteInEditMode]
    public class WindPoint : WindProducer
    {
        public bool useRotationAsWindVector = true;
        public Vector3 wind;
        public float windStrength = 1;

        //dirty flags
        private bool windDirty = false;
        private Vector3 lastWindVec;

        private void Awake()
        {
            lastWindVec = wind;
        }

        private void Update()
        {
            wind = useRotationAsWindVector ? transform.rotation * Vector3.forward * windStrength : wind * windStrength;

            //update dirty flags
            windDirty = wind != lastWindVec;
            lastWindVec = wind;
        }

        protected override ComputeBuffer CalcWindFieldPoints()
        {
            if (windPointsBuffer != null) windPointsBuffer.Release();
            windPointsBuffer = new ComputeBuffer(1, WindFieldPoint.stride);
            windPointsBuffer.SetData(new WindFieldPoint[1] { new WindFieldPoint(transform.position, wind, mode, priority) });
            return windPointsBuffer;
        }

        protected override void UpdateWindFieldPoints()
        {
            if (mode == WindProducerMode.Dynamic)
            {
                if (windDirty) windPointsBuffer = CalcWindFieldPoints();
            }

        }
    }
}