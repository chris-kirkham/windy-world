using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//based on https://unionassets.com/blog/spatial-hashing-295
public class SpatialHash<T> : MonoBehaviour where T : HashCell, new()
{

    public Vector3 cellSize;
    public int initNumCellsX, initNumCellsY, initNumCellsZ;

    public float updateInterval; //interval in seconds between hash updates. 0 = update every frame
    private float updateTime; //update interval counter

    //false = add to cells by transform.position (faster, good for very small objects), true = add to cells by AABB (slower, more accurately represents large objects)
    //TODO: make this public when implemented
    private bool useAABB = false; 

    private Dictionary<Vector3Int, T> cells; //<key, bucket> dictionary representing each spatial cell's key and its contents
    
    //private Dictionary<Vector3Int, float> timeouts; //stores the timeout timers of each cell in the hash

    //objects to be put into cells by the spatial hash algorithm; each GameObject with a HashObject script (which points to the GameObject with this script) 
    //will add itself to includedObjs on Start() and remove itself on OnDestroy()
    //N.B. must initialise this in Awake() not Start() or there is no guarantee it will be initialised before any HashObjects try to add themselves to it
    private HashSet<GameObject> includedObjs; //https://stackoverflow.com/questions/150750/hashset-vs-list-performance

    void Awake()
    {
        updateTime = updateInterval;

        includedObjs = new HashSet<GameObject>();

        //initialise dictionary with initial capacity = number of initial cells
        cells = new Dictionary<Vector3Int, T>(initNumCellsX * initNumCellsY * initNumCellsZ);

        //add keys for initial cells and initialise corresponding empty buckets 
        //N.B. goes from (-initNumCellsX/Y/Z / 2) to (+initNumCellsX/Y/Z / 2) so the center of the hash is at the position of the attached GameObject
        int halfCellsX = initNumCellsX / 2;
        int halfCellsY = initNumCellsY / 2;
        int halfCellsZ = initNumCellsZ / 2;

        for (int i = -halfCellsX; i < halfCellsX; ++i)
        {
            for (int j = 0; j < initNumCellsY; ++j)
            {
                for (int k = -halfCellsZ; k < halfCellsZ; ++k)
                {
                    Vector3Int key = Key(new Vector3(i * cellSize.x, j * cellSize.y, k * cellSize.z));
                    cells.Add(key, new T()); //initialise empty cell
                }
            }
        }

    }

    void Start()
    {
        StartCoroutine(UpdateHash());
    }

    private IEnumerator UpdateHash()
    {
        //delete timed-out cells
        List<Vector3Int> cellsToRemove = new List<Vector3Int>();
        foreach (KeyValuePair<Vector3Int, T> item in cells)
        {
            item.Value.UpdateTimeout(Time.deltaTime);
            if (item.Value.IsTimedOut()) cellsToRemove.Add(item.Key);
        }

        foreach (Vector3Int key in cellsToRemove)
        {
            cells.Remove(key);
        }


        ClearBuckets();

        /*Add updated objects back into cells*/
        if (!useAABB)
        {
            foreach (GameObject obj in includedObjs)
            {
                AddByPoint(obj);
            }
        }
        else
        {
            foreach (GameObject obj in includedObjs)
            {
                //AddByAABB
            }
        }

        yield return new WaitForSeconds(updateInterval);
    }

    //inserts a GameObject into the correct bucket by its transform.position only (i.e. not taking into account its size, won't be added to more than one bucket);
    //this is faster than adding an object to the bucket(s) its AABB overlaps, but will of course cause inaccurate results for big objects.
    //Use only on very small objects (like boids)
    public void AddByPoint(GameObject obj)
    {
        Vector3Int key = Key(obj);

        if (cells.ContainsKey(key))
        {
            cells[key].Add(obj);
        }
        else
        {
            cells.Add(key, new T());
            cells[key].Add(obj);
        }
    }

    //adds multiple GameObjects to the hash by their transform.positions
    public void AddByPoint(List<GameObject> objs)
    {
        foreach(GameObject obj in objs)
        {
            AddByPoint(obj);
        }
    }

    //TODO
    //Inserts a GameObject into the bucket(s) which its AABB overlaps. Copies of objects whose AABB overlaps more than one cell will
    //be added to all overlapping cells. This allows big objects to be spatially represented more accurately, but means the dictionary may contain
    //duplicate objects - use GetByRadiusNoDups if you are doing spatial checking on a dictionary containing objects added by AABB and do not want duplicates
    /*
    public void AddByAABB(GameObject o)
    {

    }
    */

    //returns a list of all objects in buckets[key], or empty list if key isn't in the dictionary
    public List<GameObject> GetCellObjs(Vector3Int key)
    {
        return cells.ContainsKey(key) ? cells[key].GetObjs() : new List<GameObject>();
    }
    
    //returns a list of all objects in the cell which contains pos, or empty list if pos is not a coordinate in an existing cell 
    public List<GameObject> GetCellObjs(Vector3 pos)
    {
        Vector3Int key = Key(pos);
        return cells.ContainsKey(key) ? cells[key].GetObjs() : new List<GameObject>();
    }

