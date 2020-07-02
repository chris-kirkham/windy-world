using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Wind;
using Unity.Collections;

/// <summary>
/// INEFFICIENT first-test method for moving particles in the wind
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class MoveParticlesInWindField : MonoBehaviour
{
    [SerializeField] private WindField windField;
    [SerializeField] private bool active = true;
    [SerializeField] private int batchSize = 100;
    [SerializeField] private float mass = 1f;
    
    private ParticleSystem particles;
    private int maxParticles;
    ParticleSystem.Particle[] particlesCopy;
    
    private float updateInterval = 0f;

    /* Particle movement job */
    JobHandle moveParticlesHandle;
    NativeArray<float3> particlePositions;
    NativeArray<float3> particleVelocities;
    NativeArray<float3> windVelocities;

    private void Start()
    {
        particles = GetComponent<ParticleSystem>();
        maxParticles = particles.main.maxParticles;
        particlesCopy = new ParticleSystem.Particle[maxParticles];
        particlePositions = new NativeArray<float3>(maxParticles, Allocator.Persistent);
        particleVelocities = new NativeArray<float3>(maxParticles, Allocator.Persistent);
        windVelocities = new NativeArray<float3>(maxParticles, Allocator.Persistent);
        if (windField == null) Debug.LogError("No wind field given for particles " + ToString() + "!");
    }

    private void Update()
    {
        //int i = 0;
        if(active)
        {
            /*
            particlesCopy = new ParticleSystem.Particle[maxParticles];
            particlePositions = new NativeArray<float3>(maxParticles, Allocator.TempJob);
            particleVelocities = new NativeArray<float3>(maxParticles, Allocator.TempJob);
            windVelocities = new NativeArray<float3>(maxParticles, Allocator.TempJob);

            particles.GetParticles(particlesCopy);

            for(int i = 0; i < particlesCopy.Length; i++)
            {
                ParticleSystem.Particle p = particlesCopy[i];
                particlePositions[i] = p.position;
                particleVelocities[i] = p.velocity;
                windVelocities[i] = windField.GetWind(p.position);
            }

            MoveParticles moveParticles = new MoveParticles
            {
                mass = this.mass,
                particlePositions = particlePositions,
                particleVels = particleVelocities,
                windVels = windVelocities,
                deltaTime = Time.deltaTime
            };

            moveParticlesHandle = moveParticles.Schedule(maxParticles, batchSize);
            */

            particles.GetParticles(particlesCopy);

            for(int i = 0; i < particlesCopy.Length; i++)
            {
                Vector3 pos = particlesCopy[i].position;
                particlesCopy[i].velocity = Vector3.Lerp(particlesCopy[i].velocity, windField.GetWind(pos) / (mass + 0.01f), Time.deltaTime);
            }
            particles.SetParticles(particlesCopy);

            /*
            //if batchSize > 0, update in batches of batchSize, else update all particles
            int last = batchSize > 0 ? Mathf.Min(particlesCopy.Length, i + batchSize) : particlesCopy.Length;
            for (; i < last; i++)
            {
                Vector3 pos = particlesCopy[i].position;
                particlesCopy[i].velocity += windField.GetWind(pos) / (mass + 0.0001f);
            }
            
            if (i == particlesCopy.Length) i = 0;
            particles.SetParticles(,, i - 1);
            */

        }
    }

    /*
    private void LateUpdate()
    {
        moveParticlesHandle.Complete();
        
        //set new particle positions
        for(int i = 0; i < maxParticles; i++)
        {
            particlesCopy[i].velocity = particleVelocities[i];
        }
        particles.SetParticles(particlesCopy);

        particlePositions.Dispose();
        particleVelocities.Dispose();
        windVelocities.Dispose();
    }
    */

    private void OnDestroy()
    {
        particlePositions.Dispose();
        particleVelocities.Dispose();
    }

    private struct MoveParticles : IJobParallelFor
    {
        public float mass;
        public NativeArray<float3> particlePositions;
        public NativeArray<float3> particleVels;
        public NativeArray<float3> windVels;
        public float deltaTime;

        public void Execute(int i)
        {
            particleVels[i] = Vector3.Lerp(particleVels[i], windVels[i] / (mass + 0.01f), deltaTime);
        }
    }
}
