using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class BezierSpline : MonoBehaviour
{
    public List<Vector3[]> curves;

    void OnEnable()
    {
        Vector3[] points = new Vector3[3]
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(1f, 0f, 0f),
            new Vector3(2f, 0f, 0f)
        };

        curves = new List<Vector3[]>();
        curves.Add(points);
    }
    
    void Reset()
    {
        OnEnable();
    }

    public void AddCurve(Vector3[] points)
    {
        if (points.Length > 4)
        {
            Debug.LogError("Too many points passed to AddCurve!");
        }
        else
        {
            //add new curve to spline; make first point of new curve be at same position as last point of previous curve
            int last = curves.Count - 1;
            Vector3 lastPoint = curves[last][curves[last].Length - 1]; //current last point of spline

            Vector3[] pointsOnLast = new Vector3[points.Length];
            for(int i = 0; i < points.Length; i++)
            {
                pointsOnLast[i] = points[i] + lastPoint;
            }

            curves.Add(pointsOnLast);
        }
    }

    public Vector3 GetPoint(int curveIndex, float t)
    {
        if (curveIndex < curves.Count)
        {
            return BezierCurves.GetPoint(curves[curveIndex], t);
        }
        else
        {
            Debug.LogError("curveIndex out of bounds! (" + curveIndex + ", number of curves in spline = "
                + curves.Count + ". Returning Vector3.zero");
            return Vector3.zero;
        }
    }

    public Vector3 GetWorldPoint(int curveIndex, float t)
    {
        if (curveIndex < curves.Count)
        {
            return transform.TransformPoint(BezierCurves.GetPoint(curves[curveIndex], t));
        }
        else
        {
            Debug.LogError("curveIndex out of bounds! (" + curveIndex + ", number of curves in spline = "
                + curves.Count + ". Returning Vector3.zero");
            return Vector3.zero;
        }
    }

    void OnDrawGizmos()
    {
        foreach(Vector3[] curve in curves)
        {
            foreach(Vector3 point in curve)
            {
                Gizmos.DrawSphere(transform.TransformPoint(point), 0.2f);
            }
        }
    }

}
