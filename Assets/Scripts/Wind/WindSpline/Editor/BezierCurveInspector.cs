using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BezierCurve))]
public class BezierCurveInspector : Editor
{
    private int selectedIndex = -1;

    private void OnSceneGUI()
    {
        BezierCurve curve = (BezierCurve)target;
        Transform t = curve.transform;
        Quaternion handleRot = Tools.pivotRotation == PivotRotation.Local ? t.rotation : Quaternion.identity;

        /* Transform curve points into world space */
        Vector3[] worldPoints = new Vector3[curve.points.Length];

        for (int i = 0; i < worldPoints.Length; i++)
        {
            worldPoints[i] = t.TransformPoint(curve.points[i]);
        }

        /* Draw handle line */
        Handles.color = SplineGeneralUIParams.handleLineColour;
        for (int i = 1; i < worldPoints.Length; i++)
        {
            Handles.DrawLine(worldPoints[i - 1], worldPoints[i]);
        }

        /* show handle for and update each point on curve */
        for(int i = 0; i < worldPoints.Length; i++)
        {
            Vector3 p = worldPoints[i];

            //display dot button for each curve point; only display handle of selected point
            //selectedIndex must persist between OnSceneGUI() calls or the handle will only appear for one call
            float size = HandleUtility.GetHandleSize(p);
            Handles.color = SplineGeneralUIParams.handleColour;
            if (Handles.Button(p, handleRot, size * SplineGeneralUIParams.handleSize,
                            size * SplineGeneralUIParams.handlePickSize, Handles.DotHandleCap)) selectedIndex = i;

            if(i == selectedIndex)
            {
                EditorGUI.BeginChangeCheck();
                p = Handles.DoPositionHandle(p, handleRot); //DoPositionHandle creates an editor Handle at position and rotation
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(curve, "Move Bezier Curve Point"); //allow handle move to be undone with Undo
                    EditorUtility.SetDirty(curve); //set line to dirty so Unity knows a change was made and asks to save before closing etc.
                    curve.points[i] = t.InverseTransformPoint(p); //transform moved world point back to local position and update line point with it 
                }
            }
        }

        /* draw curve as line segments */
        float curveSegments = 10 + (10 * Vector3.SqrMagnitude(worldPoints[worldPoints.Length - 1] - worldPoints[0]));
        float segmentLength = 1 / curveSegments;
        Handles.color = SplineGeneralUIParams.curveColour;
        for(float i = segmentLength; i <= 1; i += segmentLength)
        {
            Handles.DrawLine(curve.GetWorldPoint(i - segmentLength), curve.GetWorldPoint(i));
        }

    }

}
