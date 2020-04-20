using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Displays visual debug information for wind fields.
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(WindField))]
public class WindFieldDebug : MonoBehaviour
{
    //Arrow model to visualise wind directions
    private WindField windField;
    private GameObject windArrow;
    private Dictionary<Vector3Int, GameObject> arrowField; 
    public bool showWindArrows = false;

    private float updateInterval = 0.1f;

    void Start()
    {
        windField = GetComponent<WindField>();
        windArrow = Resources.Load<GameObject>("Debug/Wind/WindArrow");
        arrowField = new Dictionary<Vector3Int, GameObject>();

        StartCoroutine(UpdateWindArrows());
    }

    private IEnumerator UpdateWindArrows()
    {
        while(showWindArrows)
        {
            //Update wind arrow visualisation with current wind directions. Definitely a faster way to do this
            //(e.g. have something in WindField that stores only updated wind directions (inc. new cells) in a List<key, windDir>
            //and only loop through those)
            foreach (KeyValuePair<Vector3Int, WindFieldCell> kv in windField.GetCellDict())
            {
                Vector3Int key = kv.Key;
                if (!arrowField.ContainsKey(key))
                {
                    arrowField[key] = Instantiate(windArrow);
                }

                arrowField[key].transform.position = windField.GetCellWorldPosition(key); 
                arrowField[key].transform.rotation = Quaternion.LookRotation(kv.Value.GetWind());

                yield return null;
            }

            yield return new WaitForSecondsRealtime(updateInterval);
        }

        arrowField.Clear();

        //why is this not necessary?
        //yield return null;
    }
}
