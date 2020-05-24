using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpatialHashKey : WF_HashKey<Vector3Int>
{
    private Vector3Int key;

    public SpatialHashKey(Vector3 pos, float cellSize)
    {
        key = GetCellCoord(pos, cellSize);
    }

    public override int GetHashCode()
    {
        return key.GetHashCode();
    }

    public override bool Equals(WF_HashKey<Vector3Int> other)
    {
        return key == other.GetKey();
    }

}
