﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//based on https://unionassets.com/blog/spatial-hashing-295
/**
 * <summary>Represents a 3D wind field of equal-size cube cells, each containing a wind vector and, optionally, 
 * a list of objects in that cell (must have HashObject script attached to them)
 * </summary>
 */ 
public class WindField : MonoBehaviour
{
    public float rootCellSize = 1;
    public Vector3Int initNumRootCells;

    //If true, factor in a global wind vector when getting wind from cells; this vector will also be the default wind vector for new cells,
    //and for getting wind info from a world position at which there is no cell (if false, the latter two values will be Vector3.zero)
    public bool useGlobalWind = true; 
    public Vector3 globalWind; //the default global wind vector applied to all cells. 

    private Dictionary<WindField_HashKey, WindField_Cell> cells;
    private List<WindField_WindProducer> dynamicProducers;
    //private List<WindField_WindPoint> dynamicWindPoints; //stores dynamic-mode WindFieldPoints included in the wind field - these need to be updated every update interval
    private float updateInterval = 0.2f;

    //DEBUG
    private Vector3 DEBUG_testWFPoint1 = new Vector3(5.01f, 6.02f, 1.05f);
    private Vector3 DEBUG_testWFPoint2 = new Vector3(1.01f, 10.02f, 5.05f);

    private void Awake()
    {
        //create initial cells
        cells = new Dictionary<WindField_HashKey, WindField_Cell>(initNumRootCells.x * initNumRootCells.y * initNumRootCells.z);
        dynamicProducers = new List<WindField_WindProducer>();
        //dynamicWindPoints = new List<WindField_WindPoint>();

        Vector3Int halfNumCells = initNumRootCells / 2;
        for (int i = -halfNumCells.x; i < halfNumCells.x; i++)
        {
            for (int j = -halfNumCells.y; j < halfNumCells.y; j++)
            {
                for (int k = -halfNumCells.z; k < halfNumCells.z; k++)
                {
                    WindField_WindPoint point = new WindField_WindPoint
                    (
                        //new Vector3(i * rootCellSize, j * rootCellSize, k * rootCellSize) + new Vector3(Random.Range(0, rootCellSize), Random.Range(0, rootCellSize), Random.Range(0, rootCellSize)),
                        new Vector3(i * rootCellSize, j * rootCellSize, k * rootCellSize),
                        Vector3.forward,
                        0,
                        0,
                        WindProducerMode.Static
                    );

                    AddToCell(point);
                }
            }
        }

        //DEBUG
        WindField_WindPoint point1 = new WindField_WindPoint(DEBUG_testWFPoint1, Vector3.forward, 0, 5, WindProducerMode.Static);
        WindField_WindPoint point2 = new WindField_WindPoint(DEBUG_testWFPoint2, Vector3.forward, 0, 3, WindProducerMode.Static);
        //AddToCell(point1);
        //AddToCell(point2);
        Debug.Log("Cells: " + cells.Count);

        StartCoroutine(UpdateDynamic());
    }

    //Updates dynamic wind points for all cells in the wind field.
    private IEnumerator UpdateDynamic()
    {
        while(true)
        {
            foreach (WindField_Cell cell in cells.Values) cell.ClearDynamic();
            foreach (WindField_WindProducer p in dynamicProducers) AddToCell(p.GetWindFieldPoints());
            Debug.Log("dynamicProducers: " + string.Join(", ", dynamicProducers));

            //foreach (WindField_WindPoint windPoint in dynamicWindPoints) AddToCell(windPoint);
            yield return new WaitForSecondsRealtime(updateInterval);
        }
    }

    /*----PUBLIC FUNCTIONS----*/
    public void Include(WindField_WindProducer windProducer)
    {
        if (windProducer.mode == WindProducerMode.Dynamic) dynamicProducers.Add(windProducer);
        AddToCell(windProducer.GetWindFieldPoints());
    }

    /*
    //Includes the given WindFieldPoint in the wind field: if dynamic, adds the point to the dynamic wind objects list and then to its corresponding cell;
    //if static, just adds the point to its cell (since static wind points are never updated, there is no need to keep track of them outside their cells) 
    public void Include(WindField_WindPoint obj)
    {
        if(obj.mode == WindProducerMode.Dynamic) dynamicWindPoints.Add(obj);
        AddToCell(obj);
    }

    public void Include(WindField_WindPoint[] objs)
    {
        foreach (WindField_WindPoint obj in objs) Include(obj);
    }
    */

