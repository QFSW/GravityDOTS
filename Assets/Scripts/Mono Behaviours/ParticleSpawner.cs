using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;

namespace QFSW.GravityDOTS
{
    public class ParticleSpawner : MonoBehaviour
    {
        [SerializeField] private int _particleCount = 100;

        private EntityManager _entityManager;
        private EntityArchetype _particleType;

        private void Awake()
        {
            _entityManager = World.Active.EntityManager;
            _particleType = _entityManager.CreateArchetype(typeof(Velocity), typeof(Translation));
        }

        private void Start()
        {
            SpawnParticles(_particleCount);
        }

        public void SpawnParticles(int count)
        {
            NativeArray<Entity> particles = new NativeArray<Entity>(count, Allocator.Temp);
            _entityManager.CreateEntity(_particleType, particles);

            for (int i = 0; i < count; i++)
            {
                _entityManager.SetComponentData(particles[i], new Velocity { Value = new float2(2, 1) });
                _entityManager.SetComponentData(particles[i], new Translation { Value = new float3() });
            }

            particles.Dispose();
        }
    }
}
