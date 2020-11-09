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
    [ExecuteAlways]
    public abstract class WindVis : MonoBehaviour
    {
        [SerializeField] protected Mesh windArrowMesh;
        [SerializeField] protected Material windArrowMaterial;

        //"Buffer with arguments, bufferWithArgs, has to have five integer numbers at given argsOffset offset: index count per instance,
        //instance count, start index location, base vertex location, start instance location." - Sun Tzu
        private ComputeBuffer argsBuffer;
        private uint[] args;

        private ComputeBuffer windPoints;

        private Bounds bounds;

        void OnEnable()
        {
            argsBuffer = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);
            args = new uint[5] { 0, 0, 0, 0, 0 };

            bounds = new Bounds(transform.position, Vector3.one * 100000f);
        }

        protected void DrawWindPoints(ComputeBuffer windPoints)
        {
            windArrowMaterial.SetBuffer("points", windPoints);

            //args
            uint numIndices = windArrowMesh != null ? windArrowMesh.GetIndexCount(0) : 0;
            args[0] = numIndices;
            args[1] = (uint)windPoints.count;
            argsBuffer.SetData(args);

            Graphics.DrawMeshInstancedIndirect(windArrowMesh, 0, windArrowMaterial, bounds, argsBuffer);
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