using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wind
{
    [RequireComponent(typeof(ComputeWindField))] //main wind field script
    public class GetWindAtPosition : MonoBehaviour
    {
        private ComputeWindField windField;
        
        public ComputeShader getWindCompute;
        private int getWindComputeKernel;

        void Start()
        {
            windField = GetComponent<ComputeWindField>();
            
            getWindComputeKernel = getWindCompute.FindKernel("GetWindAtPosition");
        }

        public Vector3 GetWind(Vector3 position)
        {
            //create and set result buffer
            ComputeBuffer wind = new ComputeBuffer(1, sizeof(float) * 3);
            getWindCompute.SetBuffer(getWindComputeKernel, "Wind", wind);

            //set other variables
            getWindCompute.SetTexture(getWindComputeKernel, "windField", windField.GetWindFieldRenderTexture());
            getWindCompute.SetFloat("windFieldCellSize", windField.cellSize);
            Vector3 leastCorner = windField.WindFieldLeastCorner;
            getWindCompute.SetFloats("windFieldStart", new float[3] { leastCorner.x, leastCorner.y, leastCorner.z });
            getWindCompute.SetFloats("samplePosition", new float[3] { position.x, position.y, position.z });

            //dispatch shader
            getWindCompute.Dispatch(getWindComputeKernel, 1, 1, 1);

            //need to send buffer data to an array in order to return a Vector3
            Vector3[] windArr = new Vector3[1]; 
            wind.GetData(windArr);
            return windArr[0];
        }
    }
}