using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SocialPlatforms;

/// <summary>
/// Displays visual debug information for wind fields.
/// </summary>
[RequireComponent(typeof(WindField))]
public class WindFieldDebug : MonoBehaviour
{
    private WindField windField;
    private GameObject windArrow;
    //private Dictionary<WF_HashKey, GameObject> arrowField;
    private GameObject arrowFieldContainer; //empty parent object to hold arrows (just to keep editor tidy)
    public bool showWindArrows = false;

    public bool showCellBorders = true;
    [Range(0, 1)] public float opacity = 1f;
    private List<Vector3[]> cellVertices; //list of vertices for each cell in the wind field 

    private float updateInterval = 0f;

    private float maxWindSqrMagnitude = 0f;

    void Start()
    {
        windField = GetComponent<WindField>();
        windArrow = Resources.Load<GameObject>("Debug/Wind/WindArrow");
        //arrowField = new Dictionary<WF_HashKey, GameObject>();
        arrowFieldContainer = new GameObject("Wind field debug arrows");
        cellVertices = new List<Vector3[]>();

        StartCoroutine(UpdateDebugVis());
    }

    private void Update()
    {
        if (showCellBorders && opacity > 0)
        {
            foreach (Vector3[] verts in cellVertices)
            {
                //hacky way of getting a colour that represents cell depth (get distance between cell corners and divide them by root cell size)
                Vector3 rgb = (verts[6] - verts[0]) / windField.rootCellSize;
                Color c = new Color(rgb.x, rgb.y, rgb.z, opacity);
                DrawCellDebug(verts, c);
            }
        }
    }

    private IEnumerator UpdateDebugVis()
    {
        while(true)
        {
            List<WF_Cell> cells = windField.GetCells();
            UpdateWindArrows(cells);
            UpdateCellVertices(cells);
            yield return new WaitForSecondsRealtime(updateInterval);
        }
    }

    private void UpdateWindArrows(List<WF_Cell> cells) 
    {
        if (showWindArrows)
        {
            //Update wind arrow visualisation with current wind directions. Definitely a faster way to do this
            //(e.g. have something in WindField that stores only updated wind directions (inc. new cells) in a List<key, windDir>
            //and only loop through those)
            maxWindSqrMagnitude = 0f;
            List<Vector3> DEBUG_wind = new List<Vector3>(cells.Count);
            foreach (WF_Cell cell in cells)
            {
                Vector3 wind = windField.GetWind(cell.worldPosCentre);
                DEBUG_wind.Add(wind);
                float sqrMag = wind.sqrMagnitude;
                if (sqrMag > maxWindSqrMagnitude) maxWindSqrMagnitude = sqrMag;
                if (wind != Vector3.zero)
                {
                    /*
                    WF_HashKey key = new WF_HashKey(cell.worldPos, cell.cellSize);
                    if (!arrowField.ContainsKey(key))
                    {
                        arrowField[key] = Instantiate(windArrow);
                        arrowField[key].transform.localScale *= cell.cellSize;
                    }

                    arrowField[key].transform.position = cell.worldPosCentre;

                    //Vector3 wind = windField.GetWind(windField.GetCellWorldPosition(key));
                    arrowField[key].transform.rotation = Quaternion.LookRotation(wind);
                    arrowField[key].transform.SetParent(arrowFieldContainer.transform);
                    */

                    Debug.DrawRay(cell.worldPosCentre, wind, Color.HSVToRGB(sqrMag / maxWindSqrMagnitude, 1, 1));
                }
            }

            //Debug.Log("Wind: " + string.Join(", ", DEBUG_wind));
            //Debug.Log("number of wind arrows: " + arrowField.Count);
        }

        //foreach (GameObject arrow in arrowField.Values) Destroy(arrow);
        //arrowField.Clear();
    }

    private void UpdateCellVertices(List<WF_Cell> cells)
    {
        if (showCellBorders && opacity > 0)
        {
            List<Vector3[]> verts = new List<Vector3[]>();
            foreach(WF_Cell cell in cells)
            {
                verts.Add(GetCellVertices(cell.worldPos, cell.cellSize));
            }

            cellVertices = verts;
        }
    }

    
    //Gets a cell's vertices, assuming its input pos is bottom-left vertex
    private Vector3[] GetCellVertices(Vector3 pos, float cellSize)
    {
        Vector3[] vertices = new Vector3[8];

        float x = pos.x;
        float y = pos.y;
        float z = pos.z;

        //add each vertex of cell bounds cube (comment coords are relative vertex positions)
        //lower vertices added counterclockwise, then upper vertices added counterclockwise
        vertices[0] = new Vector3(x, y, z); //(0, 0, 0)
        vertices[1] = new Vector3(x + cellSize, y, z); //(1, 0, 0)
        vertices[2] = new Vector3(x + cellSize, y, z + cellSize); //(1, 0, 1)
        vertices[3] = new Vector3(x, y, z + cellSize); //(0, 0, 1)
        vertices[4] = new Vector3(x, y + cellSize, z); //(0, 1, 0)
        vertices[5] = new Vector3(x + cellSize, y + cellSize, z); //(1, 1, 0)
        vertices[6] = new Vector3(x + cellSize, y + cellSize, z + cellSize); //(1, 1, 1)
        vertices[7] = new Vector3(x, y + cellSize, z + cellSize); //(0, 1, 1)

        return vertices;
    }

    //Draws cell edges using Debug.DrawLine, given the list of vertices from GetCellVertices()
    private void DrawCellDebug(Vector3[] verts, Color c)
    {
        /*
        if(verts.Length != 8)
        {
            Debug.LogError("Incorrect number of vertices passed to DrawCellDebug! (" + verts.Length + ", should be 8)");
            return;
        }
        */

        //bottom edges
        Debug.DrawLine(verts[0], verts[1], c);
        Debug.DrawLine(verts[1], verts[2], c);
        Debug.DrawLine(verts[2], verts[3], c);
        Debug.DrawLine(verts[3], verts[0], c);

        //top edges
        Debug.DrawLine(verts[4], verts[5], c);
        Debug.DrawLine(verts[5], verts[6], c);
        Debug.DrawLine(verts[6], verts[7], c);
        Debug.DrawLine(verts[7], verts[4], c);

        //vertical edges
        Debug.DrawLine(verts[0], verts[4], c);
        Debug.DrawLine(verts[1], verts[5], c);
        Debug.DrawLine(verts[2], verts[6], c);
        Debug.DrawLine(verts[3], verts[7], c);
    }

}