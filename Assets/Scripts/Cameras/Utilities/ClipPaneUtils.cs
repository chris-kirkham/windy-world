using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Extension methods for doing stuff relating to the camera clip pane (raycasts etc.)
/// </summary>
public static class ClipPaneUtils
{
    //Fires rays in a direction from the corners of the camera's near clip pane, returning a bool array 
    //representing raycast hits (clockwise from top left)
    /*
    public static bool[] RaycastsFromNearClipPane(this Camera cam, Vector3 dir, out RaycastHit[] hits, float maxDist, LayerMask layerMask)
    {
        Vector3[] points = GetNearClipPaneCornersWorld(cam);

        bool[] hitBools = new bool[4] { false, false, false, false };
        hits = new RaycastHit[4];
        if (Physics.Raycast(points[0], dir, out hits[0], maxDist, layerMask)) hitBools[0] = true;
        if (Physics.Raycast(points[1], dir, out hits[1], maxDist, layerMask)) hitBools[1] = true;
        if (Physics.Raycast(points[2], dir, out hits[2], maxDist, layerMask)) hitBools[2] = true;
        if (Physics.Raycast(points[3], dir, out hits[3], maxDist, layerMask)) hitBools[3] = true;

        return hitBools;
    }
    */

    //Fires rays in a direction from the corners of the camera's near clip pane;   
    //returns true if any hit and false if none hit. The out hits variable only returns successful hits, so it will be empty if none hit.
    public static bool RaycastsFromNearClipPane(this Camera cam, Vector3 dir, out RaycastHit[] hits, float maxDist, LayerMask layerMask)
    {
        Vector3[] points = GetNearClipPaneCornersWorld(cam);

        List<RaycastHit> hitsList = new List<RaycastHit>();
        RaycastHit hit;
        if (Physics.Raycast(points[0], dir, out hit, maxDist, layerMask)) hitsList.Add(hit);
        if (Physics.Raycast(points[1], dir, out hit, maxDist, layerMask)) hitsList.Add(hit);
        if (Physics.Raycast(points[2], dir, out hit, maxDist, layerMask)) hitsList.Add(hit);
        if (Physics.Raycast(points[3], dir, out hit, maxDist, layerMask)) hitsList.Add(hit);

        hits = hitsList.ToArray();
        return hitsList.Count > 0;
    }

    //Gets near clip pane corner points in world space from a given camera.
    //Returns points in clockwise order from top left
    public static Vector3[] GetNearClipPaneCornersWorld(this Camera cam)
    {
        Vector3[] points = new Vector3[4];

        //from comment on https://www.youtube.com/watch?v=MkbovxhwM4I
        float z = cam.nearClipPlane;
        float y = Mathf.Tan(cam.fieldOfView / 2 * Mathf.Deg2Rad) * z;
        float x = y * cam.aspect;
        points[0] = cam.transform.TransformPoint(new Vector3(-x, y, z)); //top left
        points[1] = cam.transform.TransformPoint(new Vector3(x, y, z)); //top right
        points[2] = cam.transform.TransformPoint(new Vector3(x, -y, z)); //bottom right
        points[3] = cam.transform.TransformPoint(new Vector3(-x, -y, z)); //bottom left

        return points;
    }

    //Gets near clip pane corner points in local space from a given camera.
    //Returns points in clockwise order from top left
    public static Vector3[] GetNearClipPaneCornersLocal(this Camera cam)
    {
        Vector3[] points = new Vector3[4];

        //from comment on https://www.youtube.com/watch?v=MkbovxhwM4I
        float z = cam.nearClipPlane;
        float y = Mathf.Tan(cam.fieldOfView / 2 * Mathf.Deg2Rad) * z;
        float x = y * cam.aspect;
        points[0] = new Vector3(-x, y, z); //top left
        points[1] = new Vector3(x, y, z); //top right
        points[2] = new Vector3(x, -y, z); //bottom right
        points[3] = new Vector3(-x, -y, z); //bottom left

        return points;
    }

    //Returns the x and y offset of an object from the centre of the camera view.
    //Ranges from (-0.5, -0.5) for top left to (0.5, 0.5) for bottom right (centre is (0, 0)).
    public static Vector2 GetOffsetFromCentreOfScreen(this Camera cam, Vector3 objWorldPos)
    {
        Vector3 screenPos = cam.WorldToScreenPoint(objWorldPos);
        //Debug.Log(objWorldPos + " screen offset = " + new Vector2((screenPos.x / cam.pixelWidth) - 0.5f, (screenPos.y / cam.pixelHeight) - 0.5f));
        //normalise x and y screen points to [0, 1] by dividing width and height; subtract 0.5 from each so (0, 0) becomes the centre
        return new Vector2((screenPos.x / cam.pixelWidth) - 0.5f, (screenPos.y / cam.pixelHeight) - 0.5f); 
    }

}
