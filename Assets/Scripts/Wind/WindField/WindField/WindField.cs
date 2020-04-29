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
    public float cellSize = 1;
    public Vector3Int initNumCells;

    private Dictionary<WFHashKey, WindFieldCell> cells;

    private void Awake()
    {
        //create initial cells
        cells = new Dictionary<WFHashKey, WindFieldCell>(initNumCells.x * initNumCells.y * initNumCells.z);
        Vector3Int halfNumCells = initNumCells / 2;
        for (int i = -halfNumCells.x; i < halfNumCells.x; i++)
        {
            for (int j = -halfNumCells.y; j < halfNumCells.y; j++)
            {
                for (int k = -halfNumCells.z; k < halfNumCells.z; k++)
                {
                    WFHashKey key = Key(new Vector3(i * cellSize, j * cellSize, k * cellSize));
                    cells.Add(key, new WindFieldCell()); //initialise empty cell
                }
            }
        }
    }

    /*----PUBLIC FUNCTIONS----*/
    
    //Adds a given WindFieldPoint to its corresponding cell (creates new cell if one doesn't exist at generated hash position)
    public void Add(WindFieldPoint obj)
    {
        WFHashKey key = Key(obj.pos);

        if (cells.ContainsKey(key))
        {
            cells[key].Add(obj);
        }
        else
        {
            cells.Add(key, new WindFieldCell());
            cells[key].Add(obj);
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
    //Gets the wind field cell at the given world position
    public WindFieldCell GetCell(Vector3 pos)
    {
        return cells[Key(pos)];
    }

    //Gets the gameplay wind vector at the given world position
    public Vector3 GetGameplayWind(Vector3 pos)
    {
        return GetCell(pos).GetWind();
    }

    public List<KeyValuePair<WFHashKey, WindFieldCell>> GetCellDict()
    {
        return cells.ToList();
    }

    public Vector3 GetCellWorldPosition(WFHashKey key)
    {
        return new Vector3(key.x * cellSize, key.y * cellSize, key.z * cellSize);
    }

    /*----PRIVATE UTILITY FUNCTIONS----*/
    //generate a key for the given Vector3
    private WFHashKey Key(Vector3 pos)
    {
        return new WFHashKey(pos, cellSize);
    }

}
