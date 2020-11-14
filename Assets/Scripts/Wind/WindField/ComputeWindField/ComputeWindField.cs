﻿using System.Collections.Generic;
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
        private RenderTexture windFieldNoise; //stores generated wind noise vectors; updated every tick
        private RenderTextureDescriptor windFieldRTDesc; //descriptor for the wind field render textures; used to initialise all wind 
        public Texture3D noiseTex;

        //wind producer lists
        private List<WindProducer> staticWindProducers = new List<WindProducer>();
        private List<WindProducer> dynamicWindProducers = new List<WindProducer>();

        //general wind field parameters
        [SerializeField] private Vector3Int numCells = Vector3Int.one;
        [SerializeField] [Min(0)] private float cellSize = 1f;
        [SerializeField] private Vector3 globalWind = Vector3.zero;
        public Vector3 LeastCorner { get; private set; } //least corner of wind field

        public ComputeShader DEBUG_WindFieldTestsCompute;
        private int DEBUG_TestAssignToRenderTexturesKernel;
        private int DEBUG_TestSpatialHashingKernel;

        private void Start()
        {
            InitWindField();

            DEBUG_TestAssignToRenderTexturesKernel = DEBUG_WindFieldTestsCompute.FindKernel("DEBUG_TestAssignToRenderTextures");
            DEBUG_TestSpatialHashingKernel = DEBUG_WindFieldTestsCompute.FindKernel("DEBUG_TestSpatialHashing");
        }

        private void InitWindField()
        {
            Debug.Log("Initialising wind field..."); 

            //get other scripts
            addPointsScript = GetComponent<AddPointsToWindField>();
            getWindScript = GetComponent<GetWindAtPosition>();

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
            windFieldNoise = new RenderTexture(windFieldRTDesc);
            windFieldNoise.Create();

            //add static wind producers to static rt
            foreach (WindProducer wp in staticWindProducers)
            {
                windFieldStatic = addPointsScript.AddPoints(windFieldStatic, LeastCorner, cellSize, wp.GetWindFieldPointsBuffer());
            }

            //add dynamic wind producers to dynamic rt
            UpdateDynamicWindField();

            UpdateLeastCorner();

            Debug.Log("Static wind producers size: " + staticWindProducers.Count);
            Debug.Log("Dynamic wind producers size: " + dynamicWindProducers.Count);
            Debug.Log("Wind field least corner = " + LeastCorner);
        }

        private void Update()
        {
            //DEBUG_TestAssignToWindFieldRenderTextures();
            //DEBUG_TestSpatialHashing();
            UpdateDynamicWindField();
        }

        //Clears the dynamic wind field render texture and (re-)adds the wind producers in the dynamic wind producers list
        private void UpdateDynamicWindField()
        {
            //if (windFieldDynamic != null) windFieldDynamic.Release();
            //windFieldDynamic = new RenderTexture(windFieldRTDesc);

            foreach(WindProducer wp in dynamicWindProducers)
            {
                windFieldDynamic = addPointsScript.AddPoints(windFieldDynamic, LeastCorner, cellSize, wp.GetWindFieldPointsBuffer());
            }
        }

        //adds a wind producer to the corresponding (static or dynamic) wind field.
        public void Include(WindProducer windProducer)
        {
            switch(windProducer.mode)
            {
                case WindProducerMode.Static:
                    staticWindProducers.Add(windProducer);
                    break;
                case WindProducerMode.Dynamic:
                    dynamicWindProducers.Add(windProducer);
                    break;
                default:
                    Debug.LogError("Trying to add a wind producer with an unhandled wind producer mode to this wind field." +
                        "Did you add another mode and forget to update this function???");
                    break;
            }
        }

        public RenderTexture GetStaticWindField()
        {
            return windFieldStatic;
        }

        public RenderTexture GetDynamicWindField()
        {
            return windFieldDynamic;
        }

        public RenderTexture GetNoiseWindField()
        {
            return windFieldNoise;
        }

        public Vector3 GetWind(Vector3 position)
        {
            //return globalWind;
            return getWindScript.GetWind(position);
        }

        public Vector3 GetGlobalWind()
        {
            return globalWind;
        }

        public float GetCellSize()
        {
            return cellSize;
        }

        public Vector3Int GetNumCells()
        {
            return numCells;
        }

        private void UpdateLeastCorner()
        {
            LeastCorner = transform.position 
                + (Vector3.left * (numCells.x / 2f) * cellSize) 
                + (Vector3.down * (numCells.y / 2f) * cellSize) 
                + (Vector3.back * (numCells.z / 2f) * cellSize);
        }

        //Function to visually debug whether the wind field's render textures are being assigned to properly.
        private void DEBUG_TestAssignToWindFieldRenderTextures()
        {
            DEBUG_WindFieldTestsCompute.SetTexture(DEBUG_TestAssignToRenderTexturesKernel, "windFieldStatic", windFieldStatic);
            DEBUG_WindFieldTestsCompute.SetTexture(DEBUG_TestAssignToRenderTexturesKernel, "windFieldDynamic", windFieldDynamic);
            DEBUG_WindFieldTestsCompute.SetTexture(DEBUG_TestAssignToRenderTexturesKernel, "windFieldNoise", windFieldNoise);
            DEBUG_WindFieldTestsCompute.SetFloat("cellSize", cellSize);
            DEBUG_WindFieldTestsCompute.SetFloats("windFieldStartPos", new float[3] { LeastCorner.x, LeastCorner.y, LeastCorner.z });

            uint[] groupSize = new uint[3];
            DEBUG_WindFieldTestsCompute.GetKernelThreadGroupSizes(DEBUG_TestAssignToRenderTexturesKernel, out groupSize[0], out groupSize[1], out groupSize[2]);
            int[] numGroups = new int[3];
            numGroups[0] = Mathf.Max(1, Mathf.CeilToInt((float)numCells.x / groupSize[0]));
            numGroups[1] = Mathf.Max(1, Mathf.CeilToInt((float)numCells.y / groupSize[1]));
            numGroups[2] = Mathf.Max(1, Mathf.CeilToInt((float)numCells.z / groupSize[2]));
            Debug.Log("numGroups = (" + numGroups[0] + ", " + numGroups[1] + ", " + numGroups[2] + ")");

            DEBUG_WindFieldTestsCompute.Dispatch(DEBUG_TestAssignToRenderTexturesKernel, numGroups[0], numGroups[1], numGroups[2]);
        }

        //Function to visually debug whether wind field is being hashed to properly
        private void DEBUG_TestSpatialHashing()
        {
            DEBUG_WindFieldTestsCompute.SetTexture(DEBUG_TestSpatialHashingKernel, "windFieldStatic", windFieldStatic);
            DEBUG_WindFieldTestsCompute.SetTexture(DEBUG_TestSpatialHashingKernel, "windFieldDynamic", windFieldDynamic);
            DEBUG_WindFieldTestsCompute.SetTexture(DEBUG_TestSpatialHashingKernel, "windFieldNoise", windFieldNoise);
            DEBUG_WindFieldTestsCompute.SetFloat("cellSize", cellSize);
            DEBUG_WindFieldTestsCompute.SetFloats("windFieldStartPos", new float[3] { LeastCorner.x, LeastCorner.y, LeastCorner.z });

            uint[] groupSize = new uint[3];
            DEBUG_WindFieldTestsCompute.GetKernelThreadGroupSizes(DEBUG_TestSpatialHashingKernel, out groupSize[0], out groupSize[1], out groupSize[2]);
            int[] numGroups = new int[3];
            numGroups[0] = Mathf.Max(1, Mathf.CeilToInt((float)numCells.x / groupSize[0]));
            numGroups[1] = Mathf.Max(1, Mathf.CeilToInt((float)numCells.y / groupSize[1]));
            numGroups[2] = Mathf.Max(1, Mathf.CeilToInt((float)numCells.z / groupSize[2]));
            Debug.Log("numGroups = (" + numGroups[0] + ", " + numGroups[1] + ", " + numGroups[2] + ")");

            DEBUG_WindFieldTestsCompute.Dispatch(DEBUG_TestSpatialHashingKernel, numGroups[0], numGroups[1], numGroups[2]);
        }

    }
}