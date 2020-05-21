using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OctreeCells : WF_Cells
{
    public float rootCellSize = 1;
    public Vector3Int initNumRootCells;

    private Dictionary<WF_HashKey, OctreeCell> cells;

    //Adds a given WindFieldPoint to the cell corresponding to its position and depth, creating that cell and its parent(s) if they don't exist.
    //TODO: getting a new key each time is very inefficient compared to getting the deepest key initially and removing elements from that. 
    public override void AddToCell(WF_WindPoint obj)
    {
        WF_HashKey key = KeyAtDepth(obj.position, obj.depth);
        //Debug.Log("Adding object at " + obj.position + "at depth " + obj.depth + "; key = " + key);

        if (cells.ContainsKey(key)) //if cell already exists, add the object to that cell
        {
            cells[key].Add(obj);
        }
        else
        {
            //if obj is at root, no need to check for parents
            if (obj.depth == 0)
            {
                cells.Add(key, new OctreeCell(rootCellSize, GetCellWorldPos(key)));
                cells[key].Add(obj);
            }
            else
            {
                float cellSize = rootCellSize; //track cell size with depth (halves with each depth increment)

                //find depth of deepest parent-to-be of cell obj will be added to
                uint depth = 0;
                while (cells.ContainsKey(KeyAtDepth(obj.position, depth))) depth++;

                //add parent cells down to obj.depth < 1
                while (depth < obj.depth)
                {
                    WF_HashKey kDepth = KeyAtDepth(obj.position, depth);
                    cells.Add(kDepth, new OctreeCell(cellSize, GetCellWorldPos(kDepth)));
                    cells[kDepth].hasChild = true;

                    depth++;
                    cellSize /= 2;
                }

                //add cell at obj.depth
                cells.Add(key, new OctreeCell(cellSize, GetCellWorldPos(key)));
                cells[key].Add(obj); //add object to newly-created cell
            }

        }
    }

    /*
    public override bool TryGetCell(WF_HashKey key, out WF_Cell cell)
    {
        OctreeCell c;
        if(cells.TryGetValue(key, out c))
        {
            cell = c;
            return true;
        }
        else
        {
            cell = c;
            return false;
        }
    }
    */

    public override bool TryGetWind(Vector3 pos, out Vector3 wind)
    {
        //adds the wind vectors from root cell down to its deepest child at the given position, plus the global wind vector (if using).
        //If no root at this position, returns either the global wind vec or zero.
        OctreeCell cell;
        wind = Vector3.zero;

        if (cells.TryGetValue(KeyAtDepth(pos, 0), out cell))
        {
            wind += cell.GetWind();
            uint depth = 1;
            while (cell.hasChild)
            {
                //cell = cells[KeyAtDepth(pos, depth)]; //this may fail if using the method where a cell doesn't need to have all eight children

                //if using method where a cell may have children but not necessarily all eight, this is necessary
                WF_HashKey key = KeyAtDepth(pos, depth);
                if (cells.ContainsKey(key))
                {
                    cell = cells[key];
                    wind += cell.GetWind();
                    depth++;
                }
                else
                {
                    break;
                }
            }

            return true;
        }

        return false;
    }

    public override Vector3 GetWind(Vector3 pos)
    {
        //return TryGetCell(pos, out cell) ? cell.GetWind() : (useGlobalWind ? globalWind : Vector3.zero);   

        //adds the wind vectors from root cell down to its deepest child at the given position, plus the global wind vector (if using).
        //If no root at this position, returns either the global wind vec or zero.
        OctreeCell cell;
        Vector3 wind = Vector3.zero;

        if (cells.TryGetValue(KeyAtDepth(pos, 0), out cell))
        {
            wind += cell.GetWind();
            uint depth = 1;
            while (cell.hasChild)
            {
                //cell = cells[KeyAtDepth(pos, depth)]; //this may fail if using the method where a cell doesn't need to have all eight children

                //if using method where a cell may have children but not necessarily all eight, this is necessary
                WF_HashKey key = KeyAtDepth(pos, depth);
                if (cells.ContainsKey(key))
                {
                    cell = cells[key];
                    wind += cell.GetWind();
                    depth++;
                }
                else
                {
                    break;
                }
            }
        }

        return wind;
    }

    //Get the world position of the cell with the given hash key. Note that this returns the
    //leastmost corner of the cell, not its centre (so a cell with bounds from (0,0,0) to (1,1,1)
    //would return (0,0,0), not (0.5,0.5,0.5))
    public override Vector3 GetCellWorldPos(WF_HashKey key)
    {
        Vector3Int[] k = key.GetKey();
        Vector3 worldPos = (Vector3)k[0] * rootCellSize;
        float cellSize = rootCellSize / 2;

        for (int i = 1; i < k.Length; i++)
        {
            worldPos += (Vector3)k[i] * cellSize;
            cellSize /= 2;
        }

        return worldPos;
    }

    //Get the world position of the centre of the cell with the given hash key.
    public override Vector3 GetCellWorldPosCentre(WF_HashKey key)
    {
        Vector3Int[] k = key.GetKey();
        Vector3 worldPos = ((Vector3)k[0] * rootCellSize);
        float cellSize = rootCellSize / 2;

        for (int i = 1; i < k.Length; i++)
        {
            worldPos += (Vector3)k[i] * cellSize;
            cellSize /= 2;
        }

        //add half of deepest cell size in each dimension to get centre of deepest cell
        return worldPos + new Vector3(cellSize, cellSize, cellSize);
    }

    public override void UpdateCells(List<WF_WindProducer> dynamicProducers)
    {
        //clear dynamic wind points in cells and re-add updated points
        foreach (WF_Cell cell in cells.Values) cell.ClearDynamic();
        foreach (WF_WindProducer p in dynamicProducers) AddToCell(p.GetWindFieldPoints());

        //delete any now-empty non-parent cells
        List<WF_HashKey> cellsToRemove = new List<WF_HashKey>();
        foreach (KeyValuePair<WF_HashKey, OctreeCell> cell in cells)
        {
            if (cell.Value.IsEmpty() && !cell.Value.hasChild) cellsToRemove.Add(cell.Key);
        }
        foreach (WF_HashKey key in cellsToRemove)
        {
            //TODO: CHECK IF PARENT NOW HAS NO CHILDREN AND SET ITS hasChild TO FALSE IF SO (extend cell from monobehaviour and set it to check children/set this stuff automatically on update?)
            cells.Remove(key);
        }
    }

    public override int CellCount()
    {
        return cells.Count;
    }

    /* UTILITY FUNCTION(S) */
    private WF_HashKey KeyAtDepth(Vector3 pos, uint depth)
    {
        return new WF_HashKey(pos, rootCellSize, depth);
    }
}
