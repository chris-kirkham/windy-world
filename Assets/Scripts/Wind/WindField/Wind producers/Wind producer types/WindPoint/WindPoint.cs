using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a single point of wind in space, which will be added to the wind field at the given depth
/// </summary>
[ExecuteInEditMode]
public class WindPoint : WindField_WindProducer
{
    public bool useRotationAsWindVector = true;
    public Vector3 wind;
    public float windStrength = 1;

    protected override WindField_WindPoint[] CalcWindFieldPoints()
    {
        return new WindField_WindPoint[1] { new WindField_WindPoint(transform.position, wind, priority, depth, mode) };
    }

    private void OnValidate()
    {
        if(useRotationAsWindVector) wind = transform.rotation * Vector3.forward * windStrength;
    }
}
