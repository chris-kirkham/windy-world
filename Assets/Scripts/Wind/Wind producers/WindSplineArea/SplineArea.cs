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

        public int numCellsX = 1;
        public int numCellsY = 1;
        [Min(0)] public int windSamplesPerCurve;

        //compute
        public ComputeShader splineAreaCompute;
        private int splineAreaComputeHandle;
        private ComputeBuffer windPointsBuffer;
        private const int windPointsBufferStride = (sizeof(float) * 6) + sizeof(uint) + (sizeof(int) * 2);

        private int GROUP_SIZE = 64;

        // Start is called before the first frame update
        protected override void Start()
        {
            spline = GetComponent<BezierSpline>();

            splineAreaComputeHandle = splineAreaCompute.FindKernel("CSMain");

            base.Start();
        }

        protected override WindFieldPoint[] CalcWindFieldPoints()
        {
            int numCellsZ = spline.curves.Count * windSamplesPerCurve;

            windPointsBuffer.Release();
            windPointsBuffer = new ComputeBuffer(numCellsX * numCellsY * numCellsZ, windPointsBufferStride, ComputeBufferType.Default);


            List<Vector3> positions = new List<Vector3>(numCellsZ);
            List<Vector3> windDirs = new List<Vector3>(numCellsZ);
            List<Vector3> upDirs = new List<Vector3>(numCellsZ);
            List<Vector3> rightDirs = new List<Vector3>(numCellsZ);

            int splineBuffersStride = sizeof(float) * 3; //all these buffers hold float3s
            ComputeBuffer positionsBuffer = new ComputeBuffer(numCellsZ, splineBuffersStride, ComputeBufferType.Append);
            ComputeBuffer windDirsBuffer = new ComputeBuffer(numCellsZ, splineBuffersStride);
            ComputeBuffer upDirsBuffer = new ComputeBuffer(numCellsZ, splineBuffersStride);
            ComputeBuffer rightDirsBuffer = new ComputeBuffer(numCellsZ, splineBuffersStride);


            float sampleInc = 1f / windSamplesPerCurve;
            for (int i = 0; i < spline.curves.Count; i++)
            {
                for (float t = 0; t < 1; t += sampleInc)
                {
                    Vector3 pos = spline.GetWorldPoint(i, t);
                    Vector3 windDir = spline.GetWorldDir(i, t);
                    Vector3 right = -Vector3.Cross(windDir, Vector3.up).normalized;
                    Vector3 up = Vector3.Cross(windDir, right).normalized;

                    positions.Add(pos);
                    windDirs.Add(windDir);
                    rightDirs.Add(right);
                    upDirs.Add(up);

                    Debug.DrawRay(pos, right, Color.red);
                    Debug.DrawRay(pos, up, Color.green);
                }
            }

            positionsBuffer.SetData(positions);
            windDirsBuffer.SetData(windDirs);
            rightDirsBuffer.SetData(rightDirs);
            upDirsBuffer.SetData(upDirs);

            //set compute stuff

            splineAreaCompute.SetBuffer(splineAreaComputeHandle, "Result", windPointsBuffer);
            splineAreaCompute.SetBuffer(splineAreaComputeHandle, "positions", positionsBuffer);
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
            positionsBuffer.Release();
            windDirsBuffer.Release();
            rightDirsBuffer.Release();
            upDirsBuffer.Release();
            
            return new WindFieldPoint[0];
        }

        protected override void UpdateWindFieldPoints()
        {
        }

        // Update is called once per frame
        void Update()
        {
            CalcWindFieldPoints();
        }

    }
}