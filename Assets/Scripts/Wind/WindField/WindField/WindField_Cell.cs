using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindField_Cell
{
    //public float size;

    //Have different vectors for different wind modes (as opposed to one vector for the sum of all modes) 
    //so we don't have to update all of them when updating one mode
    private Vector3 windDynamic;
    private Vector3 windStatic;

    private List<WindField_WindPoint> windObjsDynamic { get; }

    public bool hasChild;

    public WindField_Cell()
    {
        windObjsDynamic = new List<WindField_WindPoint>();
        
        windDynamic = Vector3.zero;
        windStatic = Vector3.zero;
        
        hasChild = false;
    }

    public WindField_Cell(List<WindField_WindPoint> windObjsDynamic, List<WindField_WindPoint> windObjsPositionStatic, List<WindField_WindPoint> windObjsStatic)
    {
        this.windObjsDynamic = windObjsDynamic;
        UpdateWindDynamic();

        windStatic = Vector3.zero;
        foreach(WindField_WindPoint w in windObjsStatic) windStatic += w.wind;

        hasChild = false;
    }

    public WindField_Cell(WindField_Cell parent)
    {
        this.windObjsDynamic = parent.windObjsDynamic;
        UpdateWindDynamic();
        
        windStatic = parent.GetStaticWind();

        hasChild = false;
    }

    //Adds given WindFieldPoint to the correct container by its static/dynamic type
    public void Add(WindField_WindPoint obj)
    {
        switch (obj.mode)
        {
            case WindProducerMode.Dynamic:
                AddDynamic(obj);
                break;
            case WindProducerMode.Static:
                AddStatic(obj);
                break;
            default:
                Debug.LogError("Unhandled WindObjectType! If you added a new one, update this function.");
                break;
        }
    }

    private void AddDynamic(WindField_WindPoint obj)
    {
        windObjsDynamic.Add(obj);
    }
    
    private void AddStatic(WindField_WindPoint obj)
    {
        windStatic += obj.wind;
    }

    private void UpdateWindDynamic()
    {
        windDynamic = Vector3.zero;

        foreach(WindField_WindPoint w in windObjsDynamic)
        {
            windDynamic += w.wind;
        }
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

    public bool HasChild()
    {
        return hasChild;
    }

}
