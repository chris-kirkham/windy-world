using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class BezierSpline : MonoBehaviour
{
    public List<Points> curves = new List<Points>
    {
        new Points
        (
            new Vector3[3]
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(0f, 0f, 1f),
                new Vector3(0f, 0f, 2f)
            }
        )
    };

    public void AddCurve(Vector3[] points)
    {
        if (points.Length > 4)
        {
            Debug.LogError("Too many points passed to AddCurve!");
        }
        else
        {
            //add new curve to spline
            if (curves.Count > 0)
            {
                //make first point of new curve be at same position as last point of previous curve
                Vector3[] lastCurve = curves[curves.Count - 1].points;
                Vector3 lastPoint = lastCurve[lastCurve.Length - 1]; //current last point of last curve of spline

                Vector3[] movedPoints = new Vector3[points.Length];
                for (int i = 0; i < points.Length; i++)
                {
                    movedPoints[i] = points[i] + lastPoint;
                }
                Points newCurve = new Points(movedPoints);

                curves.Add(newCurve);
            }
            else
            {
                curves.Add(new Points(points));
            }
        }
    }

    public void DeleteLastCurve()
    {
        if(curves.Count > 0) curves.RemoveAt(curves.Count - 1);
    }

    public Vector3 GetPoint(int curveIndex, float t)
    {
        if (curveIndex < curves.Count)
        {
            return BezierCurves.GetPoint(curves[curveIndex].points, t);
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
            return transform.TransformPoint(BezierCurves.GetPoint(curves[curveIndex].points, t));
        }
        else
        {
            Debug.LogError("curveIndex out of bounds! (" + curveIndex + ", number of curves in spline = "
                + curves.Count + ". Returning Vector3.zero");
            return Vector3.zero;
        }
    }

    public Vector3 GetDir(int curveIndex, float t)
    {
        return BezierCurves.GetFirstDeriv(curves[curveIndex].points, t);
    }

    public Vector3 GetWorldDir(int curveIndex, float t)
    {
        return transform.TransformDirection(GetDir(curveIndex, t));
    }

}
