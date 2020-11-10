﻿using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Wind;

[RequireComponent(typeof(BezierSpline))]
public class WindSpline : WindProducer
{
    private BezierSpline spline;
    public float windStrength = 1;

    private const int MIN_SAMPLES_PER_CURVE = 1;
    private const int MAX_SAMPLES_PER_CURVE = 100;
    [Range(MIN_SAMPLES_PER_CURVE, MAX_SAMPLES_PER_CURVE)] [SerializeField] private int samplesPerCurve = 1;

    //Dirty flags
    private bool rotationDirty = false;

    protected override void OnEnable()
    {
        spline = GetComponent<BezierSpline>();
        base.OnEnable();
    }

    /*
    protected override void OnValidate()
    {
        spline = GetComponent<BezierSpline>();
        base.OnValidate();
    }
    */

    //Fast but very simple way of getting wind points, with no guarantee that the number of points will match up with the wind field cells
    //(i.e. that there will be no missed cells or multiple points per cell)
    protected override ComputeBuffer CalcWindFieldPoints()
    {
        List<WindFieldPoint> points = new List<WindFieldPoint>();
        float tInterval = Mathf.Max(1f / MAX_SAMPLES_PER_CURVE, 1f / samplesPerCurve);
        for(int i = 0; i < spline.curves.Count; i++)
        {
            for (float t = 0; t < 1; t += tInterval) points.Add(new WindFieldPoint(spline.GetWorldPoint(i, t), Vector3.Normalize(spline.GetWorldDir(i, t)) * windStrength, mode, priority));
        }

        if (windPointsBuffer != null) windPointsBuffer.Release();
        windPointsBuffer = new ComputeBuffer(points.Count, WindFieldPoint.stride);
        windPointsBuffer.SetData(points);
        
        return windPointsBuffer;
    }

    protected override void UpdateWindFieldPoints()
    {
        windPointsBuffer = CalcWindFieldPoints();
    }

    private void OnDrawGizmos()
    {
        /*
        if(EditorApplication.isPlaying)
        {
            for (int i = 0; i < spline.curves.Count; i++)
            {
                for (float t = 0; t < 1; t += tInterval)
                {
                    Vector3 pos = spline.GetWorldPoint(i, t);
                    Gizmos.color = Color.blue;
                    Gizmos.DrawRay(pos, spline.GetWorldDir(i, t).normalized);
                    Gizmos.color = Color.red;
                    Gizmos.DrawRay(pos, -transform.right);
                    Gizmos.color = Color.green;
                    Gizmos.DrawRay(pos, Vector3.Cross(-transform.right, spline.GetWorldDir(i, t).normalized));
                }
            }
        }
        */
    }

}
