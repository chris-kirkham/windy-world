using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OctreeCell : WF_Cell
{
    //true if this cell has at least one child
    public bool hasChild;

    //cell depth (0 = root)
    public readonly uint depth;

    public OctreeCell(float cellSize, Vector3 worldPos, uint depth) : base(cellSize, worldPos)
    {
        hasChild = false;
        this.depth = depth;
    }

    public OctreeCell(OctreeCell parent, Vector3 worldPos) : base(parent.cellSize / 2, worldPos)
    {
        hasChild = false;
        depth = parent.depth + 1;
    }
}
