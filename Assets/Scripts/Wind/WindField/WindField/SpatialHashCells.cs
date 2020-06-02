using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpatialHashCells : WF_Cells
{
    [Range(0.1f, 100f)] public float cellSize = 1;

    private Dictionary<SpatialHashKey, WF_Cell> cells;

    private void Awake()
    {
        cells = new Dictionary<SpatialHashKey, WF_Cell>();
    }

    //Adds the given wind object to the cells structure
    public override void AddToCell(WF_WindPoint wp)
    {
        SpatialHashKey key = new SpatialHashKey(wp.position, cellSize);

        if (cells.ContainsKey(key))
        {
            cells[key].Add(wp);
        }
        else
        {
            cells.Add(key, new WF_Cell(cellSize, GetCellWorldPos(key)));
            cells[key].Add(wp);
        }
    }

    //Returns the total wind vector of the cell(s) at the given position
    public override bool TryGetWind(Vector3 pos, out Vector3 wind)
    {
        WF_Cell cell;
        if (cells.TryGetValue(new SpatialHashKey(pos, cellSize), out cell))
        {
            wind = cell.GetWind();
            return true;
        }
        else
        {
            wind = Vector3.zero;
            return false;
        }
    }

    /* Returns the total wind vector of the cell(s) at the given position, or Vector3.zero if there are none.
     * Does the same as TryGetWind() but doesn't return a bool indicating if the wind Vector was succesfully taken from cell(s) at pos
     * (note that a return value of Vector3.zero could either mean there was no cell found, or that there was a cell found and its wind vector is zero) */
    public override Vector3 GetWind(Vector3 pos)
    {
        WF_Cell cell;
        return cells.TryGetValue(new SpatialHashKey(pos, cellSize), out cell) ? cell.GetWind() : Vector3.zero;
    }

    public override List<WF_Cell> GetCells()
    {
        return cells.Values.ToList();
    }

    //Returns the world position of the least corner of the cell corresponding to the given key.
    //For example, a cell with bounds (4, 4, 4) to (8, 8, 8) would return (4, 4, 4)
    private Vector3 GetCellWorldPos(SpatialHashKey key)
    {
        return (Vector3)key.GetKey() * cellSize;
    }

    //Returns the world position of the centre of the cell corresponding to the given key.
    private Vector3 GetCellWorldPosCentre(SpatialHashKey key)
    {
        float halfCellSize = cellSize / 2;
        return ((Vector3)key.GetKey() * cellSize) + new Vector3(halfCellSize, halfCellSize, halfCellSize);
    }

    public override void UpdateCells(List<WF_WindProducer> dynamicProducers)
    {
        foreach (WF_Cell cell in cells.Values) cell.ClearDynamic();
        foreach (WF_WindProducer p in dynamicProducers) AddToCell(p.GetWindFieldPoints());
    }

    //Returns the number of cells in the data structure.
    public override int CellCount()
    {
        return cells.Count;
    }

    /* DEBUG FUNCTIONS */
    //Returns the world position and size of all cells. I can only see this being used as a debug/visualisation helper
    public override List<Tuple<Vector3, float>> DEBUG_GetCellsWorldPosAndSize()
    {
        throw new NotImplementedException();
    }

    //Returns the world positions of the centre of each cell.
    public override List<Vector3> DEBUG_GetCellWorldPositionsCentre()
    {
        throw new NotImplementedException();
    }

}
