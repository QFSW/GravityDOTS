using System;
using System.Collections;
using System.Diagnostics;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = Unity.Mathematics.Random;

namespace QFSW.GravityDOTS
{
    public class ParticleSpawner : MonoBehaviour
    {
        [SerializeField]
        private int _particleCount = 100;

        [SerializeField]
        private Material _particleMaterial;
        
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

        private static readonly float2 Min = new float2(-3, -3), Max = new float2(3, 3);

        public void SpawnParticles(int count)
        {
            NativeArray<Entity> particles = new NativeArray<Entity>(count, Allocator.TempJob);
            _entityManager.CreateEntity(_particleType, particles);

            Random r = new Random((uint)count + 1);

            for (int i = 0; i < particles.Length; i++)
            {
                _entityManager.SetComponentData(particles[i], new Velocity() { Value = r.NextFloat2(Min, Max) });
            }

            particles.Dispose();
        }
    }
}
