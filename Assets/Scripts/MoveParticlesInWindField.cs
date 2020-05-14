using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class MoveParticlesInWindField : MonoBehaviour
{
    private ParticleSystem particleSystem;
    public WindField windField;
    public bool active = true;

    private void Start()
    {
        particleSystem = GetComponent<ParticleSystem>();
        if (windField == null) Debug.LogError("No wind field given for WindFieldProducer " + ToString() + "!");
    }

    private void Update()
    {
        if(active)
        {
            ParticleSystem.Particle[] particles = new ParticleSystem.Particle[particleSystem.main.maxParticles];
            particleSystem.GetParticles(particles);
            for (int i = 0; i < particles.Length; i++)
            {
                Vector3 pos = particles[i].position;
                particles[i].position = Vector3.MoveTowards(pos, pos + windField.GetWind(pos), 1f);
            }

            particleSystem.SetParticles(particles);
        }
        
    }
}
