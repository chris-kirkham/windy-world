using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// INEFFICIENT first-test method for moving particles in the wind
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class MoveParticlesInWindField : MonoBehaviour
{
    public WindField windField;
    public bool active = true;
    //public int batchSize = 0;
    public float mass = 1f;
    
    private ParticleSystem particles;
    private float updateInterval = 0f;

    private void Start()
    {
        particles = GetComponent<ParticleSystem>();
        if (windField == null) Debug.LogError("No wind field given for particles " + ToString() + "!");
        StartCoroutine(UpdateParticles());
    }

    private IEnumerator UpdateParticles()
    {
        //int i = 0;
        while(active)
        {
            ParticleSystem.Particle[] particlesCopy = new ParticleSystem.Particle[particles.main.maxParticles];
            particles.GetParticles(particlesCopy);

            for(int i = 0; i < particlesCopy.Length; i++)
            {
                Vector3 pos = particlesCopy[i].position;
                particlesCopy[i].velocity += windField.GetWind(pos) / (mass + 1);
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

            yield return new WaitForSecondsRealtime(updateInterval);
        }

        yield return new WaitForSecondsRealtime(updateInterval);
    }
}
