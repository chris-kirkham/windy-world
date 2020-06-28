using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Base class for wind arrow visualisation on lines/curves/splines
/// </summary>
abstract public class WindArrowVis : Editor
{
    public bool showArrows;
    protected GameObject windArrow = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Editor/WindVis/WindArrow");

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        showArrows = GUILayout.Button("Show wind arrows");
    }
}
