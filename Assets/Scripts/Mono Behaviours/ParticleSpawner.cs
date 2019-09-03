using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;

namespace QFSW.GravityDOTS
{
    public class ParticleSpawner : MonoBehaviour
    {
        private EntityManager entityManager;
        private EntityArchetype particleType;

        private void Awake()
        {
            entityManager = World.Active.EntityManager;
            particleType = entityManager.CreateArchetype(typeof(Velocity), typeof(Translation));
        }

        private void Start()
        {
            SpawnParticles(100);
        }

        public void SpawnParticles(int count)
        {
            NativeArray<Entity> particles = new NativeArray<Entity>(count, Allocator.Temp);
            entityManager.CreateEntity(particleType, particles);

            for (int i = 0; i < count; i++)
            {
                entityManager.SetComponentData(particles[i], new Velocity { Value = new float2(2, 1) });
                entityManager.SetComponentData(particles[i], new Translation { Value = new float3() });
            }

            particles.Dispose();
        }
    }
}
