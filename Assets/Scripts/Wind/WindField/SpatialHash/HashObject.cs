using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///<summary>
///Script to attach to an object to be stored in a spatial hash.
///Adds this object to the attached SpatialHash on Start; removes it on destroy.
///</summary>
public class HashObject<T> : MonoBehaviour where T : HashCell, new()
{
    public string hashName; //name of spatial hash object to find
    private SpatialHash<T> hash; //hash in which to store this object

    void Start()
    {

        hash = GameObject.Find(hashName).GetComponent<SpatialHash<T>>();

        if (hash == null)
        {
            Debug.LogError("HashObject cannot find SpatialHash by name " + hashName + "!");
        }
        else
        {
            hash.Include(gameObject);
        }
    }

    void OnDestroy()
    {
        hash.Remove(gameObject);
    }
}
