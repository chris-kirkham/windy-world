using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hash key for the wind field. Used to encapsulate hash key generation so the WindField class doesn't have to worry about how the key is generated
/// </summary>
public struct WFHashKey
{
    private Vector3Int key;
    public int x, y, z; //Convenience variables for accessing key's coords

    //constructor generates the hash key
    public WFHashKey(Vector3 pos, float cellSize) : this()
    {
        key = new Vector3Int(FastFloor(pos.x / cellSize), FastFloor(pos.y / cellSize), FastFloor(pos.z / cellSize));
        x = key.x;
        y = key.y;
        z = key.z;
    }

    /*
    public WFHashKey(WindFieldPoint wfObj) : this()
    {
        //gen key using WindFieldPoint info
    }
    */

    public override bool Equals(object other)
    {
        if (!(other is WFHashKey)) return false;

        return Equals((WFHashKey)other);
    }

    public bool Equals(WFHashKey other)
    {
        return other.key == key;
    }

    public override int GetHashCode()
    {
        return key.GetHashCode();
    }

    public static bool operator ==(WFHashKey l, WFHashKey r)
    {
        return l.key == r.key;
    }

    public static bool operator !=(WFHashKey l, WFHashKey r)
    {
        return l.key != r.key;
    }

    //from https://www.codeproject.com/Tips/700780/Fast-floor-ceiling-functions
    private int FastFloor(float f)
    {
        return (int)(f + 32768f) - 32768;
    }
}