    public List<KeyValuePair<Vector3Int, T>> GetCellDict()
    {
        return cells.ToList();
    }

    public List<T> GetCells()
    {
        return cells.Values.ToList();
    }

    public List<T> GetNonEmptyCells()
    {
        List<T> nonEmptyCells = new List<T>();

        foreach (KeyValuePair<Vector3Int, T> item in cells)
        {
            if (!item.Value.IsEmpty()) nonEmptyCells.Add(item.Value);
        }

        return nonEmptyCells;
    }

    public List<T> GetEmptyCells()
    {
        List<T> emptyCells = new List<T>();

        foreach (KeyValuePair<Vector3Int, T> item in cells)
        {
            if (item.Value.IsEmpty()) emptyCells.Add(item.Value);
        }

        return emptyCells;
    }


    //returns a list of cells in the spatial hash, i.e. returns the keys. This is NOT the cells' positions in space, just their spatial keys
    //e.g (0, 0, 0), (0, 0, 1), (1, 0, 0) etc.
    public List<Vector3Int> GetCellKeys()
    {
        return cells.Keys.ToList();
    }

    //returns a list of cells containing objects. This is NOT the cells' positions in space, just their spatial keys, e.g (0, 0, 0), (0, 0, 1), (1, 0, 0) etc.
    public List<Vector3Int> GetNonEmptyCellKeys()
    {
        List<Vector3Int> nonEmptyKeys = new List<Vector3Int>();

        foreach(KeyValuePair<Vector3Int, T> item in cells)
        {
            if (!item.Value.IsEmpty()) nonEmptyKeys.Add(item.Key);
        }

        return nonEmptyKeys;
    }

    //returns a list of cells not containing objects
    public List<Vector3Int> GetEmptyCellKeys()
    {
        List<Vector3Int> emptyKeys = new List<Vector3Int>();

        foreach (KeyValuePair<Vector3Int, T> item in cells)
        {
            if (item.Value.IsEmpty()) emptyKeys.Add(item.Key);
        }

        return emptyKeys;
    }

    //Returns the world position of cell with the given key
    public Vector3 GetCellWorldPosition(Vector3Int key)
    {
        return new Vector3(key.x * cellSize.x, key.y * cellSize.y, key.z * cellSize.z);
    }


    //returns a list of the world positions of cells in the spatial hash
    public List<Vector3> GetCellWorldPositions()
    {
        List<Vector3> cellWorldPositions = new List<Vector3>();

        foreach (KeyValuePair<Vector3Int, T> item in cells)
        {
            T cell = item.Value;
            Vector3Int key = item.Key;
            cellWorldPositions.Add(new Vector3(key.x * cell.sizeX, key.y * cell.sizeY, key.z * cell.sizeZ));
        }

        return cellWorldPositions;
    }

    //returns a list of the world positions of cells in the spatial hash containing objects
    public List<Vector3Int> GetNonEmptyCellWorldPositions()
    {
        List<Vector3Int> keys = new List<Vector3Int>();

        foreach (KeyValuePair<Vector3Int, T> item in cells)
        {
            if (!item.Value.IsEmpty()) keys.Add(item.Key);
        }

        return keys;
    }


    //Clears each bucket in the buckets dictionary. Does not delete the buckets themselves
    public void ClearBuckets()
    {
        foreach(KeyValuePair<Vector3Int, T> bucket in cells)
        {
            bucket.Value.Clear();
        }
    }


    //Adds an object to list of objects to hash
    public void Include(GameObject obj)
    {
        includedObjs.Add(obj);
    }

    //Removes an object from list of objects to hash and from its cell object list in the hash itself
    //N.B. objects with the HashObject script call this function when they are destroyed. It is necessary to 
    //remove the destroyed object from the hash immediately, even though it would be removed anyway on the next hash update,
    //because other scripts could try to get the now-destroyed object from the hash before it is updated
    public void Remove(GameObject obj)
    {
        includedObjs.Remove(obj);
        Vector3Int key = Key(obj.transform.position);
        if (cells.ContainsKey(key)) cells[key].GetObjs().Remove(obj);
    }


    //generate a key for the given GameObject (using its transform.position)
    private Vector3Int Key(GameObject obj)
    {
        return new Vector3Int(FastFloor(obj.transform.position.x / cellSize.x), 
                              FastFloor(obj.transform.position.y / cellSize.y),
                              FastFloor(obj.transform.position.z / cellSize.z));
    }

    //generate a key for the given Vector3
    private Vector3Int Key(Vector3 pos)
    {
        return new Vector3Int(FastFloor(pos.x / cellSize.x), FastFloor(pos.y / cellSize.y), FastFloor(pos.z / cellSize.z));
    }


    //from https://www.codeproject.com/Tips/700780/Fast-floor-ceiling-functions
    private int FastFloor(float f)
    {
        return (int)(f + 32768f) - 32768;
    }


    /*----DEBUG/VISUALISATION FUNCTIONS - PASS DEBUG DATA TO SpatialHashDebug.cs ----*/
    public int DEBUG_GetIncludedObjsCount()
    {
        return includedObjs.Count();
    }
    
}
