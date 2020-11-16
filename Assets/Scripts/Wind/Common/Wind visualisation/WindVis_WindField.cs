using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Wind
{
    [RequireComponent(typeof(ComputeWindField))]
    public class WindVis_WindField : WindVis
    {
        private ComputeWindField windField;

        //compute shader to convert wind field (3D textures + global wind) into a 1D buffer of wind values 
        public ComputeShader windFieldTo1DCompute;
        private int kernel;
        private uint[] groupSizes;

        private ComputeBuffer windField1DBuffer;
        private int bufferStride = WindFieldPoint.stride;

        void Start()
        {
            windField = GetComponent<ComputeWindField>();
            Vector3Int windFieldNumCells = windField.GetNumCells();
            windField1DBuffer = new ComputeBuffer(windFieldNumCells.x * windFieldNumCells.y * windFieldNumCells.z, bufferStride);

            kernel = windFieldTo1DCompute.FindKernel("WindFieldTo1DBuffer");
            groupSizes = new uint[3];
            windFieldTo1DCompute.GetKernelThreadGroupSizes(kernel, out groupSizes[0], out groupSizes[1], out groupSizes[2]);
        }

        private void Update()
        {
            if (EditorApplication.isPlaying && displayWindArrows)
            {
                UpdateWindFieldBuffer();
                DrawWindPoints(windField1DBuffer);
            }
        }

        private void OnDisable()
        {
            if (windField1DBuffer != null) windField1DBuffer.Release();
        }

        //identical to drawing wind points for wind producers, but set the wind field cell size in the shader (affects wind arrow scale)
        protected override void DrawWindPoints(ComputeBuffer windPoints)
        {
            thisObjMaterial.SetFloat("_WindFieldCellSize", windField.GetCellSize());
            base.DrawWindPoints(windPoints);
        }

        private void UpdateWindFieldBuffer()
        {
            Vector3Int windFieldNumCells = windField.GetNumCells();

            if (windField1DBuffer != null) windField1DBuffer.Release();
            windField1DBuffer = new ComputeBuffer(windFieldNumCells.x * windFieldNumCells.y * windFieldNumCells.z, bufferStride);
            windFieldTo1DCompute.SetBuffer(kernel, "Result", windField1DBuffer);

            windFieldTo1DCompute.SetTexture(kernel, "windFieldStatic", windField.GetStaticWindField());
            windFieldTo1DCompute.SetTexture(kernel, "windFieldDynamic", windField.GetDynamicWindField());
            windFieldTo1DCompute.SetTexture(kernel, "windFieldNoise", windField.GetNoiseWindField());
            Vector3 globalWind = windField.GetGlobalWind();
            windFieldTo1DCompute.SetFloats("globalWind", new float[3] { globalWind.x, globalWind.y, globalWind.z });
            windFieldTo1DCompute.SetInt("numCellsX", windFieldNumCells.x);
            windFieldTo1DCompute.SetInt("numCellsY", windFieldNumCells.y);
            windFieldTo1DCompute.SetFloat("cellSize", windField.GetCellSize());
            Vector3 leastCorner = windField.LeastCorner;
            windFieldTo1DCompute.SetFloats("leastCorner", new float[3] { leastCorner.x, leastCorner.y, leastCorner.z });

            /*
            int[] numGroups = new int[3]
            {
                Mathf.Max(1, Mathf.CeilToInt(windFieldNumCells.x / (float)groupSizes[0])),
                Mathf.Max(1, Mathf.CeilToInt(windFieldNumCells.y / (float)groupSizes[1])),
                Mathf.Max(1, Mathf.CeilToInt(windFieldNumCells.z / (float)groupSizes[2]))
            };
            */

            int numGroupsX = Mathf.Max(1, Mathf.CeilToInt((windFieldNumCells.x * windFieldNumCells.y * windFieldNumCells.z) / 64));

            //windFieldTo1DCompute.Dispatch(kernel, numGroups[0], numGroups[1], numGroups[2]);
            windFieldTo1DCompute.Dispatch(kernel, numGroupsX, 1, 1);
        }

    }
}