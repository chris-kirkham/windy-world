using System.Collections;
using System.Collections.Generic;
using Unity.Entities.UniversalDelegates;
using UnityEngine;
using UnityEngine.Rendering;

namespace Wind
{
    /// <summary>
    /// The main wind field script. Initialises the wind field on startup and provides an interface to actions on the wind field
    /// e.g. adding wind points to it, getting the wind at a particular position, getting the wind field RenderTexture
    /// </summary>
    //wind vector field
    [RequireComponent(typeof(RenderTexture))]
    //compute scripts
    [RequireComponent(typeof(AddPointsToWindField))]
    [RequireComponent(typeof(GetWindAtPosition))]
    public class ComputeWindField : MonoBehaviour
    {
        //scripts for compute shaders that do wind field functions
        private AddPointsToWindField addPointsScript;
        private GetWindAtPosition getWindScript;

        //3D render textures which act as vector fields to store wind information
        private RenderTexture windFieldStatic; //stores wind vectors from static wind producers; updated only on game start  
        private RenderTexture windFieldDynamic; //stores wind vectors from dynamic wind producers; updated from dynamic producer list every tick
        private RenderTextureDescriptor windFieldRTDesc; //descriptor for the wind field render textures; used to initialise all wind 

        //wind producer lists
        private List<WindProducer> staticWindProducers;
        private List<WindProducer> dynamicWindProducers;

        //general wind field parameters
        [SerializeField] private Vector3Int numCells = Vector3Int.one;
        [SerializeField] [Min(0)] private float cellSize = 1f;
        [SerializeField] private Vector3 globalWind = Vector3.zero;
        public Vector3 WindFieldLeastCorner { get; private set; } //least corner of wind field

        private void Start()
        {
            InitWindField();

            //get other scripts
            addPointsScript = GetComponent<AddPointsToWindField>();
            getWindScript = GetComponent<GetWindAtPosition>();

            UpdateLeastCorner();
        }

        private void OnValidate()
        {
            UpdateLeastCorner();
        }

        private void InitWindField()
        {
            //set wind field RT description
            windFieldRTDesc = new RenderTextureDescriptor(numCells.x, numCells.y, RenderTextureFormat.ARGBFloat);
            windFieldRTDesc.depthBufferBits = 0;
            windFieldRTDesc.dimension = TextureDimension.Tex3D;
            windFieldRTDesc.volumeDepth = numCells.z;
            windFieldRTDesc.enableRandomWrite = true;

            //create 3D render textures for wind field
            windFieldStatic = new RenderTexture(windFieldRTDesc);
            windFieldStatic.Create();
            windFieldDynamic = new RenderTexture(windFieldRTDesc);
            windFieldDynamic.Create();

            //add static wind producers to static rt
            foreach(WindProducer wp in staticWindProducers)
            {
                addPointsScript.AddPoints(windFieldStatic, WindFieldLeastCorner, cellSize, wp.GetWindFieldPointsBuffer());
            }

            //add dynamic wind producers to dynamic rt
            UpdateDynamicWindField();
        }

        //Clears the dynamic wind field render texture and (re-)adds the wind producers in the dynamic wind producers list
        private void UpdateDynamicWindField()
        {
            if (windFieldDynamic != null) windFieldDynamic.Release();
            windFieldDynamic = new RenderTexture(windFieldRTDesc);

            foreach(WindProducer wp in dynamicWindProducers)
            {
                addPointsScript.AddPoints(windFieldDynamic, WindFieldLeastCorner, cellSize, wp.GetWindFieldPointsBuffer());
            }
        }

        //Returns the 3D RenderTexture which acts as the wind field
        public RenderTexture GetWindFieldRenderTexture()
        {
            return windFieldStatic;
        }

        public Vector3 GetWind(Vector3 position)
        {
            return globalWind;
            //return getWindScript.GetWind(position);
        }

        public Vector3 GetGlobalWind()
        {
            return globalWind;
        }

        public float GetCellSize()
        {
            return cellSize;
        }

        private void UpdateLeastCorner()
        {
            WindFieldLeastCorner = transform.position 
                + (Vector3.left * ((float)numCells.x / 2f)) 
                + (Vector3.down * ((float)numCells.y / 2f)) 
                + (Vector3.back * ((float)numCells.z / 2f));
        }

    }
}