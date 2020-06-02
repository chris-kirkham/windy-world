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

    void Awake()
    {
        windWorld = wind;
    }

    void Update()
    {
        windWorld = transform.TransformDirection(wind);
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
                    points.Add(new WF_WindPoint(pos, GetWind(), depth, mode));
                }
            }
        }
        
        stopwatch.Stop();
        Debug.Log("WindArea update time: " + stopwatch.ElapsedMilliseconds);
        
        return points.ToArray();
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, GetWind());
    }

}
