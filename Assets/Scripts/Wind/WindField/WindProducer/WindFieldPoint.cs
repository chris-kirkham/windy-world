using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An object representing wind information at a point in world space, used in the wind field.
/// </summary>
public struct WindFieldPoint
{
    //Position in space
    public Vector3 pos;

    //Wind vector
    public Vector3 wind;
    
    //Size of the wind field cell this object should be added to. Should always be (wind field root cell size) / (2 * cell depth)
    // - this should be managed by whatever script creates this object [should check again here? Would necessitate this object knowing the wind field's root cell size]
    //An incorrect cell size will produce wrong results when hashing.
    public float cellSize;
    
    //The behaviour mode of the wind producer that created this object. See WindProducerMode script for a detailed description
    public WindProducerMode mode;

    //The wind vector of WindFieldPoints of a higher priority will override that of those of a lower priority in the same cell;
    //the wind vector of points with the same priority will be added to find that cell's overall wind vector
    public int priority;

    public WindFieldPoint(Vector3 pos, Vector3 wind, int priority, float cellSize, WindProducerMode mode)
    {
        this.pos = pos;
        this.wind = wind;
        this.priority = priority;
        this.cellSize = cellSize;
        this.mode = mode;
    }
}
