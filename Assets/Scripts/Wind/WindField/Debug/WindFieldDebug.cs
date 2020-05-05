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

    private float updateInterval = 0.001f;

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

                arrowField[key].transform.position = windField.GetCellWorldPositionCentre(key);

                Vector3 wind = kv.Value.GetWind();
                if(wind != Vector3.zero) arrowField[key].transform.rotation = Quaternion.LookRotation(wind);
                
                yield return null;
            }

            yield return new WaitForSecondsRealtime(updateInterval);
        }

        arrowField.Clear();

        //yield return null; //why is this not necessary?
    }

    private void OnDrawGizmos()
    {
        foreach (KeyValuePair<WFHashKey, WindFieldCell> kv in windField.GetCellDict())
        {
            float depth = kv.Key.GetKey().Length;
            Gizmos.color = Color.HSVToRGB(1 / depth, 1, 1);
            //Gizmos.color = Color.white * (1 / depth);
            Gizmos.DrawWireCube(windField.GetCellWorldPositionCentre(kv.Key), Vector3.one * (windField.rootCellSize / Mathf.Pow(2, depth)));
            //Gizmos.DrawSphere(windField.GetCellWorldPosition(kv.Key), 0.25f * (windField.rootCellSize / Mathf.Pow(2, depth)));
        }
    }
    
}
