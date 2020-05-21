using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class WF_Cells : MonoBehaviour
{
    //Adds the given wind object to the cells structure
    public abstract void AddToCell(WF_WindPoint wp);

    public void AddToCell(WF_WindPoint[] windPoints)
    {
        foreach (WF_WindPoint wp in windPoints) AddToCell(wp);
    }

    //Tries to get a cell at the given key; assigns it to the out parameter and returns true if successful, else returns false.
    //public abstract bool TryGetCell(WF_HashKey key, out Cell cell);

    //Returns the total wind vector of the cell(s) at the given position
    public abstract bool TryGetWind(Vector3 pos, out Vector3 wind);

    /* Returns the total wind vector of the cell(s) at the given position, or Vector3.zero if there are none.
     * Does the same as TryGetWind() but doesn't return a bool indicating if the wind Vector was succesfully taken from cell(s) at pos
     * (note that a return value of Vector3.zero could either mean there was no cell found, or that there was a cell found and its wind vector is zero) */
    public abstract Vector3 GetWind(Vector3 pos);

    //Returns the world position of the least corner of the cell corresponding to the given key.
    //For example, a cell with bounds (4, 4, 4) to (8, 8, 8) would return (4, 4, 4)
    public abstract Vector3 GetCellWorldPos(WF_HashKey key);

    //Returns the world position of the centre of the cell corresponding to the given key.
    public abstract Vector3 GetCellWorldPosCentre(WF_HashKey key);

    public abstract void UpdateCells(List<WF_WindProducer> dynamicProducers);

    public abstract int CellCount();
}
