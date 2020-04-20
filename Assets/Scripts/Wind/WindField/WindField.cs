using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//based on https://unionassets.com/blog/spatial-hashing-295
/**
 * <summary>Represents a 3D wind field of equal-size cube cells, each containing a wind vector and, optionally, 
 * a list of objects in that cell (must have HashObject script attached to them)
 * </summary>
 */ 
public class WindField : SpatialHash<WindFieldCell>
{
    void Update()
    {
    }

    void OnDrawGizmos()
    {
    }
}
