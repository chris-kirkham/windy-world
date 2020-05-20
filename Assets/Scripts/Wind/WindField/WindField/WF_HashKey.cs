using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hash key for the wind field. Used to encapsulate hash key generation so the WindField class doesn't have to worry about how the key is generated
/// </summary>
public struct WF_HashKey
{
    private Vector3Int[] key;

    //constructor generates the hash key
    public WF_HashKey(Vector3 pos, float rootCellSize, uint depth) : this()
    {
        this.key = new Vector3Int[depth + 1]; //depth starts at 0 (root)
        //Vector3[] DEBUG_cellPositions = new Vector3[depth + 1];

        //root cells are not relative to any parent
        key[0] = GetCellCoord(pos, rootCellSize);

        //DEBUG_cellPositions[0] = (Vector3)key[0] * rootCellSize;

        Vector3 parentPos = (Vector3)key[0] * rootCellSize;
        float cellSize = rootCellSize / 2;

        for (int i = 1; i <= depth; i++)
        {
            Vector3 relPos = pos - parentPos; //all child cell keys should be relative to their parent (so they will be in the range (0,0,0)..(1,1,1))
            key[i] = GetCellCoord(relPos, cellSize);
            parentPos += (Vector3)key[i] * cellSize;

            //DEBUG_cellPositions[i] = DEBUG_cellPositions[i - 1] + (Vector3)key[i] * thisCellSize;

            cellSize /= 2;
        }

        //Debug.Log("Cell positions = [" + string.Join(", ", DEBUG_cellPositions) + "]");
        //Debug.Log("Key = [" + string.Join(", ", key) + "]");
    }

    /*
    public WFHashKey(Vector3 pos, float rootCellSize, uint depth) : this()
    {
        this.key = new Vector3Int[depth + 1]; //depth starts at 0 (root)
        Vector3Int flooredPos = new Vector3Int(FastFloor(pos.x), FastFloor(pos.y), FastFloor(pos.z));

        //root cells are not relative to any parent
        key[0] = flooredPos / rootCellSize;
        Vector3 parentPos = (Vector3)key[0] * rootCellSize;
        float cellSize = rootCellSize / 2;

        for (int i = 1; i <= depth; i++)
        {
            Vector3 relPos = pos - parentPos; //all child cell keys should be relative to their parent (so they will be in the range (0,0,0)..(1,1,1))
            key[i] = GetCellCoord(relPos, thisCellSize);
            parentPos += (Vector3)key[i] * thisCellSize;
            cellSize /= 2;
        }
    }
    */

    /*
    public WFHashKey(WindFieldPoint wfObj) : this()
    {
        //gen key using WindFieldPoint info
    }
    */

    public override bool Equals(object other)
    {
        if (!(other is WF_HashKey)) return false;

        return Equals((WF_HashKey)other);
    }

    public bool Equals(WF_HashKey other)
    {
        Vector3Int[] otherKey = other.GetKey();
        if (key.Length != otherKey.Length) return false;

        for (int i = 0; i < key.Length; i++)
        {
            if (key[i] != otherKey[i]) return false;
        }

        return true;
    }

    //https://stackoverflow.com/questions/263400/what-is-the-best-algorithm-for-overriding-gethashcode
    public override int GetHashCode()
    {
        int hash = 17;
        foreach (Vector3Int elem in key)
        {
            hash = hash * 29 * elem.GetHashCode();
        }

        return hash;
    }

    public static bool operator ==(WF_HashKey l, WF_HashKey r)
    {
        return Equals(l, r);
    }

    public static bool operator !=(WF_HashKey l, WF_HashKey r)
    {
        return !Equals(l, r);
    }

    public Vector3Int this[int i]
    {
        get => key[i];
    }

    public Vector3Int[] GetKey()
    {
        return key;
    }

    //Get the cell index for a given position and cell size
    private Vector3Int GetCellCoord(Vector3 pos, float cellSize)
    {
        return new Vector3Int(FastFloor(pos.x / cellSize), FastFloor(pos.y / cellSize), FastFloor(pos.z / cellSize));
    }

    //from https://www.codeproject.com/Tips/700780/Fast-floor-ceiling-functions
    private int FastFloor(float f)
    {
        return (int)(f + 32768f) - 32768;
    }

}