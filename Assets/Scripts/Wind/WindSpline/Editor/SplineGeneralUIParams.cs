using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Contains UI display parameters general to all types of line/curve/spline in WindSpline
/// </summary>
public static class SplineGeneralUIParams
{
    /* Point handle params */ 
    public const float handleSize = 0.05f;
    public const float handlePickSize = 0.15f;
    public static Color handleColour = Color.white;

    /* Curve visualisation params */
    public static Color handleLineColour = Color.grey;
    public static Color curveColour = Color.cyan;
    public static Color firstDerivColour = Color.green;
    public static Color secondDerivColour = Color.magenta;

}
