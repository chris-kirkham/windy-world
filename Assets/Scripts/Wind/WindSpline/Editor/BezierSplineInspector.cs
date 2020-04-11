using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BezierSpline))]
public class BezierSplineInspector : Editor
{
    private int selectedCurveIndex = -1; //selected curve in spline
    private int selectedPointIndex = -1; //selected point of selected curve

    private void OnSceneGUI()
    {
        
        BezierSpline spline = (BezierSpline)target;
        Transform transform = spline.transform;
        Quaternion handleRot = Tools.pivotRotation == PivotRotation.Local ? transform.rotation : Quaternion.identity;

        /* Transform points of each curve into world space */
        List<Vector3[]> worldCurves = new List<Vector3[]>();
        foreach(Vector3[] curve in spline.curves)
        {
            Vector3[] worldCurve = new Vector3[curve.Length];
            for (int i = 0; i < curve.Length; i++) worldCurve[i] = transform.TransformPoint(curve[i]);

            worldCurves.Add(worldCurve);
        }


        /* Loop through each curve in the spline and update handles/lines/curves for it */
        for(int i = 0; i < worldCurves.Count; i++)
        {
            Vector3[] curve = worldCurves[i];

            /* Draw handle line */
            Handles.color = SplineGeneralUIParams.handleLineColour;
            for (int j = 1; j < curve.Length; j++)
            {
                Handles.DrawLine(curve[j - 1], curve[j]);
            }

            /* show handle for and update each point on curve */
            for (int j = 0; j < curve.Length; j++)
            {
                Vector3 p = curve[j];

                //display dot button for each curve point; only display handle of selected point
                //selectedIndex must persist between OnSceneGUI() calls or the handle will only appear for one call
                float size = HandleUtility.GetHandleSize(p);
                Handles.color = SplineGeneralUIParams.handleColour;
                if (Handles.Button(p, handleRot, size * SplineGeneralUIParams.handleSize,
                                size * SplineGeneralUIParams.handlePickSize, Handles.DotHandleCap))
                {
                    selectedCurveIndex = i;
                    selectedPointIndex = j;
                }

                if (i == selectedCurveIndex && j == selectedPointIndex)
                {
                    EditorGUI.BeginChangeCheck();
                    p = Handles.DoPositionHandle(p, handleRot); //DoPositionHandle creates an editor Handle at position and rotation
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(spline, "Move Bezier Curve Point"); //allow handle move to be undone with Undo
                        EditorUtility.SetDirty(spline); //set line to dirty so Unity knows a change was made and asks to save before closing etc.
                        spline.curves[i][j] = transform.InverseTransformPoint(p); //transform moved world point back to local position and update line point with it 
                    }
                }

            }

            /* draw curve as line segments */
            float curveSegments = 10 + (10 * Vector3.SqrMagnitude(curve[curve.Length - 1] - curve[0]));
            float segmentLength = 1 / curveSegments;

            Handles.color = SplineGeneralUIParams.curveColour;
            for (float t = segmentLength; t <= 1; t += segmentLength)
            {
                Handles.DrawLine(spline.GetWorldPoint(i, t - segmentLength), spline.GetWorldPoint(i, t));
            }
        }


    }


    private enum CurveType { Line, Quadratic, Cubic };
    private CurveType selectedCurveType = CurveType.Quadratic;

    public override void OnInspectorGUI()
    {
        BezierSpline spline = (BezierSpline)target;

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Add curve"))
        {
            if (selectedCurveType == CurveType.Line)
            {
                spline.AddCurve(new Vector3[2] { Vector3.zero, Vector3.right });
            }
            else if(selectedCurveType == CurveType.Quadratic)
            {
                spline.AddCurve(new Vector3[3] { Vector3.zero, Vector3.right, new Vector3(2f, 0f, 0f) });
            }
            else //cubic
            {
                spline.AddCurve(new Vector3[4] 
                    {
                        Vector3.zero,
                        Vector3.right,
                        new Vector3(2f, 0f, 0f),
                        new Vector3(3f, 0f, 0f)
                    }
                );
            }
        }

        selectedCurveType = (CurveType)EditorGUILayout.EnumPopup(selectedCurveType);

        GUILayout.EndHorizontal();
    
    }
}
