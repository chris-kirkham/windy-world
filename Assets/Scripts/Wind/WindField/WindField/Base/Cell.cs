using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wind
{
    public class Cell
    {
        //the cell's current wind vector
        private Vector3 wind;

        //The list of dynamic wind points in this cell
        private List<WindFieldPoint> windObjsStatic { get; }
        private List<WindFieldPoint> windObjsDynamic { get; }

        //the size of this cell
        public readonly float cellSize;

        //the world position of the least corner of this cell
        public readonly Vector3 worldPos;

        //the world position of the centre of this cell
        public readonly Vector3 worldPosCentre;

        public Cell(float cellSize, Vector3 worldPos)
        {
            wind = Vector3.zero;
            windObjsStatic = new List<WindFieldPoint>();
            windObjsDynamic = new List<WindFieldPoint>();
            this.cellSize = cellSize;
            this.worldPos = worldPos;
            float halfCellSize = cellSize / 2;
            worldPosCentre = worldPos + new Vector3(halfCellSize, halfCellSize, halfCellSize);
        }

        public Cell(Cell parent, Vector3 worldPos)
        {
            wind = Vector3.zero;
            windObjsStatic = new List<WindFieldPoint>();
            windObjsDynamic = new List<WindFieldPoint>();
            cellSize = parent.cellSize / 2;
            this.worldPos = worldPos;
            float halfCellSize = cellSize / 2;
            worldPosCentre = worldPos + new Vector3(halfCellSize, halfCellSize, halfCellSize);
        }

        //Adds given WindFieldPoint to the correct container by its static/dynamic type
        public void Add(WindFieldPoint obj)
        {
            switch (obj.mode)
            {
                case WindProducerMode.Dynamic:
                    AddDynamic(obj);
                    UpdateWind();
                    break;
                case WindProducerMode.Static:
                    AddStatic(obj);
                    UpdateWind();
                    break;
                default:
                    Debug.LogError("Unhandled ProducerMode! If you added a new one, update this function.");
                    break;
            }
        }

        private void AddDynamic(WindFieldPoint obj)
        {
            windObjsDynamic.Add(obj);
        }

        private void AddStatic(WindFieldPoint obj)
        {
            windObjsStatic.Add(obj);
            //windStatic += obj.wind;
        }

        //Update the current wind vector with the cell's static and dynamic wind objects
        private void UpdateWind()
        {
            Vector3 newWind = Vector3.zero;
            int currHighestPriority = int.MinValue;

            foreach(WindFieldPoint wp in windObjsStatic)
            {
                if(wp.priority == currHighestPriority)
                {
                    newWind += wp.wind;
                }
                else if(wp.priority > currHighestPriority)
                {
                    currHighestPriority = wp.priority;
                    newWind = wp.wind;
                }
            }

            foreach (WindFieldPoint wp in windObjsDynamic)
            {
                if (wp.priority == currHighestPriority)
                {
                    newWind += wp.wind;
                }
                else if (wp.priority > currHighestPriority)
                {
                    currHighestPriority = wp.priority;
                    newWind = wp.wind;
                }
            }

            wind = newWind;
        }

        public void ClearStatic()
        {
            windObjsStatic.Clear();
        }

        public void ClearDynamic()
        {
            windObjsDynamic.Clear();
        }

        //Returns true if the cell contains no dynamic wind points and its static wind vector is zero. 
        public bool IsEmpty()
        {
            return windObjsDynamic.Count == 0 && windObjsStatic.Count == 0;
        }

        /*----GETTERS----*/
        public Vector3 GetWind()
        {
            return wind;
        }

    }
}