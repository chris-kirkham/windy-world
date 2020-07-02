using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wind
{
    public class OctreeKey : HashKey<Vector3Int[]>
    {
        //constructor generates the hash key
        public OctreeKey(Vector3 pos, float rootCellSize, uint depth)
        {
            key = new Vector3Int[depth + 1]; //depth starts at 0 (root)
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
        public OctreeKey(Vector3 pos, float rootCellSize, uint depth) : this()
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

        public override bool Equals(HashKey<Vector3Int[]> other)
        {
            Vector3Int[] otherKey = other.GetKey();
            if (key.Length != otherKey.Length) return false;

            for (int i = 0; i < key.Length; i++)
            {
                if (key[i] != otherKey[i]) return false;
            }

            return true;
        }

        public Vector3Int this[int i]
        {
            get => key[i];
        }
    }
}