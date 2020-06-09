using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class WindArea : WF_WindProducer
{
    public Vector3 wind; //wind vector settable in editor. This will either be the local or world direction, depending on if using local
    private Vector3 windWorld; //world space wind; if not using windRelativeToLocalRotation, this will be the same as wind
    public bool relativeToLocalRotation = false;
    public Vector3Int numCells = new Vector3Int(2, 2, 2);
    private const int maxCellsX = 64;
    private const int maxCellsY = 64;
    private const int maxCellsZ = 64;
    private const int maxTotalCells = maxCellsX * maxCellsY * maxCellsZ;

    public ComputeShader calcWindPointsShader;
    private ComputeBuffer windPoints; //stores the wind points output by the compute shader

    void Awake()
    {
        windWorld = wind;
        windPoints = new ComputeBuffer(numCells.x * numCells.y * numCells.z, sizeof(float) * 9, ComputeBufferType.Append);
    }

    void Update()
    {
        windWorld = transform.TransformDirection(wind);
    }

    private void OnValidate()
    {
        if (numCells.x > maxCellsX)
        {
            Debug.LogError("numCells.x > maxCellsX! (" + numCells.x + ", max " + maxCellsX + "). Clamping");
            numCells.x = maxCellsX;
        }

        if (numCells.y > maxCellsY)
        {
            Debug.LogError("numCells.y > maxCellsY! (" + numCells.y + ", max " + maxCellsY + "). Clamping");
            numCells.y = maxCellsY;
        }

        if (numCells.z > maxCellsZ)
        {
            Debug.LogError("numCells.z > maxCellsZ! (" + numCells.z + ", max " + maxCellsZ + "). Clamping");
            numCells.z = maxCellsZ;
        }
    }

    /*----GETTERS AND SETTERS----*/
    //Gets world space wind vector
    public Vector3 GetWind()
    {
        return relativeToLocalRotation ? windWorld : wind;
    }

    protected override WF_WindPoint[] CalcWindFieldPoints()
    {
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        
        /*
        //(SINGLE-THREAD) CPU METHOD
        List<WF_WindPoint> points = new List<WF_WindPoint>();
        
        Vector3 halfNumCells = new Vector3((float)numCells.x / 2, (float)numCells.y / 2, (float)numCells.z / 2);
        Vector3 start = -(halfNumCells * cellSize);
        Vector3 end = halfNumCells * cellSize;
        float halfCellSize = cellSize / 2;

        for (float x = start.x + halfCellSize; x <= end.x; x += cellSize)
        {
            for (float y = start.y + halfCellSize; y <= end.y; y += cellSize)
            {
                for (float z = start.z + halfCellSize; z <= end.z; z += cellSize)
                {
                    Vector3 pos = transform.position + transform.TransformDirection(new Vector3(x, y, z));
                    points.Add(new WF_WindPoint(pos, GetWind(), mode, priority, depth));
                }
            }
        }
        */

        //COMPUTE SHADER METHOD
        WF_WindPoint[] points = new WF_WindPoint[windPoints.count];

        //https://forum.unity.com/threads/system-says-struct-is-blittable-unity-says-struct-isnt-blittable.590251/
        //https://forum.unity.com/threads/computebuffer-getdata.165501/
        //windPoints.SetData(new WF_WindPoint[maxTotalCells]);

        float[] start = new float[3] { ((float)numCells.x / 2) * cellSize, ((float)numCells.y / 2) * cellSize, ((float)numCells.z / 2) * cellSize };
        Debug.Log(string.Join(", ", start));
        calcWindPointsShader.SetFloats("startPos", start);
        calcWindPointsShader.SetFloats("right", new float[3] { transform.right.x, transform.right.y, transform.right.z } );
        calcWindPointsShader.SetFloats("up", new float[3] { transform.up.x, transform.up.y, transform.up.z } );
        calcWindPointsShader.SetFloats("fwd", new float[3] { transform.forward.x, transform.forward.y, transform.forward.z } );
        calcWindPointsShader.SetInts("numPoints", new int[3] { numCells.x, numCells.y, numCells.z } );
        calcWindPointsShader.SetFloats("windDir", new float[3] { wind.x, wind.y, wind.z } );

        int kernelHandle = calcWindPointsShader.FindKernel("CalcWindPoints");
        calcWindPointsShader.SetBuffer(kernelHandle, "Result", windPoints);
        calcWindPointsShader.Dispatch(kernelHandle, numCells.x, numCells.y, numCells.z);
        windPoints.GetData(points);
        
        
        stopwatch.Stop();
        Debug.Log("WindArea update time for " + numCells.x * numCells.y * numCells.z + " cells : " + stopwatch.ElapsedMilliseconds + " milliseconds");

        foreach (WF_WindPoint wp in points)
        {
            Debug.DrawRay(wp.position, wp.wind, Color.green);
        }

        return points;
    }

    private void OnDestroy()
    {
        windPoints.Release();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, GetWind());
    }


}
