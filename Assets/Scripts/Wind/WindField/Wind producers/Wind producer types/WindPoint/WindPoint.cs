using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a single point of wind in space, which will be added to the wind field at the given depth
/// </summary>
[ExecuteInEditMode]
public class WindPoint : WF_WindProducer
{
    public bool useRotationAsWindVector = true;
    public Vector3 wind;
    public float windStrength = 1;

    protected override WF_WindPoint[] CalcWindFieldPoints()
    {
        return new WF_WindPoint[1] { new WF_WindPoint(transform.position, wind, priority, depth, mode) };
    }

    private void Update()
    {
        if(useRotationAsWindVector) wind = transform.rotation * Vector3.forward * windStrength;
    }
}
