using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

using Random = Unity.Mathematics.Random;

namespace QFSW.GravityDOTS
{
    public class ParticleSpawner : MonoBehaviour
    {
        [SerializeField]
        private int _particleCount = 100;

        [SerializeField]
        private Material _particleMaterial;

        [SerializeField]
        private Mesh _particleMesh;
        
        private EntityManager _entityManager;
        private EntityArchetype _particleType;

        private void Awake()
        {
            _entityManager = World.Active.EntityManager;
            _particleType = _entityManager.CreateArchetype(typeof(Velocity), typeof(LocalToWorld), typeof(RenderMesh), typeof(RenderBounds));
        }

        private void Start()
        {
            SpawnParticles(_particleCount);
        }

        private static readonly float2 Min = new float2(-3, -3), Max = new float2(3, 3);

        public void SpawnParticles(int count)
        {
            LocalToWorld pos = new LocalToWorld { Value = float4x4.Translate(float3.zero) };
            
            RenderMesh rm = new RenderMesh
            {
                mesh = _particleMesh,
                material = _particleMaterial,
                castShadows = ShadowCastingMode.Off,
                receiveShadows = false
            };

            RenderBounds bounds = new RenderBounds { Value = { Extents = new float3(1, 1, 1) } };

            NativeArray<Entity> particles = new NativeArray<Entity>(count, Allocator.TempJob);
            _entityManager.CreateEntity(_particleType, particles);

            Random r = new Random((uint)count + 1);

            for (int i = 0; i < particles.Length; i++)
            {
                _entityManager.SetComponentData(particles[i], pos);                        
                _entityManager.SetComponentData(particles[i], bounds);
                _entityManager.SetSharedComponentData(particles[i], rm);
                _entityManager.SetComponentData(particles[i], new Velocity() { Value = r.NextFloat2(Min, Max) });
                
            }

            particles.Dispose();
        }
    }
}
