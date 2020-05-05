using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindFieldCell
{
    //public float size;

    //Have different vectors for different wind modes (as opposed to one vector for the sum of all modes) 
    //so we don't have to update all of them when updating one mode
    private Vector3 windDynamic;
    private Vector3 windPositionStatic;
    private Vector3 windStatic;

    private List<WindFieldPoint> windObjsDynamic { get; }
    private List<WindFieldPoint> windObjsPositionStatic { get; }
    private List<WindFieldPoint> windObjsStatic { get; }

    private bool hasChild;

    public WindFieldCell()
    {
        windObjsDynamic = new List<WindFieldPoint>();
        windObjsPositionStatic = new List<WindFieldPoint>();
        windObjsStatic = new List<WindFieldPoint>();
        
        windDynamic = Vector3.zero;
        windPositionStatic = Vector3.zero;
        windStatic = Vector3.zero;
        
        hasChild = false;
    }

    public WindFieldCell(List<WindFieldPoint> windObjsDynamic, List<WindFieldPoint> windObjsPositionStatic, List<WindFieldPoint> windObjsStatic)
    {
        this.windObjsDynamic = windObjsDynamic;
        this.windObjsPositionStatic = windObjsPositionStatic;
        this.windObjsStatic = windObjsStatic;

        UpdateWindDynamic();
        UpdateWindPositionStatic();

        //this does the same thing as UpdateDynamic/UpdatePositionStatic, but it only happens when the cell is created
        windStatic = Vector3.zero;
        foreach(WindFieldPoint w in windObjsStatic)
        {
            windStatic += w.wind;
        }

        hasChild = false;
    }

    public WindFieldCell(WindFieldCell parent)
    {
        this.windObjsDynamic = parent.windObjsDynamic;
        this.windObjsPositionStatic = parent.windObjsPositionStatic;
        this.windObjsStatic = parent.windObjsStatic;

        UpdateWindDynamic();
        UpdateWindPositionStatic();

        //this does the same thing as UpdateDynamic/UpdatePositionStatic, but it only happens when the cell is created
        windStatic = Vector3.zero;
        foreach (WindFieldPoint w in windObjsStatic)
        {
            windStatic += w.wind;
        }

        hasChild = false;
    }

    //Adds given WindFieldPoint to the correct container by its static/dynamic type
    public void Add(WindFieldPoint obj)
    {
        switch (obj.mode)
        {
            case WindProducerMode.Dynamic:
                AddDynamic(obj);
                break;
            case WindProducerMode.PositionStatic:
                AddPositionStatic(obj);
                break;
            case WindProducerMode.Static:
                AddStatic(obj);
                //wind will not change during runtime - add directly to corresponding wind field cells
                break;
            default:
                Debug.LogError("Unhandled WindObjectType! If you added a new one, update this function.");
                break;
        }
    }

    public void AddDynamic(WindFieldPoint obj)
    {
        windObjsDynamic.Add(obj);
    }
    
    public void AddPositionStatic(WindFieldPoint obj)
    {
        windObjsPositionStatic.Add(obj);
    }
    
    public void AddStatic(WindFieldPoint obj)
    {
        windObjsStatic.Add(obj);
    }

    public void UpdateWindDynamic()
    {
        windDynamic = Vector3.zero;

        foreach(WindFieldPoint w in windObjsDynamic)
        {
            windDynamic += w.wind;
        }
    }

    public void UpdateWindPositionStatic()
    {
        windPositionStatic = Vector3.zero;

        foreach (WindFieldPoint w in windObjsPositionStatic)
        {
            windPositionStatic += w.wind;
        }
    }

    /*
    public void ClearDynamic()
    {
        windObjsDynamic.Clear();
    }

    public void ClearPositionStatic()
    {
        windObjsPositionStatic.Clear();
    }
    */

    /*----GETTERS----*/
    public Vector3 GetWind()
    {
        return windDynamic + windPositionStatic + windStatic;
    }

    public bool HasChild()
    {
        return hasChild;
    }

}
