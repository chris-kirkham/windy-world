using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class WindArea : MonoBehaviour
{
    public Vector3 wind; //wind vector settable in editor. This will either be the local or world direction, depending on if using local
    private Vector3 windWorld; //world space wind; if not using windRelativeToLocalRotation, this will be the same as wind
    public bool relativeToLocalRotation = false;

    void Awake()
    {
        windWorld = wind;
    }

    void Update()
    {
        windWorld = transform.TransformDirection(wind);
    }

    /*----GETTERS AND SETTERS----*/
    //Gets world space wind vector
    public Vector3 GetWind()
    {
        return relativeToLocalRotation ? windWorld : wind;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, GetWind());
    }

}
