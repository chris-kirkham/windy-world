using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ComputeTestScript : MonoBehaviour
{
    public ComputeShader compute;
    public int texResolution;

    private Renderer r;
    private RenderTexture rt;

    private void Start()
    {
        rt = new RenderTexture(texResolution, texResolution, 24);
        rt.enableRandomWrite = true;
        rt.Create();

        r = GetComponent<Renderer>();
        r.enabled = true;

        UpdateTextureFromCompute();
    }

    private void Update()
    {
        UpdateTextureFromCompute();
    }

    private void UpdateTextureFromCompute()
    {
        int kernelHandle = compute.FindKernel("CSMain");
        compute.SetInt("Offset", (int)(Time.time * 100));

        compute.SetTexture(kernelHandle, "Result", rt);
        int numThreadsXY = Mathf.Min(texResolution / 8, 1024);
        compute.Dispatch(kernelHandle, numThreadsXY, numThreadsXY, 1);

        r.material.SetTexture("_MainTex", rt);
    }

}
