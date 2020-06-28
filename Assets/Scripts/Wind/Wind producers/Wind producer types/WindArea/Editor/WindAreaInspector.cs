using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WindArea))]
public class WindAreaInspector : Editor
{
    private WindArea windArea;

    private void OnEnable()
    {
        windArea = (WindArea)target;
    }

    private void OnDestroy()
    {
    }

    private void OnSceneGUI()
    {

    }

}
