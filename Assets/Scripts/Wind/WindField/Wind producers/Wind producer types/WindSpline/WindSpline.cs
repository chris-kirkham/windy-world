using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BezierSpline))]
public class WindSpline : WF_WindProducer
{
    private BezierSpline spline;
    public float windStrength = 1;

    [Range(1, 100)] public int samplesPerCurve = 1;
    private float tInterval;
    protected override void Start()
    {
        spline = GetComponent<BezierSpline>();
        tInterval = 1f / (float)samplesPerCurve;
        base.Start();
    }

    /*
    private void Update()
    {
        if(mode == WindProducerMode.Dynamic) tInterval = 1f / (float)samplesPerCurve;
    }
    */

    /*
    protected override WF_WindPoint[] CalcWindFieldPoints()
    {
        //approximate length of each curve by summing distance between control points
        foreach(Points curve in spline.curves)
        {
            float curveLength = 0f;
            Vector3[] points = curve.points;
            for(int i = 1; i < points.Length; i++) curveLength += Vector3.Distance(points[i - 1], points[i]);

        }

    }
    */

    //Fast but very simple way of getting wind points, with no guarantee that the number of points will match up with the wind field cells
    //(i.e. that there will be no missed cells or multiple points per cell)
    protected override WF_WindPoint[] CalcWindFieldPoints()
    {
        List<WF_WindPoint> points = new List<WF_WindPoint>();
        for(int i = 0; i < spline.curves.Count; i++)
        {
            for (float t = 0; t < 1; t += tInterval) points.Add(new WF_WindPoint(spline.GetWorldPoint(i, t), Vector3.Normalize(spline.GetWorldDir(i, t)) * windStrength, depth, mode));
        }

        return points.ToArray();
    }
    
}
