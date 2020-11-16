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
        private ComputeShader addPointsCompute;
        private int addPointsKernel;
        private uint groupSize; //same as in compute shader

        public void Awake()
        {
            addPointsCompute = Resources.Load<ComputeShader>("AddPointsToWindField");
            addPointsKernel = addPointsCompute.FindKernel("AddPointsToWindField");
            addPointsCompute.GetKernelThreadGroupSizes(addPointsKernel, out groupSize, out _, out _);
        }

        //Adds the given wind points to the given wind field render texture. RT is passed by reference so no need to return it???
        public RenderTexture AddPoints(RenderTexture windFieldRT, Vector3 windFieldLeastCorner, float windFieldCellSize, ComputeBuffer windPoints)
        {
            int numGroups = Mathf.Max(1, Mathf.CeilToInt((float)windPoints.count / groupSize)); //always round numGroups up so no points get missed out; min 1 group

            addPointsCompute.SetTexture(addPointsKernel, "WindField", windFieldRT);
            addPointsCompute.SetBuffer(addPointsKernel, "windPoints", windPoints);
            addPointsCompute.SetFloat("cellSize", windFieldCellSize);
            addPointsCompute.SetFloats("windFieldStartPos", new float[3] { windFieldLeastCorner.x, windFieldLeastCorner.y, windFieldLeastCorner.z });

            addPointsCompute.Dispatch(addPointsKernel, numGroups, 1, 1);
            
            return windFieldRT;
        }
    }
}