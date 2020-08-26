using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wind
{
    /// <summary>
    /// Hash key for the wind field. Used to encapsulate hash key generation so the WindField class doesn't have to worry about how the key is generated
    /// </summary>
    public abstract class HashKey<KeyType>
    {
        protected KeyType key;

        public KeyType GetKey()
        {
            return key;
        }

        public abstract override int GetHashCode();

        public override bool Equals(object other)
        {
            if (!(other is HashKey<KeyType>)) return false;

            return Equals((HashKey<KeyType>)other);
        }

        public abstract bool Equals(HashKey<KeyType> other);

        public static bool operator ==(HashKey<KeyType> l, HashKey<KeyType> r)
        {
            return Equals(l, r);
        }

        public static bool operator !=(HashKey<KeyType> l, HashKey<KeyType> r)
        {
            return !Equals(l, r);
        }

        //Get the cell index for a given position and cell size
        protected Vector3Int GetCellCoord(Vector3 pos, float cellSize)
        {
            return new Vector3Int(FastFloor(pos.x / cellSize), FastFloor(pos.y / cellSize), FastFloor(pos.z / cellSize));
        }

        //from https://www.codeproject.com/Tips/700780/Fast-floor-ceiling-functions
        protected int FastFloor(float f)
        {
            return (int)(f + 32768f) - 32768;
        }
    }
}