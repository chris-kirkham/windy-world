using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class MoveParticlesInWindField : MonoBehaviour
{
    private ParticleSystem particles;
    public WindField windField;
    public bool active = true;

    private void Start()
    {
        particles = GetComponent<ParticleSystem>();
        if (windField == null) Debug.LogError("No wind field given for particles " + ToString() + "!");
    }

    private void Update()
    {
        //INEFFICIENT first-test method for moving particles in the wind
        if(active)
        {
            ParticleSystem.Particle[] particlesCopy = new ParticleSystem.Particle[particles.main.maxParticles];
            particles.GetParticles(particlesCopy);
            for (int i = 0; i < particlesCopy.Length; i++)
            {
                Vector3 pos = particlesCopy[i].position;
                particlesCopy[i].position = Vector3.MoveTowards(pos, pos + windField.GetWind(pos), 1f);
            }

            particles.SetParticles(particlesCopy);
        }
        
    }
}
