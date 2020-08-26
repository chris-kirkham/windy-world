using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BezierCurve))]
public class CurveArrowVis : WindArrowVis
{
    private void OnSceneGUI()
    {
        BezierCurve curve = (BezierCurve)target;

        float segmentLength = SplineUIParams.GetCurveSegmentLength(curve.points);

        for (float t = segmentLength; t <= 1; t += segmentLength)
        {
            Instantiate(windArrow, curve.GetWorldPoint(t), Quaternion.LookRotation(curve.GetWorldDir(t), Vector3.up));
        }
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }

}
