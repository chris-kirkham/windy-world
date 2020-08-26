using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wind
{
    /// <summary>
    /// Blittable struct version of SpatialHashKey, for use in C# Jobs
    /// </summary>
    public struct SpatialHashKeyBlittable : IHashKey<Vector3Int>, IEquatable<SpatialHashKeyBlittable>
    {
        private Vector3Int key;
        
        public SpatialHashKeyBlittable(Vector3 pos, float cellSize)
        {
            key = GetCellCoord(pos, cellSize);
        }

        public Vector3Int GetKey()
        {
            return key;
        }

        public override int GetHashCode()
        {
            return key.GetHashCode();
        }

        public override bool Equals(object other)
        {
            if (!(other is IHashKey<Vector3Int>)) return false;

            return Equals((IHashKey<Vector3Int>)other);
        }

        public bool Equals(IHashKey<Vector3Int> other)
        {
            return key == other.GetKey();
        }

        public bool Equals(SpatialHashKeyBlittable other)
        {
            return key == other.GetKey();
        }

        public static bool operator ==(SpatialHashKeyBlittable l, IHashKey<Vector3Int> r)
        {
            return Equals(l, r);
        }

        public static bool operator !=(SpatialHashKeyBlittable l, IHashKey<Vector3Int> r)
        {
            return !Equals(l, r);
        }

        //Get the cell index for a given position and cell size
        private static Vector3Int GetCellCoord(Vector3 pos, float cellSize)
        {
            return new Vector3Int(FastFloor(pos.x / cellSize), FastFloor(pos.y / cellSize), FastFloor(pos.z / cellSize));
        }

        //from https://www.codeproject.com/Tips/700780/Fast-floor-ceiling-functions
        private static int FastFloor(float f)
        {
            return (int)(f + 32768f) - 32768;
        }

    }
}

