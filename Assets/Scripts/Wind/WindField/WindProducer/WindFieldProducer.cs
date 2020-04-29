using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Abstract base class for objects that produce wind, that should be added to a wind field. 
/// </summary>
public abstract class WindFieldProducer : MonoBehaviour
{
    public WindField windField;
    public WindProducerMode mode = WindProducerMode.Dynamic;
    public int cellDepth; //Depth at which this object should be added to the wind field. A cell at depth n is half the size of one at depth (n-1) 
    private float cellSize; //Actual size of the wind field cell at cellDepth

    void Start()
    {
        if (windField == null) Debug.LogError("No wind field given for WindFieldProducer " + ToString() + "!");

        cellSize = windField.cellSize / (2 * cellDepth);
    }


    //Returns the individual WindFieldPoint(s) to be added to wind field cells
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
                Debug.LogError("Unhandled WindObjectType! If you added a new one, update this function.");
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
                //wind will not change during runtime - add directly to corresponding wind field cells
                break;
            default:
                Debug.LogError("Unhandled WindObjectType! If you added a new one, update this function.");
                break;
        }
    }
}
