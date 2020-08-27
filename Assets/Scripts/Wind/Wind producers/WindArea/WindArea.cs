using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.Mathematics;
using UnityEditor;
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

    //compute 
    public ComputeShader calcWindPointsShader;
    int calcWindPointsShaderKernel;
    private ComputeBuffer windPointsBuffer; //stores the wind points output by the compute shader
    private const int stride = (sizeof(float) * 6) + sizeof(uint) + (sizeof(int) * 2);
    private const int GROUP_SIZE_1D = 64; //MUST BE SAME AS IN COMPUTE SHADER
    private const int GROUP_SIZE_3D = 4; //MUST BE SAME AS IN COMPUTE SHADER

    //dirty flags
    private bool windDirty = false;
    private Vector3 lastWindVec;
    private bool numCellsDirty = false;
    private Vector3Int lastNumCells;

    void Awake()
    {
        windWorld = wind;

        calcWindPointsShaderKernel = calcWindPointsShader.FindKernel("CalcWindPoints");
        windPointsBuffer = new ComputeBuffer(numCells.x * numCells.y * numCells.z, stride, ComputeBufferType.Default);

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

        //COMPUTE SHADER METHOD
        windPointsBuffer.Release();
        windPointsBuffer = new ComputeBuffer(numCells.x * numCells.y * numCells.z, stride, ComputeBufferType.Default);
        WindFieldPoint[] points = new WindFieldPoint[windPointsBuffer.count];
        //https://forum.unity.com/threads/system-says-struct-is-blittable-unity-says-struct-isnt-blittable.590251/
        //https://forum.unity.com/threads/computebuffer-getdata.165501/
        windPointsBuffer.SetData(points); //necessary?

        //calculate start point of wind area (centre of least cell)
        Vector3 halfNumCells = new Vector3((float)numCells.x / 2, (float)numCells.y / 2, (float)numCells.z / 2);
        float halfCellSize = cellSize / 2;
        Vector3 startPos = transform.TransformPoint(-(halfNumCells * cellSize) + (Vector3.one * halfCellSize)); 
        float[] startPosAsArray = new float[3] { startPos.x, startPos.y, startPos.z };
        
        //set shader vars
        calcWindPointsShader.SetFloats("startPos", startPosAsArray);
        calcWindPointsShader.SetFloats("right", new float[3] { transform.right.x * cellSize, transform.right.y * cellSize, transform.right.z * cellSize } );
        calcWindPointsShader.SetFloats("up", new float[3] { transform.up.x * cellSize, transform.up.y * cellSize, transform.up.z * cellSize } );
        calcWindPointsShader.SetFloats("fwd", new float[3] { transform.forward.x * cellSize, transform.forward.y * cellSize, transform.forward.z * cellSize } );
        calcWindPointsShader.SetInts("numPoints", new int[3] { numCells.x, numCells.y, numCells.z } );
        Vector3 wind = GetWind();
        calcWindPointsShader.SetFloats("wind", new float[3] { wind.x, wind.y, wind.z } );

        calcWindPointsShader.SetBuffer(calcWindPointsShaderKernel, "Result", windPointsBuffer);

        //find number of thread groups 
        //3D
        /*
        int[] numGroups = new int[3] { numCells.x / GROUP_SIZE_3D, numCells.y / GROUP_SIZE_3D, numCells.z / GROUP_SIZE_3D }; 
        for (int i = 0; i < numGroups.Length; i++)
        {
            if (numGroups[i] <= 0) numGroups[i] = 1;
        }
        */

        //1D
        int[] numGroups = new int[3] { (numCells.x * numCells.y * numCells.z) / GROUP_SIZE_1D, 1, 1 };
        if (numGroups[0] <= 0) numGroups[0] = 1;
        //Debug.Log("numCells = " + numCells + ", numGroups = " + String.Join(", ", numGroups));
        calcWindPointsShader.Dispatch(calcWindPointsShaderKernel, numGroups[0], numGroups[1], numGroups[2]);

        stopwatch.Stop();
        //Debug.Log("WindArea update time for " + numCells.x * numCells.y * numCells.z + " cells : " + stopwatch.ElapsedMilliseconds + " milliseconds");

        stopwatch.Restart();
        windPointsBuffer.GetData(points);
        stopwatch.Stop();
        //Debug.Log("WindArea GetData time: " + stopwatch.ElapsedMilliseconds + "milliseconds");
        return points;
    }
    protected override void UpdateWindFieldPoints()
    {
        /*
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
        */

        windPoints = CalcWindFieldPoints();
    }

    private void OnDestroy()
    {
        windPointsBuffer.Release();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        //Gizmos.DrawRay(transform.position, GetWind());

        if(EditorApplication.isPlaying)
        {
            foreach(WindFieldPoint point in windPoints)
            {
                Gizmos.DrawRay(point.position, point.wind);
            }
        }
    }


}
