using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wind
{
    /// <summary>
    /// Renders wind field/wind producer visualiser arrows using Graphics.DrawMeshInstancedIndirect
    /// </summary>
    /// REFERENCES:
    /// https://docs.unity3d.com/ScriptReference/Graphics.DrawMeshInstancedIndirect.html
    /// https://medium.com/@bagoum/devlog-002-graphics-drawmeshinstancedindirect-a4024e05737f
    /// https://forum.unity.com/threads/drawmeshinstancedindirect-example-comments-and-questions.446080/
    /// https://github.com/tiiago11/Unity-InstancedIndirectExamples/tree/master/Demos-DrawMeshInstancedIndirect/Assets/InstancedIndirectCompute
    public abstract class WindVis : MonoBehaviour
    {
        [SerializeField] protected Mesh windArrowMesh;
        [SerializeField] protected Material windArrowMaterial;

        //material exclusive to this instance of the WindVis. Copies its parameters from windArrowMaterial on enable.
        //this is used so each object with a WindVis script attached has its own material and so doesn't overwrite the "master" windArrowMaterial's points buffer in DrawWindPoints.
        //
        //If we used .SetBuffer on windArrowMaterial, Unity would end up only drawing the arrows of the last object to call DrawWindPoints, since each call would overwrite the buffer. 
        //Apparently this issue can also be fixed using MaterialPropertyBlock, but this seems simpler (probably has some nasty side effects though)
        protected Material thisObjMaterial; 

        //"Buffer with arguments, bufferWithArgs, has to have five integer numbers at given argsOffset offset: index count per instance,
        //instance count, start index location, base vertex location, start instance location." - Sun Tzu
        private ComputeBuffer argsBuffer;
        private uint[] args;

        private ComputeBuffer windPoints;

        private Bounds bounds;

        void OnEnable()
        {
            thisObjMaterial = new Material(windArrowMaterial);

            argsBuffer = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);
            args = new uint[5] { 0, 0, 0, 0, 0 };

            bounds = new Bounds(transform.position, Vector3.one * 100000f);
        }

        protected virtual void DrawWindPoints(ComputeBuffer windPoints)
        {
            if (windPoints == null)
            {
                Debug.LogError("Null wind points buffer passed to WindVis::DrawWindPoints! Returning...");
                return;
            }

            //using thisObjMaterial rather than windArrowMaterial so we don't overwrite other instances of this scripts' buffers. See big comment on variable declaration for more
            thisObjMaterial.SetBuffer("points", windPoints);
            
            //args
            argsBuffer.Release();
            argsBuffer = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);
            args = new uint[5] { 0, 0, 0, 0, 0 };
            uint numIndices = windArrowMesh != null ? windArrowMesh.GetIndexCount(0) : 0;
            args[0] = numIndices;
            args[1] = (uint)windPoints.count;
            argsBuffer.SetData(args);

            Graphics.DrawMeshInstancedIndirect(windArrowMesh, 0, thisObjMaterial, bounds, argsBuffer);
        }

        private void OnDisable()
        {
            if (argsBuffer != null) argsBuffer.Release();
            if (windPoints != null) windPoints.Release();
        }

        public void SetWindPointsBuffer(ComputeBuffer windPoints)
        {
            this.windPoints = windPoints;
        }
    }
}