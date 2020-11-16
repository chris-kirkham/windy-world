using System.Collections.Generic;
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

        //compute shader to zero out wind field rt
        private ComputeShader resetWindFieldRT;
        private int resetWindFieldRTKernel;
        private uint[] resetWindFieldKernelGroupSizes;
        
        //wind producer lists
        private List<WindProducer> staticWindProducers = new List<WindProducer>();
        private List<WindProducer> dynamicWindProducers = new List<WindProducer>();

        //general wind field parameters
        [SerializeField] private Vector3Int numCells = Vector3Int.one;
        [SerializeField] [Min(0)] private float cellSize = 1f;
        [SerializeField] private Vector3 globalWind = Vector3.zero;
        public Vector3 LeastCorner { get; private set; } //least corner of wind field

        //wind field noise parameters
        [Tooltip("Strength of wind noise on each axis. Set to zero for no noise.")] 
        [SerializeField] private Vector3 noiseStrength = Vector3.one;

        [Tooltip("Scale of the noise on each axis.")]
        [SerializeField] private Vector3 noiseScale = Vector3.one;

        [Tooltip("Speed at which noise texture scrolls on each axis. Set to zero for static noise.")]
        [SerializeField] private Vector3 noiseTimeScale = Vector3.one;

        //noise updater compute
        private ComputeShader updateNoiseRT;
        private int updateNoiseRTKernel;
        private uint[] updateNoiseRTGroupSizes;

        //DEBUG
        private ComputeShader DEBUG_WindFieldTestsCompute;
        private int DEBUG_TestAssignToRenderTexturesKernel;
        private int DEBUG_TestSpatialHashingKernel;

        private void Start()
        {
            //reset wind field compute
            resetWindFieldRT = Resources.Load<ComputeShader>("Utilities/ResetTexture");
            resetWindFieldRTKernel = resetWindFieldRT.FindKernel("ResetTexture");
            resetWindFieldKernelGroupSizes = new uint[3];
            resetWindFieldRT.GetKernelThreadGroupSizes(resetWindFieldRTKernel, out resetWindFieldKernelGroupSizes[0], out resetWindFieldKernelGroupSizes[1], out resetWindFieldKernelGroupSizes[2]);

            //update noise wind field compute
            updateNoiseRT = Resources.Load<ComputeShader>("UpdateNoiseRenderTexture");
            Debug.Log("updateNoiseRT = " + updateNoiseRT);
            updateNoiseRTKernel = updateNoiseRT.FindKernel("UpdateNoiseRenderTexture");
            updateNoiseRTGroupSizes = new uint[3];
            updateNoiseRT.GetKernelThreadGroupSizes(updateNoiseRTKernel, out updateNoiseRTGroupSizes[0], out updateNoiseRTGroupSizes[1], out updateNoiseRTGroupSizes[2]);

            InitWindField();

            DEBUG_WindFieldTestsCompute = Resources.Load<ComputeShader>("DEBUG_TestAssignToWindFieldRenderTextures");
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

            //have to do this before adding anything to the RTs or they'll have the wrong least corner position
            UpdateLeastCorner();

            //add static wind producers to static rt
            foreach (WindProducer wp in staticWindProducers)
            {
                Debug.Log("number of wind points: " + wp.GetWindFieldPointsBuffer().count);
                windFieldStatic = addPointsScript.AddPoints(windFieldStatic, LeastCorner, cellSize, wp.GetWindFieldPointsBuffer());
            }

            //add dynamic wind producers to dynamic rt
            UpdateDynamicWindField();

            //initialise noise wind field rt
            UpdateNoiseWindField();

            Debug.Log("Static wind producers size: " + staticWindProducers.Count);
            Debug.Log("Dynamic wind producers size: " + dynamicWindProducers.Count);
            Debug.Log("Wind field least corner = " + LeastCorner);
        }

        private void Update()
        {
            UpdateDynamicWindField();
            UpdateNoiseWindField();
        }

        //Clears the dynamic wind field render texture and (re-)adds the wind producers in the dynamic wind producers list
        private void UpdateDynamicWindField()
        {
            if(dynamicWindProducers.Count > 0)
            {
                ResetWindFieldRT(windFieldDynamic);
                foreach (WindProducer wp in dynamicWindProducers)
                {
                    windFieldDynamic = addPointsScript.AddPoints(windFieldDynamic, LeastCorner, cellSize, wp.GetWindFieldPointsBuffer());
                }
            }
        }

        private void UpdateNoiseWindField()
        {
            updateNoiseRT.SetTexture(updateNoiseRTKernel, "noiseWindField", windFieldNoise);
            updateNoiseRT.SetTexture(updateNoiseRTKernel, "noiseTex", noiseTex);
            updateNoiseRT.SetFloats("noiseStrength", noiseStrength.ToArray());
            updateNoiseRT.SetFloats("noiseScale", noiseScale.ToArray());
            updateNoiseRT.SetFloats("noiseTimeScale", noiseTimeScale.ToArray());

            int[] numGroups = new int[3];
            numGroups[0] = Mathf.Max(1, Mathf.CeilToInt((float)numCells.x / resetWindFieldKernelGroupSizes[0]));
            numGroups[1] = Mathf.Max(1, Mathf.CeilToInt((float)numCells.y / resetWindFieldKernelGroupSizes[1]));
            numGroups[2] = Mathf.Max(1, Mathf.CeilToInt((float)numCells.z / resetWindFieldKernelGroupSizes[2]));

            updateNoiseRT.Dispatch(updateNoiseRTKernel, numGroups[0], numGroups[1], numGroups[2]);
        }

        private void ResetWindFieldRT(RenderTexture windFieldRT)
        {
            resetWindFieldRT.SetTexture(resetWindFieldRTKernel, "Result", windFieldRT);

            int[] numGroups = new int[3];
            numGroups[0] = Mathf.Max(1, Mathf.CeilToInt((float)numCells.x / resetWindFieldKernelGroupSizes[0]));
            numGroups[1] = Mathf.Max(1, Mathf.CeilToInt((float)numCells.y / resetWindFieldKernelGroupSizes[1]));
            numGroups[2] = Mathf.Max(1, Mathf.CeilToInt((float)numCells.z / resetWindFieldKernelGroupSizes[2]));

            resetWindFieldRT.Dispatch(resetWindFieldRTKernel, numGroups[0], numGroups[1], numGroups[2]);
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