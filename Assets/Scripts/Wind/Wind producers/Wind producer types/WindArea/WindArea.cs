using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Wind;

[ExecuteInEditMode]
public class WindArea : WindProducer
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
    //private ComputeBuffer windPoints; //stores the wind points output by the compute shader

    //dirty flags
    private bool windDirty = false;
    private Vector3 lastWindVec;
    private bool numCellsDirty = false;
    private Vector3Int lastNumCells;

    void Awake()
    {
        windWorld = wind;
        //windPoints = new ComputeBuffer(numCells.x * numCells.y * numCells.z, sizeof(float) * 9, ComputeBufferType.Append);

        lastWindVec = GetWind();
        lastNumCells = numCells;
    }

    void Update()
    {
        windWorld = relativeToLocalRotation ? transform.TransformDirection(wind) : wind;

        //update dirty flags
        windDirty = GetWind() != lastWindVec;
        numCellsDirty = numCells != lastNumCells;

        lastWindVec = GetWind();
        lastNumCells = numCells;

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

    protected override WindFieldPoint[] CalcWindFieldPoints()
    {
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        //(SINGLE-THREAD) CPU METHOD
        List<WindFieldPoint> points = new List<WindFieldPoint>();
        
        Vector3 halfNumCells = new Vector3((float)numCells.x / 2, (float)numCells.y / 2, (float)numCells.z / 2);
        float halfCellSize = cellSize / 2;
        Vector3 start = transform.TransformPoint(-(halfNumCells * cellSize) + (Vector3.one * halfCellSize)); //start = centre of least cell
        Vector3 right = transform.right * cellSize;
        Vector3 up = transform.up * cellSize;
        Vector3 forward = transform.forward * cellSize;

        for (int i = 0; i < numCells.x; i++)
        {
            for (int j = 0; j < numCells.y; j++)
            {
                for (int k = 0; k < numCells.z; k++)
                {
                    Vector3 pos = start + (right * i) + (up * j) + (forward * k);
                    points.Add(new WindFieldPoint(pos, windWorld, mode, priority, depth));
                }
            }
        }

        //COMPUTE SHADER METHOD
        /*
        WF_WindPoint[] points = new Wind.WF_WindPoint[windPoints.count];

        //https://forum.unity.com/threads/system-says-struct-is-blittable-unity-says-struct-isnt-blittable.590251/
        //https://forum.unity.com/threads/computebuffer-getdata.165501/
        //windPoints.SetData(new WF_WindPoint[maxTotalCells]);

        float[] start = new float[3] { ((float)numCells.x / 2) * cellSize, ((float)numCells.y / 2) * cellSize, ((float)numCells.z / 2) * cellSize };
        Debug.Log(string.Join(", ", start));
        calcWindPointsShader.SetFloats("startPos", start);
        calcWindPointsShader.SetFloats("right", new float[3] { transform.right.x * cellSize, transform.right.y * cellSize, transform.right.z * cellSize } );
        calcWindPointsShader.SetFloats("up", new float[3] { transform.up.x * cellSize, transform.up.y * cellSize, transform.up.z * cellSize } );
        calcWindPointsShader.SetFloats("fwd", new float[3] { transform.forward.x * cellSize, transform.forward.y * cellSize, transform.forward.z * cellSize } );
        calcWindPointsShader.SetInts("numPoints", new int[3] { numCells.x, numCells.y, numCells.z } );
        calcWindPointsShader.SetFloats("wind", new float[3] { wind.x, wind.y, wind.z } );

        int kernelHandle = calcWindPointsShader.FindKernel("CalcWindPoints");
        calcWindPointsShader.SetBuffer(kernelHandle, "Result", windPoints);
        calcWindPointsShader.Dispatch(kernelHandle, numCells.x, numCells.y, numCells.z);
        windPoints.GetData(points);
        */

        stopwatch.Stop();
        //Debug.Log("WindArea update time for " + numCells.x * numCells.y * numCells.z + " cells : " + stopwatch.ElapsedMilliseconds + " milliseconds");

        return points.ToArray();
    }
    protected override void UpdateWindFieldPoints()
    {
        if (mode == WindProducerMode.Dynamic)
        {
            windPoints = CalcWindFieldPoints();
            //if number of cells has changed, do full update of all wind field points;
            //if only wind vector has changed, just update existing wind field points with new wind vector.
            //Moving position will never affect the wind vector; changing rotation will if the wind is set to be relative to local rotation
            //TODO: I'm trying to update wind automatically by using reference vars, but it's not working
            if (numCellsDirty)
            {
                Debug.Log("numCellsDirty at wind area at " + transform.position);
                windPoints = CalcWindFieldPoints();
            }
            else if(windDirty)
            {
                Debug.Log("windDirty at wind area at " + transform.position);
                foreach (WindFieldPoint wp in windPoints) wp.wind = GetWind();
            }

        }
    }

    private void OnDestroy()
    {
        //windPoints.Release();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, GetWind());
    }


}
