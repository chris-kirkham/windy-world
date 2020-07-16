using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wind
{
    /// <summary>
    /// An object to be added to a wind field, representing wind information at a point in world space
    /// (and some info about how the wind field should handle including/updating the point in the field).
    /// </summary>
    public class WindFieldPoint
    {
        //Position in space
        public Vector3 position;

        //Wind vector
        public Vector3 wind;

        //depth from the root node at which to add this point to the wind field
        public uint depth;

        //The behaviour mode of the wind producer that created this object. See WindProducerMode script for a detailed description
        public WindProducerMode mode;

        //The wind vector of WindFieldPoints of a higher priority will override that of those of a lower priority in the same cell;
        //the wind vector of points with the same priority will be added to find that cell's overall wind vector
        public int priority;

        public WindFieldPoint(Vector3 position, Vector3 wind, WindProducerMode mode, int priority = 0, uint depth = 0)
        {
            this.position = position;
            this.wind = wind;
            this.priority = 0;
            this.depth = 0;
            this.mode = mode;
        }

        public WindFieldPoint(Vector3 position, ref Vector3 wind, WindProducerMode mode, int priority = 0, uint depth = 0)
        {
            this.position = position;
            this.wind = wind;
            this.priority = 0;
            this.depth = 0;
            this.mode = mode;
        }

        public override string ToString()
        {
            //return position.ToString();
            return "Pos = " + position + ", wind dir = " + wind;
        }
    }
}