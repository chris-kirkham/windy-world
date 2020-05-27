using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class MoveParticlesInWindField : MonoBehaviour
{
    public WindField windField;
    public bool active = true;
    public float mass = 1f; 

    private ParticleSystem particles;
    //private Vector3[] particleVelocities;

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
                //particlesCopy[i].position = Vector3.MoveTowards(pos, pos + windField.GetWind(pos), 1f);
                //particlesCopy[i].velocity = (particlesCopy[i].velocity * decelMultiplier) + windField.GetWind(pos);
                particlesCopy[i].velocity += windField.GetWind(pos);
            }

            particles.SetParticles(particlesCopy);
        }
        
    }
}
