using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class WindSpline : MonoBehaviour
{
    private BezierSpline spline;

    /* start/end planes - planes whose centres are the first/last points of the spline;
     * they are oriented perpendicular to the lines p[0]->[1]/p[last - 1]->[last] respectively. 
     * Vertices are wound clockwise from top-left. */
    private Vector3[] startPlane;
    private Vector3[] endPlane;

    private float width;
    private float halfWidth;
    public float Width
    {
        get { return width; }
        set { width = value; halfWidth = value / 2; }
    }

    void Start()
    {
        spline = new BezierSpline();
        startPlane = new Vector3[4];
        endPlane = new Vector3[4];
    }

    public void GetHashPoints(Vector3 hashCellSize)
    {

    }

    private Vector3[] ConstructPlane(Vector3 midPoint, float halfSize, Vector3 normalDir)
    {
        //relative left and up directions to perpDir
        Vector3 left = Vector3.Cross(normalDir, Vector3.up).normalized * halfSize;
        Vector3 up = Vector3.Cross(normalDir, left).normalized * halfSize;

        //Plane vertices - clockwise from top left
        Vector3[] plane = new Vector3[4];
        plane[0] = midPoint + left + up;
        plane[1] = midPoint - left + up;
        plane[2] = midPoint - left - up;
        plane[3] = midPoint + left - up;

        return plane;
    }

    void OnDrawGizmos()
    {
        //left/up of start and end planes

        //vertices of start and end planes
    }


}
