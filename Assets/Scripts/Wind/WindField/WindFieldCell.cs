using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindFieldCell : HashCell
{
    private Vector3 wind;
    private Vector3 windAesthetic;

    public WindFieldCell() : base()
    {
        wind = Vector3.zero;
        windAesthetic = Vector3.zero;
    }

    public WindFieldCell(GameObject obj) : base(obj)
    {
        wind = Vector3.zero;
        windAesthetic = Vector3.zero;
    }

    public WindFieldCell(List<GameObject> objs) : base(objs)
    {
        wind = Vector3.zero;
        windAesthetic = Vector3.zero;
    }

    public Vector3 GetWind()
    {
        return wind;
    }

    public Vector3 GetWindAesthetic()
    {
        return windAesthetic;
    }

}
