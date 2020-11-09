using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Wind
{
    [RequireComponent(typeof(BezierSpline))]
    public class SplineArea : WindProducer
    {
        private BezierSpline spline;

        [SerializeField] [Min(1)] private int numCellsX = 1;
        [SerializeField] [Min(1)] private int numCellsY = 1;
        private int numCellsZ; //windSamplesPerCurve * number of curves in spline
        [SerializeField] [Min(1)] private int windSamplesPerCurve = 10;
        [SerializeField] [Min(0)] private float cellSizeXY = 1;

        //compute
        public ComputeShader splineAreaCompute;
        private int splineAreaComputeHandle;

        private int GROUP_SIZE = 64;

        protected override void OnEnable()
        {
            spline = GetComponent<BezierSpline>();

            numCellsZ = spline.curves.Count * windSamplesPerCurve;
            splineAreaComputeHandle = splineAreaCompute.FindKernel("CalcWindPoints");
            windPointsBuffer = new ComputeBuffer(numCellsX * numCellsY * numCellsZ, WindFieldPoint.stride, ComputeBufferType.Default);

            base.OnEnable();
        }

        protected override void OnValidate()
        {
            spline = GetComponent<BezierSpline>();
            base.OnValidate();
        }

        protected override ComputeBuffer CalcWindFieldPoints()
        {
            int numCellsZ = spline.curves.Count * windSamplesPerCurve;
            
            if(windPointsBuffer != null) windPointsBuffer.Release();
            windPointsBuffer = new ComputeBuffer(numCellsX * numCellsY * numCellsZ, WindFieldPoint.stride, ComputeBufferType.Default);

            List<Vector3> startPositions = new List<Vector3>(numCellsZ);
            List<Vector3> windDirs = new List<Vector3>(numCellsZ);
            List<Vector3> upDirs = new List<Vector3>(numCellsZ);
            List<Vector3> rightDirs = new List<Vector3>(numCellsZ);

            int splineBuffersStride = sizeof(float) * 3; //all these buffers hold float3s
            ComputeBuffer startPositionsBuffer = new ComputeBuffer(numCellsZ, splineBuffersStride);
            ComputeBuffer windDirsBuffer = new ComputeBuffer(numCellsZ, splineBuffersStride);
            ComputeBuffer upDirsBuffer = new ComputeBuffer(numCellsZ, splineBuffersStride);
            ComputeBuffer rightDirsBuffer = new ComputeBuffer(numCellsZ, splineBuffersStride);

            //lengthX and lengthY are used to calculate startPos (the position of the least cell) for each curve sample
            float lengthX = ((numCellsX / 2f) * cellSizeXY) - (cellSizeXY / 2);
            float lengthY = ((numCellsY / 2f) * cellSizeXY) - (cellSizeXY / 2);
            float sampleInc = 1f / windSamplesPerCurve;
            for (int i = 0; i < spline.curves.Count; i++)
            {
                for (float t = 0; t < 1; t += sampleInc)
                {
                    Vector3 windDir = spline.GetWorldDir(i, t);
                    Vector3 right = -Vector3.Cross(windDir, Vector3.up).normalized;
                    Vector3 up = Vector3.Cross(windDir, right).normalized;
                    Vector3 startPos = spline.GetWorldPoint(i, t) - ((right * lengthX) + (up * lengthY));

                    windDirs.Add(windDir);
                    rightDirs.Add(right * cellSizeXY);
                    upDirs.Add(up * cellSizeXY);
                    startPositions.Add(startPos);
                    
                    //debug up/right vector drawing
                    //Debug.DrawRay(spline.GetWorldPoint(i, t), right, Color.red);
                    //Debug.DrawRay(spline.GetWorldPoint(i, t), up, Color.green);
                }
            }

            startPositionsBuffer.SetData(startPositions);
            windDirsBuffer.SetData(windDirs);
            rightDirsBuffer.SetData(rightDirs);
            upDirsBuffer.SetData(upDirs);

            //set compute stuff
            splineAreaCompute.SetBuffer(splineAreaComputeHandle, "Result", windPointsBuffer);
            splineAreaCompute.SetBuffer(splineAreaComputeHandle, "positions", startPositionsBuffer);
            splineAreaCompute.SetBuffer(splineAreaComputeHandle, "windDirs", windDirsBuffer);
            splineAreaCompute.SetBuffer(splineAreaComputeHandle, "rightDirs", rightDirsBuffer);
            splineAreaCompute.SetBuffer(splineAreaComputeHandle, "upDirs", upDirsBuffer);
            splineAreaCompute.SetInt("numCellsX", numCellsX);
            splineAreaCompute.SetInt("numCellsY", numCellsY);
            splineAreaCompute.SetFloat("cellSizeXY", cellSize);
            splineAreaCompute.SetFloat("halfCellSizeXY", cellSize / 2);

            //dispatch shader
            int numGroups = (numCellsX * numCellsY * numCellsZ) / GROUP_SIZE;
            if (numGroups <= 0) numGroups = 1;
            splineAreaCompute.Dispatch(splineAreaComputeHandle, numGroups, 1, 1);

            //release buffers
            startPositionsBuffer.Release();
            windDirsBuffer.Release();
            rightDirsBuffer.Release();
            upDirsBuffer.Release();

            return windPointsBuffer;
        }

        protected override void UpdateWindFieldPoints()
        {
            windPointsBuffer = CalcWindFieldPoints();
        }

        // Update is called once per frame
        void Update()
        {
            CalcWindFieldPoints();
        }

    }
}