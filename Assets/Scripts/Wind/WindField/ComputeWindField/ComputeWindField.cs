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

        private RenderTexture windField; //3D texture; acts as a vector field to store wind information

        public Vector3Int numCells = Vector3Int.one;
        [Min(0)] public float cellSize = 1f;
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
            //create 3D render texture for wind field
            RenderTextureDescriptor desc = new RenderTextureDescriptor(numCells.x, numCells.y, RenderTextureFormat.ARGBFloat);
            desc.depthBufferBits = 0;
            desc.dimension = TextureDimension.Tex3D;
            desc.volumeDepth = numCells.z;
            desc.enableRandomWrite = true;

            windField = new RenderTexture(desc);
            windField.Create();
        }

        //Returns the 3D RenderTexture which acts as the wind field
        public RenderTexture GetWindFieldRenderTexture()
        {
            return windField;
        }

        public Vector3 GetWind(Vector3 position)
        {
            return getWindScript.GetWind(position);
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