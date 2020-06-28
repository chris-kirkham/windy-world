using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class that holds an array of points, to represent a line or Bezier curve.
/// Mostly a workaround to allow serialisation of the list of curves in BezierSpline/BezierSplineInspector
/// </summary>
[System.Serializable]
public class Points
{
    public Vector3[] points;

    public Points()
    {
        points = new Vector3[4];
    }

    public Points(Vector3[] points)
    {
        this.points = points;
    }

}
