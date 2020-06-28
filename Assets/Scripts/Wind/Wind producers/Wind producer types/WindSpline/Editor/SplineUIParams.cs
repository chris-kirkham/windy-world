using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Contains UI display parameters general to all types of line/curve/spline in WindSpline
/// </summary>
public static class SplineUIParams
{
    /* Point handle params */ 
    public const float handleSize = 0.05f;
    public const float handlePickSize = 0.15f;
    public static Color handleColour = Color.white;

    /* Curve visualisation colours */
    public static Color handleLineColour = Color.grey;
    public static Color selectedHandleLineColour = Color.white;
    public static Color curveColour = Color.blue;
    public static Color selectedCurveColour = Color.cyan;
    public static Color tanColour = Color.green;
    public static Color normalColour = Color.magenta;
    //public static Color firstDerivColour = Color.green;
    public static Color secondDerivColour = Color.yellow;

    /* Curve drawing segment length params */
    private const float curveResMult = 5f;
    private const float minSegmentLength = 0.01f;
    private const float maxSegmentLength = 0.2f;

    //Generates a suitable segment length for visualising a curve based on the distance between its points (more distance = more segments).
    public static float GetCurveSegmentLength(Vector3[] curve)
    {
        float totalSqrDist = 0f;
        for(int i = 1; i < curve.Length; i++)
        {
            totalSqrDist += Vector3.SqrMagnitude(curve[i] - curve[i - 1]);
        }

        float numSegments = curveResMult + (curveResMult * totalSqrDist);
        return Mathf.Min(maxSegmentLength, Mathf.Max(minSegmentLength, 1 / numSegments));
    }

}
