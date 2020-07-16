using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Wind;
using Unity.Collections;
using UnityEngine.ParticleSystemJobs;
using Unity.Burst;

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
    [SerializeField] private bool useJobSystem = false;
    
    private ParticleSystem particles;
    private MoveParticlesJob moveParticlesJob = new MoveParticlesJob();

    ParticleSystem.Particle[] particlesCopy;
    private int maxParticles;

    private float updateInterval = 0f;


    private void Start()
    {
        particles = GetComponent<ParticleSystem>();
        maxParticles = particles.main.maxParticles;
        particlesCopy = new ParticleSystem.Particle[maxParticles];

        if (windField == null) Debug.LogError("No wind field given for particles " + ToString() + "!");
    }

    private void Update()
    {
        //int i = 0;
        if(active)
        {
            if(useJobSystem)
            {
                //moveParticlesJob.nativeWindFieldCells = windField.GetBlittableCellsWind();
                moveParticlesJob.globalWind = windField.globalWind;
                moveParticlesJob.windFieldCellSize = windField.GetCellSize();
                moveParticlesJob.mass = mass;
                moveParticlesJob.lerpAmount = Time.deltaTime;
            }
            else
            {
                System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

                particles.GetParticles(particlesCopy);

                for (int i = 0; i < particlesCopy.Length; i++)
                {
                    Vector3 pos = particlesCopy[i].position;
                    particlesCopy[i].velocity = Vector3.Lerp(particlesCopy[i].velocity, windField.GetWind(pos) / (mass + 0.01f), Time.deltaTime);
                }
                particles.SetParticles(particlesCopy);

                stopwatch.Stop();
                Debug.Log("Non-jobified particle update time: " + stopwatch.ElapsedMilliseconds + " ms.");


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
    }
    
    void OnParticleUpdateJobScheduled()
    {
        if (useJobSystem)
        {
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            moveParticlesJob.Schedule(particles);
            stopwatch.Stop();
            Debug.Log("Jobified particle update time: " + stopwatch.ElapsedMilliseconds + " ms.");
        }
    }

    private void LateUpdate()
    {
        if(useJobSystem)
        {
           //moveParticlesHandle.Complete();
        }
    }

    private void OnDestroy()
    {
    
    }

    [BurstCompile]
    private struct MoveParticlesJob : IJobParticleSystem
    {
        //public NativeHashMap<SpatialHashKeyBlittable, Vector3> nativeWindFieldCells;
        public Vector3 globalWind;
        public float windFieldCellSize;
        public float mass;
        public float lerpAmount;

        public void Execute(ParticleSystemJobData particles)
        {
            NativeArray<float> positionsX = particles.positions.x;
            NativeArray<float> positionsY = particles.positions.y;
            NativeArray<float> positionsZ = particles.positions.z;

            NativeArray<float> velocitiesX = particles.velocities.x;
            NativeArray<float> velocitiesY = particles.velocities.y;
            NativeArray<float> velocitiesZ = particles.velocities.z;

            for(int i = 0; i < particles.count; i++)
            {
                SpatialHashKeyBlittable key = new SpatialHashKeyBlittable(new Vector3(positionsX[i], positionsY[i], positionsZ[i]), windFieldCellSize);

                Vector3 wind = globalWind;
                //nativeWindFieldCells.TryGetValue(key, out wind);
                wind /= (mass + 0.01f);

                velocitiesX[i] = wind.x;
                velocitiesY[i] = wind.y;
                velocitiesZ[i] = wind.z;

                /*
                velocitiesX[i] = Mathf.Lerp(velocitiesX[i], wind.x, lerpAmount);
                velocitiesY[i] = Mathf.Lerp(velocitiesY[i], wind.y, lerpAmount);
                velocitiesZ[i] = Mathf.Lerp(velocitiesZ[i], wind.z, lerpAmount);
                */
            }
        }
    }
}
