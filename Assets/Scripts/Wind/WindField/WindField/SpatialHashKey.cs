using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wind
{
    public class SpatialHashKey : HashKey<Vector3Int>
    {
        public SpatialHashKey(Vector3 pos, float cellSize)
        {
            key = GetCellCoord(pos, cellSize);
        }

        public override int GetHashCode()
        {
            return key.GetHashCode();
        }

        public override bool Equals(HashKey<Vector3Int> other)
        {
            return key == other.GetKey();
        }

    }
}