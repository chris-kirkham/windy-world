using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Line))]
public class LineInspector : Editor
{
    private Vector3? selectedPoint = null;

    private void OnSceneGUI()
    {
        Line l = (Line)target;

        //Need to transform line points into world space 
        Transform t = l.transform;
        Vector3 worldP0 = t.TransformPoint(l.p0);
        Vector3 worldP1 = t.TransformPoint(l.p1);
        Quaternion handleRot = Tools.pivotRotation == PivotRotation.Local ? t.rotation : Quaternion.identity;

        /* Draw handle line */
        Handles.color = Color.white;
        Handles.DrawLine(worldP0, worldP1);

        //display dot button for each line point; only display handle of selected point
        //selectedIndex must persist between OnSceneGUI() calls or the handle will only appear for one call
        /*
        float size = HandleUtility.GetHandleSize(worldP0);
        if (Handles.Button(worldP0, handleRot, size * 0.075f, size * 0.15f, Handles.DotHandleCap))
        {
            selectedPoint = worldP0;
            isPointSelected = true;
        }
        else
        {
            size = HandleUtility.GetHandleSize(worldP1);
            if (Handles.Button(worldP0, handleRot, size * 0.075f, size * 0.15f, Handles.DotHandleCap))
            {
                selectedPoint = worldP1;
                isPointSelected = true;
            }
        }
        */

        /* Allow dragging handles to change line point positions */
        EditorGUI.BeginChangeCheck();
        worldP0 = Handles.DoPositionHandle(worldP0, handleRot); //DoPositionHandle creates an editor Handle at position and rotation
        if(EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(l, "Move Line Point"); //allow handle move to be undone with Undo
            EditorUtility.SetDirty(l); //set line to dirty so Unity knows a change was made and asks to save before closing etc.
            l.p0 = t.InverseTransformPoint(worldP0); //transform moved world point back to local position and update line point with it 
        }

        EditorGUI.BeginChangeCheck();
        worldP1 = Handles.DoPositionHandle(worldP1, handleRot);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(l, "Move Line Point");
            EditorUtility.SetDirty(l);
            l.p1 = t.InverseTransformPoint(worldP1);
        }

    }
}
