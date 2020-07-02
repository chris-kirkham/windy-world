using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Wind
{
    public class OctreeCells : Cells
    {
        [Range(0.1f, 100f)] public float rootCellSize = 1;
        public Vector3Int initNumRootCells;

        private Dictionary<OctreeKey, OctreeCell> cells;

        private void Awake()
        {
            cells = new Dictionary<OctreeKey, OctreeCell>();
        }

        //Adds a given WindFieldPoint to the cell corresponding to its position and depth, creating that cell and its parent(s) if they don't exist.
        //TODO: getting a new key each time is very inefficient compared to getting the deepest key initially and removing elements from that. 
        public override void AddToCell(WindFieldPoint obj)
        {
            OctreeKey key = KeyAtDepth(obj.position, obj.depth);
            //Debug.Log("Adding object at " + obj.position + " at depth " + obj.depth + "; key = [" + string.Join(", ", key.GetKey()) + "]");

            if (cells.ContainsKey(key)) //if cell already exists, add the object to that cell
            {
                cells[key].Add(obj);
            }
            else
            {
                //if obj is at root, no need to check for parents
                if (obj.depth == 0)
                {
                    cells.Add(key, new OctreeCell(rootCellSize, GetCellWorldPos(key), 0));
                    cells[key].Add(obj);
                }
                else
                {
                    float cellSize = rootCellSize; //track cell size with depth (halves with each depth increment)

                    //find depth of deepest parent-to-be of cell obj will be added to
                    uint depth = 0;
                    while (cells.ContainsKey(KeyAtDepth(obj.position, depth)))
                    {
                        depth++;
                        cellSize /= 2;
                    }

                    //add parent cells down to obj.depth < 1
                    while (depth < obj.depth)
                    {
                        OctreeKey kDepth = KeyAtDepth(obj.position, depth);
                        cells.Add(kDepth, new OctreeCell(cellSize, GetCellWorldPos(kDepth), depth));
                        cells[kDepth].hasChild = true;

                        depth++;
                        cellSize /= 2;
                    }

                    //add cell at obj.depth
                    cells.Add(key, new OctreeCell(cellSize, GetCellWorldPos(key), obj.depth));
                    cells[key].Add(obj); //add object to newly-created cell
                }
            }

            Debug.Log("Cell added: " + cells[key] + "; wind: " + cells[key].GetWind());
        }

        /*
        public override bool TryGetCell(OctreeKey key, out WF_Cell cell)
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
                    OctreeKey key = KeyAtDepth(pos, depth);
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
                    OctreeKey key = KeyAtDepth(pos, depth);
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

        public override List<Cell> GetCells()
        {
            return cells.Values.ToList<Cell>();
        }

        public override float GetCellSize()
        {
            return rootCellSize;
        }

        //Get the world position of the cell with the given hash key. Note that this returns the
        //leastmost corner of the cell, not its centre (so a cell with bounds from (0,0,0) to (1,1,1)
        //would return (0,0,0), not (0.5,0.5,0.5))
        private Vector3 GetCellWorldPos(OctreeKey key)
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
        private Vector3 GetCellWorldPosCentre(OctreeKey key)
        {
            Vector3Int[] k = key.GetKey();
            Vector3 worldPos = (Vector3)k[0] * rootCellSize;
            float cellSize = rootCellSize / 2;

            for (int i = 1; i < k.Length; i++)
            {
                worldPos += (Vector3)k[i] * cellSize;
                cellSize /= 2;
            }

            //add half of deepest cell size in each dimension to get centre of deepest cell
            return worldPos + new Vector3(cellSize, cellSize, cellSize);
        }


        public override void UpdateCells(List<WindProducer> dynamicProducers)
        {
            //clear dynamic wind points in cells and re-add updated points
            foreach (OctreeCell cell in cells.Values) cell.ClearDynamic();
            foreach (WindProducer p in dynamicProducers)
            {
                AddToCell(p.GetWindFieldPoints());
                //Debug.Log(p + ": " + string.Join(", ", p.GetWindFieldPoints().ToList()));
            }

            /*
            //delete any now-empty non-parent cells
            List<OctreeKey> cellsToRemove = new List<OctreeKey>();
            foreach (KeyValuePair<OctreeKey, OctreeCell> cell in cells)
            {
                if (cell.Value.IsEmpty() && !cell.Value.hasChild) cellsToRemove.Add(cell.Key);
            }
            foreach (OctreeKey key in cellsToRemove)
            {
                //TODO: CHECK IF PARENT NOW HAS NO CHILDREN AND SET ITS hasChild TO FALSE IF SO (extend cell from monobehaviour and set it to check children/set this stuff automatically on update?)
                cells.Remove(key);
            }
            */
        }

        public override int CellCount()
        {
            return cells.Count;
        }

        /* DEBUG FUNCTIONS */
        public override List<Tuple<Vector3, float>> DEBUG_GetCellsWorldPosAndSize()
        {
            List<Tuple<Vector3, float>> worldPosAndSize = new List<Tuple<Vector3, float>>(cells.Count);

            foreach (OctreeCell cell in cells.Values)
            {
                worldPosAndSize.Add(new Tuple<Vector3, float>(cell.worldPos, cell.cellSize));
            }

            return worldPosAndSize;
        }


        public override List<Vector3> DEBUG_GetCellWorldPositionsCentre()
        {
            List<Vector3> worldPositionsCentre = new List<Vector3>(cells.Count);

            foreach (OctreeCell cell in cells.Values) worldPositionsCentre.Add(cell.worldPosCentre);

            return worldPositionsCentre;
        }

        /* UTILITY FUNCTION(S) */
        private OctreeKey KeyAtDepth(Vector3 pos, uint depth)
        {
            return new OctreeKey(pos, rootCellSize, depth);
        }
    }
}