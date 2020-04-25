using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindFieldCell
{
    private Vector3 wind;

    public WindFieldCell()
    {
        wind = Vector3.forward;
    }

    public Vector3 GetWind()
    {
        return wind;
    }
}
