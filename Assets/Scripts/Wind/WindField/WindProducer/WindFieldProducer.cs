using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Abstract base class for wind-producing objects that should be added to a wind field. 
/// </summary>
public abstract class WindFieldProducer : MonoBehaviour
{
    public WindField windField;
    public WindProducerMode mode = WindProducerMode.Dynamic;
    public int cellDepth; //Depth at which this object should be added to the wind field. A cell at depth n is half the size of one at depth (n-1) 
    private float cellSize; //Actual size of the wind field cell at cellDepth
    //[Range(0, 1)] float precision;

    void Start()
    {
        if (windField == null) Debug.LogError("No wind field given for WindFieldProducer " + ToString() + "!");

        cellSize = windField.rootCellSize / Mathf.Pow(2, cellDepth);
    }


    //Returns an approximation of the wind producer in the form of individual WindFieldPoint(s) to be added to wind field cells.
    //
    //Each wind producer must specify one or more WindFieldPoints (a point in space which holds a wind vector 
    //and other data about how the wind field should handle that point) in this function; the wind field adds these points
    //to its corresponding positional cells. 
    //
    //The points returned by this function should be an approximation of the wind producer's shape and wind vector(s). 
    protected abstract WindFieldPoint[] GetWindFieldPoints();

    public void Include()
    {
        switch (mode)
        {
            case WindProducerMode.Dynamic:
                break;
            case WindProducerMode.PositionStatic:
                break;
            case WindProducerMode.Static:
                //wind will not change during runtime - add directly to corresponding wind field cells
                break;
            default:
                Debug.LogError("Unhandled WindProducerMode! If you added a new one, update this function.");
                break;
        }
    }


    //Removes this object from the wind field.
    public void Remove()
    {
        switch(mode)
        {
            case WindProducerMode.Dynamic:
                break;
            case WindProducerMode.PositionStatic:
                break;
            case WindProducerMode.Static:
                break;
            default:
                Debug.LogError("Unhandled WindProducerMode! If you added a new one, update this function.");
                break;
        }
    }
}
