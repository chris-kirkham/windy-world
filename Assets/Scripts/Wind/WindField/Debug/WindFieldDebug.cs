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
    //Arrow model to visualise wind directions
    private WindField windField;
    private GameObject windArrow;
    private Dictionary<WF_HashKey, GameObject> arrowField;
    private GameObject arrowFieldContainer; //empty parent object to hold arrows (just to keep editor tidy)
    public bool showWindArrows = false;

    public bool showCellBorders = true;
    [Range(0, 1)] public float opacity = 1f;
    private List<Vector3[]> cellVertices; //list of vertices for each cell in the wind field 

    private float updateInterval = 0.1f;


    void Start()
    {
        windField = GetComponent<WindField>();
        windArrow = Resources.Load<GameObject>("Debug/Wind/WindArrow");
        arrowField = new Dictionary<WF_HashKey, GameObject>();
        arrowFieldContainer = new GameObject("Wind field debug arrows");
        cellVertices = new List<Vector3[]>();

        StartCoroutine(UpdateCellVertices());
        StartCoroutine(UpdateWindArrows());
    }

    private IEnumerator UpdateWindArrows()
    {
        while (showWindArrows)
        {
            //Update wind arrow visualisation with current wind directions. Definitely a faster way to do this
            //(e.g. have something in WindField that stores only updated wind directions (inc. new cells) in a List<key, windDir>
            //and only loop through those)
            foreach (KeyValuePair<WF_HashKey, WF_Cell> kv in windField.GetCellDict())
            {
                WF_HashKey key = kv.Key;
                Vector3 wind = kv.Value.GetWind();
                if (wind != Vector3.zero)
                {
                    if (!arrowField.ContainsKey(key))
                    {
                        arrowField[key] = Instantiate(windArrow);
                        arrowField[key].transform.localScale *= windField.rootCellSize / Mathf.Pow(2, key.GetKey().Length);
                    }
                    arrowField[key].transform.position = windField.GetCellWorldPositionCentre(key);

                    Debug.Log("wind = " + wind);
                    //Vector3 wind = windField.GetWind(windField.GetCellWorldPosition(key));
                    arrowField[key].transform.rotation = Quaternion.LookRotation(wind);
                    arrowField[key].transform.SetParent(arrowFieldContainer.transform);
                }
            }

            yield return new WaitForSecondsRealtime(updateInterval);
        }

        foreach (GameObject arrow in arrowField.Values) Destroy(arrow);
        arrowField.Clear();

        yield return new WaitForSecondsRealtime(updateInterval);
    }

    private IEnumerator UpdateCellVertices()
    {
        while (showCellBorders)
        {
            List<Vector3[]> verts = new List<Vector3[]>();
            List<KeyValuePair<WF_HashKey, WF_Cell>> kv = windField.GetCellDict().ToList();
            for (int i = 0; i < kv.Count; i++)
            {
                float depth = kv[i].Key.GetKey().Length - 1;
                Vector3 worldPos = windField.GetCellWorldPosition(kv[i].Key);
                float cellSize = windField.rootCellSize / Mathf.Pow(2, depth);
                verts.Add(GetCellVertices(worldPos, cellSize));
            }

            cellVertices = verts;

            yield return new WaitForSecondsRealtime(updateInterval);
        }

        yield return new WaitForSecondsRealtime(updateInterval);

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

    //Gets a cell's vertices, assuming its input pos is bottom-left vertex
    Vector3[] GetCellVertices(Vector3 pos, float cellSize)
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
    void DrawCellDebug(Vector3[] verts, Color c)
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