using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Contains methods for getting points on Bezier curves.
/// </summary>
public static class BezierCurves
{
    //Gets a point on the line given by points p0 and p1 at t where 0 <= t <= 1
    public static Vector3 GetPointLinear(Vector3 p0, Vector3 p1, float t)
    {
        return Vector3.Lerp(p0, p1, t);
    }

    //Gets a point on the quadratic Bezier curve given by handles p0, p1, and p2 at t where 0 <= t <= 1
    public static Vector3 GetPointQuadratic(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        t = Mathf.Clamp01(t);

        //https://www.gamasutra.com/blogs/VivekTank/20180806/323709/How_to_work_with_Bezier_Curve_in_Games_with_Unity.php     
        float oneMinusT = 1 - t;
        return (oneMinusT * oneMinusT * p0) + (2 * oneMinusT * t * p1) + (t * t * p2);
    }

    //Gets a point on the cubic Bezier curve given by handles p0 .. p3 at t where 0 <= t <= 1
    public static Vector3 GetPointCubic(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        t = Mathf.Clamp01(t);

        float oneMinusT = 1 - t;
        return (Mathf.Pow(oneMinusT, 3) * p0) + (3 * Mathf.Pow(oneMinusT, 2) * t * p1)
               + (3 * oneMinusT * t * t * p2) + (Mathf.Pow(t, 3) * p3);
    }
}
