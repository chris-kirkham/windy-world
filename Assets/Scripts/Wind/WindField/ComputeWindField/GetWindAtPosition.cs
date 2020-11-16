using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wind
{
    [RequireComponent(typeof(ComputeWindField))] //main wind field script
    public class GetWindAtPosition : MonoBehaviour
    {
        private ComputeWindField windField;
        
        private ComputeShader getWindCompute;
        private ComputeBuffer wind;
        private int getWindKernelSinglePos;
        private int getWindKernelPosArray;

        void Awake()
        {
            windField = GetComponent<ComputeWindField>();

            getWindCompute = Resources.Load<ComputeShader>("GetWindAtPosition");
            getWindKernelSinglePos = getWindCompute.FindKernel("GetWindAtPosition");
            getWindKernelPosArray = getWindCompute.FindKernel("GetWindAtPositions");
        }

        private void OnDestroy()
        {
            if (wind != null) wind.Release();
        }

        //Get wind vector at a single position
        public Vector3 GetWind(Vector3 position)
        {
            //create and set result buffer
            if (wind != null) wind.Release();
            wind = new ComputeBuffer(1, sizeof(float) * 3);
            getWindCompute.SetBuffer(getWindKernelSinglePos, "Wind", wind);

            //set other variables
            getWindCompute.SetTexture(getWindKernelSinglePos, "windFieldStatic", windField.GetStaticWindField());
            getWindCompute.SetTexture(getWindKernelSinglePos, "windFieldDynamic", windField.GetDynamicWindField());
            getWindCompute.SetTexture(getWindKernelSinglePos, "windFieldNoise", windField.GetNoiseWindField());
            Vector3 globalWind = windField.GetGlobalWind();
            getWindCompute.SetFloats("globalWind", new float[3] { globalWind.x, globalWind.y, globalWind.z });
            getWindCompute.SetFloat("windFieldCellSize", windField.GetCellSize());
            Vector3 leastCorner = windField.LeastCorner;
            getWindCompute.SetFloats("windFieldStart", new float[3] { leastCorner.x, leastCorner.y, leastCorner.z });
            getWindCompute.SetFloats("samplePosition", new float[3] { position.x, position.y, position.z });

            //dispatch shader
            getWindCompute.Dispatch(getWindKernelSinglePos, 1, 1, 1);

            //need to send buffer data to an array in order to return a Vector3
            Vector3[] windArr = new Vector3[1]; 
            wind.GetData(windArr);
            return windArr[0];
        }

        //Get array of wind vectors from array of positions; saves overhead from setting up/dispatching compute shader multiple times when getting
        //wind from lots of positions
        public Vector3[] GetWind(Vector3[] positions)
        {
            //create and set result buffer
            if (wind != null) wind.Release();
            wind = new ComputeBuffer(positions.Length, sizeof(float) * 3);
            getWindCompute.SetBuffer(getWindKernelPosArray, "Wind", wind);

            //set other variables
            getWindCompute.SetTexture(getWindKernelPosArray, "windFieldStatic", windField.GetStaticWindField());
            getWindCompute.SetTexture(getWindKernelPosArray, "windFieldDynamic", windField.GetDynamicWindField());
            getWindCompute.SetTexture(getWindKernelPosArray, "windFieldNoise", windField.GetNoiseWindField());
            Vector3 globalWind = windField.GetGlobalWind();
            getWindCompute.SetFloats("globalWind", new float[3] { globalWind.x, globalWind.y, globalWind.z });
            getWindCompute.SetFloat("windFieldCellSize", windField.GetCellSize());
            Vector3 leastCorner = windField.LeastCorner;
            getWindCompute.SetFloats("windFieldStart", new float[3] { leastCorner.x, leastCorner.y, leastCorner.z });
            ComputeBuffer samplePositionsBuffer = new ComputeBuffer(positions.Length, sizeof(float) * 3);
            samplePositionsBuffer.SetData(positions);
            getWindCompute.SetBuffer(getWindKernelPosArray, "samplePositions", samplePositionsBuffer);

            //dispatch shader
            getWindCompute.Dispatch(getWindKernelSinglePos, 1, 1, 1);
            
            //send buffer data back to array
            Vector3[] windArr = new Vector3[positions.Length];
            wind.GetData(windArr);
            return windArr;
        }
    }
}