    //Adds a given WindFieldPoint to the cell corresponding to its position and depth, creating that cell and its parent(s) if they don't exist.
    //TODO: getting a new key each time is very inefficient compared to getting the deepest key initially and removing from that. 
    public void AddToCell(WindField_WindPoint obj)
    {
        WindField_HashKey key = KeyAtDepth(obj.position, obj.depth);

        if (cells.ContainsKey(key)) //if cell already exists, add the object to that cell
        {
            cells[key].Add(obj);
        }
        else
        {
            //if obj is at root, no need to check for parents
            if(obj.depth == 0)
            {
                cells.Add(key, new WindField_Cell());
                cells[key].Add(obj);
            }
            else
            {
                //find depth of deepest parent-to-be of cell obj will be added to
                uint depth = 0;
                while (cells.ContainsKey(KeyAtDepth(obj.position, depth))) depth++;
               
                //if no parent, create one at root depth
                if(depth == 0)
                {
                    WindField_HashKey k0 = KeyAtDepth(obj.position, 0);
                    cells.Add(k0, new WindField_Cell());
                    cells[k0].hasChild = true;
                    depth++;
                }

                //add parent cells down to obj.depth < 1
                while(depth < obj.depth)
                {
                    WindField_HashKey kDepth = KeyAtDepth(obj.position, depth);
                    cells.Add(kDepth, new WindField_Cell(cells[KeyAtDepth(obj.position, depth - 1)])); //initialise child cell with parent's wind info
                    cells[kDepth].hasChild = true;
                    depth++;
                }

                //add cell at obj.depth
                cells.Add(key, new WindField_Cell(cells[KeyAtDepth(obj.position, depth - 1)]));
                cells[key].Add(obj); //add object to newly-created cell
            }

        }
    }

    //Adds given WindFieldPoints to their corresponding cells (creates new cell(s) if none exist at generated hash position(s))
    public void AddToCell(WindField_WindPoint[] objs)
    {
        foreach (WindField_WindPoint obj in objs) AddToCell(obj);
    }
    
    /*----GETTERS AND SETTERS----*/
    //Gets the wind vector at the given world position. If no cell exists at the given position, returns (0, 0, 0)
    public Vector3 GetWind(Vector3 pos)
    {
        WindField_Cell cell;
        return TryGetCell(pos, out cell) ? cell.GetWind() : (useGlobalWind ? globalWind : Vector3.zero);   
    }

    public Dictionary<WindField_HashKey, WindField_Cell> GetCellDict()
    {
        return cells;
    }
    
    //Get the world position of the cell with the given hash key. Note that this returns the
    //leastmost corner of the cell, not its centre (so a cell with bounds from (0,0,0) to (1,1,1)
    //would return (0,0,0), not (0.5,0.5,0.5))
    public Vector3 GetCellWorldPosition(WindField_HashKey key)
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
    public Vector3 GetCellWorldPositionCentre(WindField_HashKey key)
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
        float halfFinalCellSize = k.Length == 1 ? rootCellSize / 2 : cellSize / 2;
        return worldPos + new Vector3(halfFinalCellSize, halfFinalCellSize, halfFinalCellSize);
    }


    /*----PRIVATE UTILITY FUNCTIONS----*/


    //Gets the wind field cell at the given position, or null if no such cell exists
    private bool TryGetCell(Vector3 pos, out WindField_Cell cell)
    {
        //if root cell exists at this key, cell will be set to it; if not, there is no cell at this position so return false
        if (cells.TryGetValue(KeyAtDepth(pos, 0), out cell))
        {
            uint depth = 1;
            while (cell.hasChild)
            {
                //cell = cells[KeyAtDepth(pos, depth)]; //this may fail if using the method where a cell doesn't need to have all eight children
                
                //if using method where a cell may have children but not necessarily all eight, this is necessary
                WindField_HashKey key = KeyAtDepth(pos, depth);
                if(cells.ContainsKey(key))
                {
                    cell = cells[key];
                    depth++;
                }
                else
                {
                    return true;
                }
                
            }

            return true;
        }

        return false;
    }
        
    //Generate the key at a given position and depth
    private WindField_HashKey KeyAtDepth(Vector3 pos, uint depth)
    {
        return new WindField_HashKey(pos, rootCellSize, depth);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(DEBUG_testWFPoint1, 0.1f);
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(DEBUG_testWFPoint2, 0.1f);
    }
}
