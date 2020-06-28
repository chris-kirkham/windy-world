using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WF_Cell
{
    //This cell's dynamic and static wind vectors. 
    private Vector3 windDynamic;
    private Vector3 windStatic;

    //The list of dynamic wind points in this cell. 
    private List<WF_WindPoint> windObjsDynamic { get; }

    //the size of this cell. 
    public readonly float cellSize;

    //the world position of the least corner of this cell.
    public readonly Vector3 worldPos;

    //the world position of the centre of this cell.
    public readonly Vector3 worldPosCentre;

    public WF_Cell(float cellSize, Vector3 worldPos)
    {
        windObjsDynamic = new List<WF_WindPoint>();
        windDynamic = Vector3.zero;
        windStatic = Vector3.zero;
        this.cellSize = cellSize;
        this.worldPos = worldPos;
        float halfCellSize = cellSize / 2;
        worldPosCentre = worldPos + new Vector3(halfCellSize, halfCellSize, halfCellSize);
    }

    public WF_Cell(WF_Cell parent, Vector3 worldPos)
    {
        windObjsDynamic = new List<WF_WindPoint>();
        windDynamic = Vector3.zero;
        windStatic = Vector3.zero;
        cellSize = parent.cellSize / 2;
        this.worldPos = worldPos;
        float halfCellSize = cellSize / 2;
        worldPosCentre = worldPos + new Vector3(halfCellSize, halfCellSize, halfCellSize);
    }

    //Adds given WindFieldPoint to the correct container by its static/dynamic type
    public void Add(WF_WindPoint obj)
    {
        switch (obj.mode)
        {
            case WindProducerMode.Dynamic:
                AddDynamic(obj);
                UpdateDynamic();
                break;
            case WindProducerMode.Static:
                AddStatic(obj);
                break;
            default:
                Debug.LogError("Unhandled ProducerMode! If you added a new one, update this function.");
                break;
        }
    }

    private void AddDynamic(WF_WindPoint obj)
    {
        windObjsDynamic.Add(obj);
        //windDynamic += obj.wind;
    }
    
    private void AddStatic(WF_WindPoint obj)
    {
        windStatic += obj.wind;
    }

    private void UpdateDynamic()
    {
        windDynamic = Vector3.zero;

        foreach (WF_WindPoint wp in windObjsDynamic) windDynamic += wp.wind;
    }

    public void ClearDynamic()
    {
        windObjsDynamic.Clear();
    }

    //Returns true if the cell contains no dynamic wind points and its static wind vector is zero. 
    public bool IsEmpty()
    {
        return (windObjsDynamic.Count == 0) && (windStatic == Vector3.zero);
    }

    /*----GETTERS----*/
    public Vector3 GetWind()
    {
        return windDynamic + windStatic;
    }

    public Vector3 GetStaticWind()
    {
        return windStatic;
    }

}
