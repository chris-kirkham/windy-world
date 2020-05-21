using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OctreeCell : WF_Cell
{
    //true if this cell has at least one child
    public bool hasChild;

    public OctreeCell(float cellSize, Vector3 worldPos) : base(cellSize, worldPos)
    {
        hasChild = false;
    }
}
