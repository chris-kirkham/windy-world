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
public class WindField : MonoBehaviour
{
    public float rootCellSize = 1;
    public Vector3Int initNumRootCells;

    public Vector3 globalWind; //the default global wind vector applied to all cells. 

    private Dictionary<WFHashKey, WindFieldCell> cells;

    private void Awake()
    {
        //create initial cells
        cells = new Dictionary<WFHashKey, WindFieldCell>(initNumRootCells.x * initNumRootCells.y * initNumRootCells.z);
        Vector3Int halfNumCells = initNumRootCells / 2;

        for (int i = -halfNumCells.x; i < halfNumCells.x; i++)
        {
            for (int j = -halfNumCells.y; j < halfNumCells.y; j++)
            {
                for (int k = -halfNumCells.z; k < halfNumCells.z; k++)
                {
                    //WFHashKey key = KeyAtDepth(new Vector3(i * rootCellSize, j * rootCellSize, k * rootCellSize), 0);
                    //cells.Add(key, new WindFieldCell()); //initialise empty root cell
                    WindFieldPoint point = new WindFieldPoint(new Vector3(i * rootCellSize, j * rootCellSize, k * rootCellSize), Vector3.forward, 0, 1, WindProducerMode.PositionStatic);
                    Add(point);
                }
            }
        }

        Debug.Log("Cells: " + cells.Count);
    }

    /*----PUBLIC FUNCTIONS----*/
    //Adds a given WindFieldPoint to the cell corresponding to its position and depth, creating that cell and its parent(s) if they don't exist.
    //TODO: getting a new key each time is very inefficient compared to getting the deepest key initially and removing from that. 
    public void Add(WindFieldPoint obj)
    {
        WFHashKey key = KeyAtDepth(obj.position, obj.depth);

        if (cells.ContainsKey(key)) //if cell already exists, add the object to that cell
        {
            cells[key].Add(obj);
        }
        else
        {
            //if obj is at root, no need to check for parents
            if(obj.depth == 0)
            {
                cells.Add(key, new WindFieldCell());
                cells[key].Add(obj);
            }
            else
            {
                //find deepest parent of object's cell
                uint depth = obj.depth - 1;
                while (!cells.ContainsKey(KeyAtDepth(obj.position, depth)) && depth > 0) depth--;

                //if no parent cell, create one at root depth
                if (depth == 0)
                {
                    cells.Add(KeyAtDepth(obj.position, 0), new WindFieldCell());
                    depth++;
                }

                //create cells from deepest parent's depth + 1 to object's depth, adding each parent's wind data to the child cell 
                //Debug.Log("Depth = " + depth + ", obj.depth = " + obj.depth);
                for (; depth <= obj.depth; depth++)
                {
                    cells.Add(KeyAtDepth(obj.position, depth), new WindFieldCell(cells[KeyAtDepth(obj.position, depth - 1)]));
                }

                //add object to newly-created cell
                cells[key].Add(obj);
            }

            
        }
    }

    //Adds given WindFieldPoints to their corresponding cells (creates new cell(s) if none exist at generated hash position(s))
    public void Add(WindFieldPoint[] objs)
    {
        foreach(WindFieldPoint obj in objs)
        {
            Add(obj);
        }
    }
    
    /*----GETTERS AND SETTERS----*/
    //Gets the wind vector at the given world position. If no cell exists at the given position, returns (0, 0, 0)
    public Vector3 GetWind(Vector3 pos)
    {
        WindFieldCell cell;
        return TryGetCell(pos, out cell) ? cell.GetWind() : Vector3.zero;   
    }

    public Dictionary<WFHashKey, WindFieldCell> GetCellDict()
    {
        return cells;
    }
    
    //Get the world position of the cell with the given hash key. Note that this returns the
    //leastmost corner of the cell, not its centre (so a cell with bounds from (0,0,0) to (1,1,1)
    //would return (0,0,0), not (0.5,0.5,0.5))
    public Vector3 GetCellWorldPosition(WFHashKey key)
    {
        Vector3Int[] k = key.GetKey();
        Vector3 worldPos = (Vector3)k[0] * rootCellSize;

        for(int i = 1; i < k.Length; i++)
        {
            worldPos += (Vector3)k[i] * (rootCellSize / (2 * i));
        }

        return worldPos;
    }

    //Get the world position of the centre of the cell with the given hash key.
    public Vector3 GetCellWorldPositionCentre(WFHashKey key)
    {
        Vector3Int[] k = key.GetKey();
        Vector3 worldPos = ((Vector3)k[0] * rootCellSize);

        for (int i = 1; i < k.Length; i++)
        {
            worldPos += (Vector3)k[i] * (rootCellSize / (2 * i));
        }

        //add half of deepest cell size in each dimension to get centre of deepest cell
        float halfFinalCellSize = k.Length == 1 ? rootCellSize / 2 : (rootCellSize / (2 * (k.Length - 1))) / 2;
        return worldPos + new Vector3(halfFinalCellSize, halfFinalCellSize, halfFinalCellSize);
    }


    /*----PRIVATE UTILITY FUNCTIONS----*/
    //Gets the wind field cell at the given position, or null if no such cell exists
    private bool TryGetCell(Vector3 pos, out WindFieldCell cell)
    {
        //if root cell exists at this key, cell will be set to it; if not, there is no cell at this position so return false
        if (cells.TryGetValue(KeyAtDepth(pos, 0), out cell))
        {
            uint depth = 1;
            while (cell.HasChild())
            {
                cell = cells[KeyAtDepth(pos, depth)]; //this may fail if using the method where a cell doesn't need to have all eight children
                
                //if using method where a cell may have children but not necessarily all eight, this is necessary
                /*
                WFHashKey key = KeyAtDepth(pos, depth);
                if(cells.ContainsKey(key))
                {
                    cell = cells[key];
                    depth++;
                }
                else
                {
                    return true;
                }
                */
            }

            return true;
        }

        return false;
    }
        
    //Generate the key at a given position and depth
    private WFHashKey KeyAtDepth(Vector3 pos, uint depth)
    {
        return new WFHashKey(pos, rootCellSize, depth);
    }
}
