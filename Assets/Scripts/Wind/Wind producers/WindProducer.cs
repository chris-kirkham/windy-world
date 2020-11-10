using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;

namespace Wind
{
    /// <summary>
    /// Abstract base class for wind-producing objects that should be added to a wind field. 
    /// </summary>
    [ExecuteAlways]
    public abstract class WindProducer : MonoBehaviour
    {
        public ComputeWindField windField;
        public WindProducerMode mode = WindProducerMode.Static;
        public int priority = 0;
        public bool active = true;
        public float updateInterval = 0f;

        protected ComputeBuffer windPointsBuffer;
        protected float cellSize; //size of cells of given wind field

        //N.B. OnEnable() rather than Start() because wind producers need to be added to their wind field before the wind field tries to access them (which it does on Start()).
        //This works but the flow of it isn't clear, ESPECIALLY BECAUSE THE WIND FIELD NEEDS TO INITIALISE ITS PRODUCER LISTS BEFORE THIS TRIES TO ADD THEM (which it does in Awake())
        //so I should try to find a better way to do it
        protected virtual void OnEnable()
        {
            if (windField == null)
            {
                throw new NullReferenceException("No wind field given for WindFieldProducer " + ToString() + "!");
            }

            cellSize = windField.GetCellSize();
            windPointsBuffer = CalcWindFieldPoints();
            AddToWindField();
            StartCoroutine(UpdateProducer());
        }

        /*
        //The wind points buffer is updated on validate in case anything relevant to it was changed in the inspector
        protected virtual void OnValidate()
        {
            windPointsBuffer = CalcWindFieldPoints();
        }
        */

        private void OnDisable()
        {
            if (windPointsBuffer != null) windPointsBuffer.Release();
        }

        /* Returns an approximation of the wind producer in the form of individual WindFieldPoint(s) to be added to wind field cells.
         *
         * Each wind producer must specify one or more WindFieldPoints (a point in space which holds a wind vector 
         * and other data about how the wind field should handle that point) in this function; the wind field adds these points
         * to its corresponding positional cells.    
         *
         * The points returned by this function should be an approximation of the wind producer's shape and wind vector(s). */
        protected abstract ComputeBuffer CalcWindFieldPoints();

        //By default, simply recalculates the wind points.
        protected virtual void UpdateWindFieldPoints()
        {
            windPointsBuffer = CalcWindFieldPoints();
        }

        //Calls UpdateWindFieldPoints at the given interval.
        private IEnumerator UpdateProducer()
        {
            while(true)
            {
                UpdateWindFieldPoints();
                yield return new WaitForSecondsRealtime(updateInterval);     
            }
        }

        public ComputeBuffer GetWindFieldPointsBuffer()
        {
            return windPointsBuffer;
        }

        public void AddToWindField()
        {
            Debug.Log("adding " + ToString() + " to wind field");
            windField.Include(this);
        }

        /*
        //Removes this object from the wind field.
        public void RemoveFromWindField()
        {
        }
        */
        
        /*
        private void OnDrawGizmos()
        {
            WindFieldPoint[] points = new WindFieldPoint[windPointsBuffer.count];
            windPointsBuffer.GetData(points);
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 windDir = points[i].wind.normalized;
                Gizmos.color = new Color(Mathf.Abs(windDir.x), Mathf.Abs(windDir.y), Mathf.Abs(windDir.z));
                Gizmos.DrawRay(points[i].position, windDir);
            }
        }
        */

        public override string ToString()
        {
            return GetType() + " at " + transform.position;
        }
    }
}