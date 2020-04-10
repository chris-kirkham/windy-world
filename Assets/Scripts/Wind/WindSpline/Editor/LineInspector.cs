using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Line))]
public class LineInspector : Editor
{
 
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

        /* Create handles for each point */
        //Handles.DoPositionHandle(worldP0, handleRot); //PositionHandle?
        //Handles.DoPositionHandle(worldP1, handleRot);

        /* Allow dragging handles to change line point positions */
        EditorGUI.BeginChangeCheck();
        worldP0 = Handles.DoPositionHandle(worldP0, handleRot);
        if(EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(l, "Move line point"); //allow handle move to be undone with Undo
            EditorUtility.SetDirty(l); //set line to dirty so Unity knows a change was made and asks to save before closing etc.
            l.p0 = t.InverseTransformPoint(worldP0); //transform point back to local position
        }

        EditorGUI.BeginChangeCheck();
        worldP1 = Handles.DoPositionHandle(worldP0, handleRot);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(l, "Move line point");
            EditorUtility.SetDirty(l);
            l.p1 = t.InverseTransformPoint(worldP1);
        }
    }
}
