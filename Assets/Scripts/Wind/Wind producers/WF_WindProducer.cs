using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Abstract base class for wind-producing objects that should be added to a wind field. 
/// </summary>
public abstract class WF_WindProducer : MonoBehaviour
{
    public WindField windField;
    public WindProducerMode mode = WindProducerMode.Dynamic;
    public uint depth = 0; //Depth at which this object should be added to the wind field octree. A cell at depth n is half the size of one at depth (n-1) 
    //[Range(0, 1)] float precision;
    public int priority = 0;
    public bool active = true;
    public float dynamicUpdateInterval = 0f; 

    private WF_WindPoint[] windPoints;
    protected float cellSize; //Actual size of the wind field cell at cellDepth

    protected virtual void Start()
    {
        if (windField == null)
        {
            throw new NullReferenceException("No wind field given for WindFieldProducer " + ToString() + "!");
        }

        cellSize = windField.rootCellSize / Mathf.Pow(2, depth);
        windPoints = CalcWindFieldPoints();
        AddToWindField();
        StartCoroutine(UpdateWindFieldPoints());
    }

    /*Returns an approximation of the wind producer in the form of individual WindFieldPoint(s) to be added to wind field cells.
     *
     *Each wind producer must specify one or more WindFieldPoints (a point in space which holds a wind vector 
     *and other data about how the wind field should handle that point) in this function; the wind field adds these points
     *to its corresponding positional cells.    
     *
     *The points returned by this function should be an approximation of the wind producer's shape and wind vector(s). */
    protected abstract WF_WindPoint[] CalcWindFieldPoints();
    
    protected IEnumerator UpdateWindFieldPoints()
    {
        while(mode == WindProducerMode.Dynamic)
        {
            windPoints = CalcWindFieldPoints();
            yield return new WaitForSecondsRealtime(dynamicUpdateInterval);
        }
    }

    public WF_WindPoint[] GetWindFieldPoints()
    {
        return windPoints;
    }

    public void AddToWindField()
    {
        Debug.Log("adding " + this.GetType() + " to wind field");
        windField.Include(this);
    }

    /*
    //Removes this object from the wind field.
    public void RemoveFromWindField()
    {
    }
    */

    public override string ToString()
    {
        return GetType() + " at " + transform.position;
    }
}
