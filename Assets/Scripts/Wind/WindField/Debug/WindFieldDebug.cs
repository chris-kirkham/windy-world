using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Displays visual debug information for wind fields.
/// </summary>
[RequireComponent(typeof(WindField))]
public class WindFieldDebug : MonoBehaviour
{
    //Arrow model to visualise wind directions
    private WindField windField;
    private GameObject windArrow;
    private Dictionary<WFHashKey, GameObject> arrowField; 
    public bool showWindArrows = false;
    public bool showCellBorders = true;

    private float updateInterval = 0.1f;

    void Start()
    {
        windField = GetComponent<WindField>();
        windArrow = Resources.Load<GameObject>("Debug/Wind/WindArrow");
        arrowField = new Dictionary<WFHashKey, GameObject>();

        StartCoroutine(UpdateWindArrows());
    }

    private IEnumerator UpdateWindArrows()
    {
        while(showWindArrows)
        {
            //Update wind arrow visualisation with current wind directions. Definitely a faster way to do this
            //(e.g. have something in WindField that stores only updated wind directions (inc. new cells) in a List<key, windDir>
            //and only loop through those)
            foreach (KeyValuePair<WFHashKey, WindFieldCell> kv in windField.GetCellDict())
            {
                WFHashKey key = kv.Key;
                if (!arrowField.ContainsKey(key))
                {
                    arrowField[key] = Instantiate(windArrow);
                    arrowField[key].transform.localScale *= (0.25f * windField.rootCellSize);
                }

                arrowField[key].transform.position = windField.GetCellWorldPosition(key);

                Vector3 wind = kv.Value.GetWind();
                if(wind != Vector3.zero) arrowField[key].transform.rotation = Quaternion.LookRotation(wind);
                
                yield return null;
            }

            yield return new WaitForSecondsRealtime(updateInterval);
        }

        arrowField.Clear();

        //yield return null; //why is this not necessary?
    }

    private void Update()
    {
        if(showCellBorders)
        {
            foreach (KeyValuePair<WFHashKey, WindFieldCell> kv in windField.GetCellDict())
            {
                float depth = kv.Key.GetKey().Length - 1;
                float cellSize = windField.rootCellSize / Mathf.Pow(2, depth);
                Vector3 worldPos = windField.GetCellWorldPosition(kv.Key);
                DrawCellDebug(GetCellVertices(worldPos, cellSize), Color.HSVToRGB(1 - (depth / 5), 1, 1));
            }
        }
    }

    //Gets a cell's vertices, assuming its input pos is bottom-left vertex
    List<Vector3> GetCellVertices(Vector3 pos, float cellSize)
    {
        List<Vector3> vertices = new List<Vector3>();

        float x = pos.x;
        float y = pos.y;
        float z = pos.z;

        //add each vertex of cell bounds cube (comment coords are relative vertex positions)
        //lower vertices added counterclockwise, then upper vertices added counterclockwise
        vertices.Add(new Vector3(x, y, z)); //(0, 0, 0)
        vertices.Add(new Vector3(x + cellSize, y, z)); //(1, 0, 0)
        vertices.Add(new Vector3(x + cellSize, y, z + cellSize)); //(1, 0, 1)
        vertices.Add(new Vector3(x, y, z + cellSize)); //(0, 0, 1)
        vertices.Add(new Vector3(x, y + cellSize, z)); //(0, 1, 0)
        vertices.Add(new Vector3(x + cellSize, y + cellSize, z)); //(1, 1, 0)
        vertices.Add(new Vector3(x + cellSize, y + cellSize, z + cellSize)); //(1, 1, 1)
        vertices.Add(new Vector3(x, y + cellSize, z + cellSize)); //(0, 1, 1)
        vertices.Add(new Vector3(x, y + cellSize, z)); //(0, 1, 0)

        return vertices;
    }

    //Draws cell edges using Debug.DrawLine, given the list of vertices from GetCellVertices()
    void DrawCellDebug(List<Vector3> verts, Color c)
    {
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
