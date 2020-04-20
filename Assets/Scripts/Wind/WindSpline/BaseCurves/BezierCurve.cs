using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BezierCurve : MonoBehaviour
{
    public Vector3[] points;

    //Docs: "Reset is called when the user hits the Reset button in the Inspector's context menu or when adding the component the first time. 
    //This function is only called in editor mode."
    //Resets points array to a straight line in the x axis
    void Reset()
    {
        points = new Vector3[3]
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(1f, 0f, 0f),
            new Vector3(2f, 0f, 0f)
        };
    }

    public Vector3 GetPoint(float t)
    {
        return BezierCurves.GetPoint(points, t);
    }

    public Vector3 GetWorldPoint(float t)
    {
        return transform.TransformPoint(BezierCurves.GetPoint(points, t));
    }

    public Vector3 GetDir(float t)
    {
        return BezierCurves.GetFirstDeriv(points, t);
    }

    public Vector3 GetWorldDir(float t)
    {
        return transform.TransformDirection(GetDir(t));
    }
}
