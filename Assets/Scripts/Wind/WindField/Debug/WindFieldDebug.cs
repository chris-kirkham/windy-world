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

    private float updateInterval = 0.1f;
    private Material lineMaterial;

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
        foreach (KeyValuePair<WFHashKey, WindFieldCell> kv in windField.GetCellDict())
        {
            float depth = kv.Key.GetKey().Length - 1;
            float cellSize = windField.rootCellSize / Mathf.Pow(2, depth);
            Vector3 worldPos = windField.GetCellWorldPosition(kv.Key);
            DrawWireCube(GetCellVertices(worldPos, cellSize), Color.HSVToRGB(1 - (depth / 5), 1, 1));
        }
    }

    //draws wire cube using GL
    void DrawWireCube(List<Vector3> verts, Color colour)
    {
        CreateLineMaterial(colour);
        lineMaterial.SetPass(0);

        GL.PushMatrix();
        GL.Color(colour);

        //bottom horizontal edges
        GL.Begin(GL.LINE_STRIP);
        GL.Vertex(verts[0]);
        GL.Vertex(verts[1]);
        GL.Vertex(verts[2]);
        GL.Vertex(verts[3]);
        GL.Vertex(verts[0]);

        //vertical line from (0, 0, 0) to (0, 1, 0) (do it now we're on (0, 0, 0) so we don't have to repeat unecessarily)
        GL.Vertex(verts[4]);

        //top horizontal edges
        GL.Vertex(verts[5]);
        GL.Vertex(verts[6]);
        GL.Vertex(verts[7]);
        GL.Vertex(verts[4]);
        GL.End();

        //remaining vertical lines
        GL.Begin(GL.LINES);
        GL.Vertex(verts[1]);
        GL.Vertex(verts[5]);

        GL.Vertex(verts[2]);
        GL.Vertex(verts[6]);

        GL.Vertex(verts[3]);
        GL.Vertex(verts[7]);
        GL.End();

        GL.PopMatrix();
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

        return vertices;
    }

    //create material for GL drawing
    void CreateLineMaterial(Color colour)
    {
        //move this to Start? it's called every OnPostRender in the doc examples 
        if (!lineMaterial)
        {
            // Unity has a built-in shader that is useful for drawing
            // simple colored things.
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            lineMaterial = new Material(shader);
            lineMaterial.SetColor("_Color", colour); //set _Color property of Internal-Colored shader
            lineMaterial.hideFlags = HideFlags.HideAndDontSave;
            // Turn on alpha blending
            lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            // Turn backface culling off
            lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            // Turn off depth writes
            lineMaterial.SetInt("_ZWrite", 0);
        }

        if (colour != lineMaterial.GetColor("_Color"))
        {
            lineMaterial.SetColor("_Color", colour);
        }
    }

    
    private void OnDrawGizmos()
    {
        foreach (KeyValuePair<WFHashKey, WindFieldCell> kv in windField.GetCellDict())
        {
            float depth = kv.Key.GetKey().Length - 1;

            Gizmos.color = Color.HSVToRGB(1 - (depth / 7), 1, 1);
            //Gizmos.color = Color.white * (1 - (depth / 5));
            //Gizmos.DrawWireCube(windField.GetCellWorldPositionCentre(kv.Key), Vector3.one * (windField.rootCellSize / Mathf.Pow(2, depth)));
            foreach (Vector3 v in GetCellVertices(windField.GetCellWorldPosition(kv.Key), windField.rootCellSize / Mathf.Pow(2, depth)))
            {
                Gizmos.DrawSphere(v, 0.05f * (windField.rootCellSize / Mathf.Pow(2, depth)));
            }

            //Gizmos.DrawSphere(windField.GetCellWorldPosition(kv.Key), 0.25f * (windField.rootCellSize / Mathf.Pow(2, depth)));
        }
    }
    
}
