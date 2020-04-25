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

    private Dictionary<Vector3Int, WindFieldCell> cells;
    private const int initCapacity = 1000;

    private void Awake()
    {
        //create initial cells
        cells = new Dictionary<Vector3Int, WindFieldCell>(initCapacity);
        Vector3Int halfNumCells = initNumCells / 2;
        for (int i = -halfNumCells.x; i < halfNumCells.x; i++)
        {
            for (int j = -halfNumCells.y; j < halfNumCells.y; j++)
            {
                for (int k = -halfNumCells.z; k < halfNumCells.z; k++)
                {
                    Vector3Int key = Key(new Vector3(i * cellSize, j * cellSize, k * cellSize));
                    cells.Add(key, new WindFieldCell()); //initialise empty cell
                }
            }
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

    public List<KeyValuePair<Vector3Int, WindFieldCell>> GetCellDict()
    {
        return cells.ToList();
    }

    public Vector3 GetCellWorldPosition(Vector3Int key)
    {
        return new Vector3(key.x * cellSize, key.y * cellSize, key.z * cellSize);
    }


    /*----PRIVATE UTILITY FUNCTIONS----*/

    //generate a key for the given Vector3
    private Vector3Int Key(Vector3 pos)
    {
        return new Vector3Int(FastFloor(pos.x / cellSize), FastFloor(pos.y / cellSize), FastFloor(pos.z / cellSize));
    }


    //from https://www.codeproject.com/Tips/700780/Fast-floor-ceiling-functions
    private int FastFloor(float f)
    {
        return (int)(f + 32768f) - 32768;
    }

}
