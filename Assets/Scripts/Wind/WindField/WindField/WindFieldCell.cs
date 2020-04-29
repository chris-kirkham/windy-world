using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindFieldCell
{
    private Vector3 wind;

    private List<WindFieldPoint> objsDynamic;
    private List<WindFieldPoint> objsPositionStatic;
    private List<WindFieldPoint> objsStatic;

    public WindFieldCell()
    {
        wind = Vector3.forward;
    }

    public Vector3 GetWind()
    {
        return wind;
    }

    //Adds given WindFieldPoint to the correct container by its static/dynamic type
    public void Add(WindFieldPoint obj)
    {
        switch (obj.type)
        {
            case WindObjectType.Dynamic:
                break;
            case WindObjectType.PositionStatic:
                break;
            case WindObjectType.Static:
                //wind will not change during runtime - add directly to corresponding wind field cells
                break;
            default:
                Debug.LogError("Unhandled WindObjectType! If you added a new one, update this function.");
                break;
        }
    }

    public void AddDynamic(WindFieldPoint obj)
    {
        objsDynamic.Add(obj);
    }
    
    public void AddPositionStatic(WindFieldPoint obj)
    {
        objsPositionStatic.Add(obj);
    }
    public void AddStatic(WindFieldPoint obj)
    {
        objsStatic.Add(obj);
    }

    public void ClearDynamic()
    {
        objsDynamic.Clear();
    }

    public void ClearPositionStatic()
    {
        objsPositionStatic.Clear();
    }


}
