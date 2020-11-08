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
        private ComputeBuffer wind; //compute buffer of length 1; just to get wind sample out of compute shader
        private int getWindComputeKernel;

        void Start()
        {
            windField = GetComponent<ComputeWindField>();
            
            getWindComputeKernel = getWindCompute.FindKernel("GetWindAtPosition");
        }

        public Vector3 GetWind(Vector3 position)
        {
            //create and set result buffer
            if (wind != null) wind.Release();
            wind = new ComputeBuffer(1, sizeof(float) * 3);
            getWindCompute.SetBuffer(getWindComputeKernel, "Wind", wind);

            //set other variables
            getWindCompute.SetTexture(getWindComputeKernel, "windFieldStatic", windField.GetStaticWindField());
            getWindCompute.SetTexture(getWindComputeKernel, "windFieldDynamic", windField.GetDynamicWindField());
            getWindCompute.SetTexture(getWindComputeKernel, "windFieldNoise", windField.GetNoiseWindField());
            Vector3 globalWind = windField.GetGlobalWind();
            getWindCompute.SetFloats("globalWind", new float[3] { globalWind.x, globalWind.y, globalWind.z });
            getWindCompute.SetFloat("windFieldCellSize", windField.GetCellSize());
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