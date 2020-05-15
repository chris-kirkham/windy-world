using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BezierSpline))]
public class BezierSplineInspector : Editor
{
    private int selectedCurveIndex = -1; //selected curve in spline
    private int selectedPointIndex = -1; //selected point of selected curve

    /* normal/tangent visualisation */
    private bool showTangents = false;
    private bool showNormals = false;

    private void OnSceneGUI()
    {
        BezierSpline spline = (BezierSpline)target;
        Transform transform = spline.transform;
        Quaternion handleRot = Tools.pivotRotation == PivotRotation.Local ? transform.rotation : Quaternion.identity;

        /* Transform points of each curve into world space */
        List<Vector3[]> worldCurves = new List<Vector3[]>();
        //foreach(Vector3[] curve in spline.curves)
        foreach(Points curve in spline.curves)
        {
            Vector3[] worldCurve = new Vector3[curve.points.Length];
            for (int i = 0; i < curve.points.Length; i++) worldCurve[i] = transform.TransformPoint(curve.points[i]);

            worldCurves.Add(worldCurve);
        }

        /* Loop through each world space curve in the spline and update handles/lines/curves for it */
        for(int i = 0; i < worldCurves.Count; i++)
        {
            Vector3[] curve = worldCurves[i];

            /* Draw handle line */
            Handles.color = i == selectedCurveIndex ? SplineUIParams.selectedHandleLineColour : SplineUIParams.handleLineColour;
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
                Handles.color = SplineUIParams.handleColour;
                if (Handles.Button(p, handleRot, size * SplineUIParams.handleSize,
                                size * SplineUIParams.handlePickSize, Handles.DotHandleCap))
                {
                    selectedCurveIndex = i;
                    selectedPointIndex = j;
                }

                //display move handle for selected point
                if (i == selectedCurveIndex && j == selectedPointIndex)
                {
                    EditorGUI.BeginChangeCheck();
                    p = Handles.DoPositionHandle(p, handleRot); //DoPositionHandle creates an editor Handle at position and rotation
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(spline, "Move Bezier Curve Point"); //allow handle move to be undone with Undo
                        EditorUtility.SetDirty(spline); //set line to dirty so Unity knows a change was made and asks to save before closing etc.

                        //Transform moved world point back to local position and update selected point with it 
                        Vector3 localPoint = transform.InverseTransformPoint(p);
                        spline.curves[i].points[j] = localPoint;

                        //If selected point is the last point of non-last curve in the spline, move first point of next curve to match;
                        //if selected point is the first point of non-first curve in the spline, move last point of previous curve to match
                        if (j == spline.curves[i].points.Length - 1 && i < spline.curves.Count - 1)
                        {
                            spline.curves[i + 1].points[0] = localPoint;
                        }
                        else if (j == 0 && i > 0) 
                        {
                            spline.curves[i - 1].points[spline.curves[i - 1].points.Length - 1] = localPoint;
                        }
                    }
                }
            }

            /* draw curve as line segments */
            float segmentLength = SplineUIParams.GetCurveSegmentLength(curve);

            Color curveCol = i == selectedCurveIndex ? SplineUIParams.selectedCurveColour : SplineUIParams.curveColour;
            for (float t = segmentLength; t <= 1; t += segmentLength)
            {
                Handles.color = curveCol;
                Handles.DrawLine(spline.GetWorldPoint(i, t - segmentLength), spline.GetWorldPoint(i, t));
                
                if(showTangents)
                {
                    Handles.color = SplineUIParams.tanColour;
                    //Handles.DrawLine(spline.)
                }

                if(showNormals)
                {
                    Handles.color = SplineUIParams.normalColour;
                    //show normals
                }
                
                //GameObject windArrow = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Editor/WindVis/WindArrow.prefab");
                //Instantiate(windArrow, spline.GetWorldPoint(i, t - segmentLength), Quaternion.identity);
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

        GUILayout.BeginHorizontal();
        showTangents = GUILayout.Button("Show tangents");
        showNormals = GUILayout.Button("Show normals");
        GUILayout.EndHorizontal();

    }
}
