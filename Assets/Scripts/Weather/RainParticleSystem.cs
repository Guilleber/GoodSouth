using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class RainParticleSystem : MonoBehaviour
{
    private ParticleSystem m_particleSystem;
    private List<ParticleCollisionEvent> collisionEvents;

    void Start()
    {
        m_particleSystem = GetComponent<ParticleSystem>();
        collisionEvents = new List<ParticleCollisionEvent>();
    }

    void OnParticleCollision(GameObject other)
    {
        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[m_particleSystem.particleCount];
        m_particleSystem.GetParticles(particles);
        Bounds particleBounds;

        bool isWater = other.name == "Sea";

        for (int i = 0; i < particles.Length; i++) {
            foreach (Collider collider in other.GetComponents<Collider>())
            {
                if (collider.isTrigger)
                    continue;
                particleBounds = new Bounds(particles[i].position,
                                            particles[i].GetCurrentSize3D(m_particleSystem));
                if (collider.bounds.Intersects(particleBounds))
                {
                    if (isWater) { m_particleSystem.TriggerSubEmitter(0, ref particles[i]); }
                }
            }
        }
    }
}