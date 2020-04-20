using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class to 
/// </summary>

public class HashCell
{
    public float sizeX, sizeY, sizeZ;
    private List<GameObject> objs;
    private const float emptyTimeout = 5f; //time in seconds to wait before deleting an empty cell. Resets if an object enters the cell
    private float timeout;

    public HashCell()
    {
        objs = new List<GameObject>();
        timeout = emptyTimeout;
    }

    public HashCell(GameObject obj)
    {
        objs = new List<GameObject> { obj };
        timeout = emptyTimeout;
    }

    public HashCell(List<GameObject> objs)
    {
        this.objs = objs;
        timeout = emptyTimeout;
    }

    public List<GameObject> GetObjs()
    {
        return objs;
    }

    public void Add(GameObject obj)
    {
        objs.Add(obj);
    }

    public void Clear()
    {
        objs.Clear();
    }

    public void UpdateTimeout(float timePassed)
    {
        if (objs.Count > 0)
        {
            timeout = emptyTimeout;
        }
        else
        {
            timeout -= timePassed;
        }
    }

    public bool IsTimedOut()
    {
        return timeout <= 0;
    }

    public bool IsEmpty()
    {
        return objs.Count == 0;
    }

}