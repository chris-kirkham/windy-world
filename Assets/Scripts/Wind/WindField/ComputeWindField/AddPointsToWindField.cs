using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wind
{
    /// <summary>
    /// Script to add wind points to the attached wind field component via a compute shader
    /// </summary>
    [RequireComponent(typeof(ComputeWindField))] //main wind field script
    public class AddPointsToWindField : MonoBehaviour
    {
        private ComputeWindField windField;

        public ComputeShader addPointsCompute;
        private int addPointsKernel;
        private const int GROUP_SIZE = 64; //same as in compute shader

        public void Start()
        {
            windField = GetComponent<ComputeWindField>();

            addPointsKernel = addPointsCompute.FindKernel("AddPointsToWindField");
        }

        //public RenderTexture AddPoints(ComputeBuffer windPoints, RenderTexture windField)
        public void AddPoints(ComputeBuffer windPoints)
        {
            int numGroups = Mathf.CeilToInt((float)windPoints.count / GROUP_SIZE); //always round numGroups up so no points get missed out?
            if (numGroups <= 0) numGroups = 0;

            addPointsCompute.SetTexture(addPointsKernel, "WindField", windField.GetWindFieldRenderTexture());
            addPointsCompute.SetBuffer(addPointsKernel, "windPoints", windPoints);
            addPointsCompute.SetFloat("cellSize", windField.cellSize);
            Vector3 leastCorner = windField.WindFieldLeastCorner;
            addPointsCompute.SetFloats("windFieldStartPos", new float[3] { leastCorner.x, leastCorner.y, leastCorner.z });

            addPointsCompute.Dispatch(addPointsKernel, numGroups, 1, 1);

            //return windField;
        }
    }
}