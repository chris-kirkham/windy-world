﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        private int bufferStride = sizeof(float) * 3; 

        void Start()
        {
            windField = GetComponent<ComputeWindField>();

            kernel = windFieldTo1DCompute.FindKernel("WindFieldTo1DBuffer");
            groupSizes = new uint[3];
            windFieldTo1DCompute.GetKernelThreadGroupSizes(kernel, out groupSizes[0], out groupSizes[1], out groupSizes[2]);
        }

        void Update()
        {
            UpdateWindFieldBuffer();
            DrawWindPoints(windField1DBuffer);
        }

        private void OnDisable()
        {
            if (windField1DBuffer != null) windField1DBuffer.Release();
        }

        private void UpdateWindFieldBuffer()
        {
            Vector3Int windFieldNumCells = windField.GetNumCells();

            if (windField1DBuffer != null) windField1DBuffer.Release();
            windField1DBuffer = new ComputeBuffer(windFieldNumCells.x * windFieldNumCells.y * windFieldNumCells.z, bufferStride);

            windFieldTo1DCompute.SetTexture(kernel, "windFieldStatic", windField.GetStaticWindField());
            windFieldTo1DCompute.SetTexture(kernel, "windFieldDynamic", windField.GetDynamicWindField());
            windFieldTo1DCompute.SetTexture(kernel, "windFieldNoise", windField.GetNoiseWindField());
            Vector3 globalWind = windField.GetGlobalWind();
            windFieldTo1DCompute.SetFloats("globalWind", new float[3] { globalWind.x, globalWind.y, globalWind.z });
            windFieldTo1DCompute.SetInt("numCellsX", windFieldNumCells.x);
            windFieldTo1DCompute.SetInt("numCellsY", windFieldNumCells.y);

            int[] numGroups = new int[3]
            {
                Mathf.Max(1, Mathf.CeilToInt(windFieldNumCells.x / (float)groupSizes[0])),
                Mathf.Max(1, Mathf.CeilToInt(windFieldNumCells.y / (float)groupSizes[1])),
                Mathf.Max(1, Mathf.CeilToInt(windFieldNumCells.z / (float)groupSizes[2]))
            };

            windFieldTo1DCompute.Dispatch(kernel, numGroups[0], numGroups[1], numGroups[2]);
        }
    }
}