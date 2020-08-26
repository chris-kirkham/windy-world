using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wind
{
    public abstract class Cells : MonoBehaviour
    {
        //Adds the given wind object to the cells structure
        public abstract void AddToCell(WindFieldPoint wp);

        public void AddToCell(WindFieldPoint[] windPoints)
        {
            foreach (WindFieldPoint wp in windPoints) AddToCell(wp);
        }

        //Tries to get a cell at the given key; assigns it to the out parameter and returns true if successful, else returns false.
        //public abstract bool TryGetCell(WF_HashKey key, out Cell cell);

        //Returns the total wind vector of the cell(s) at the given position
        public abstract bool TryGetWind(Vector3 pos, out Vector3 wind);

        /* Returns the total wind vector of the cell(s) at the given position, or Vector3.zero if there are none.
         * Does the same as TryGetWind() but doesn't return a bool indicating if the wind Vector was succesfully taken from cell(s) at pos
         * (note that a return value of Vector3.zero could either mean there was no cell found, or that there was a cell found and its wind vector is zero) */
        public abstract Vector3 GetWind(Vector3 pos);

        public abstract List<Cell> GetCells();

        public abstract float GetCellSize();

        public abstract void UpdateCells(List<WindProducer> dynamicProducers);

        //Returns the number of cells in the data structure.
        public abstract int CellCount();

        /* DEBUG FUNCTIONS */
        //Returns the world position and size of all cells. I can only see this being used as a debug/visualisation helper
        public abstract List<Tuple<Vector3, float>> DEBUG_GetCellsWorldPosAndSize();

        //Returns the world positions of the centre of each cell.
        public abstract List<Vector3> DEBUG_GetCellWorldPositionsCentre();
    }
}