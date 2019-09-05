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
        [SerializeField] private int _particleCount = 100;
        [SerializeField] private float _particleMaxSpeed = 3;

        [SerializeField] private float2 _particleMass = new float2(100, 1000);
        [SerializeField] private float _particleDensity = 1;

        [SerializeField] private Material _particleMaterial;
        [SerializeField] private Mesh _particleMesh;
        
        private EntityManager _entityManager;
        private EntityArchetype _particleType;

        private void Awake()
        {
            ComponentType[] particleComponents =
            {
                typeof(Velocity), typeof(LocalToWorld), typeof(Scale),
                typeof(Mass), typeof(Radius), typeof(Bounded),
                typeof(RenderMesh), typeof(RenderBounds)
            };

            _entityManager = World.Active.EntityManager;
            _particleType = _entityManager.CreateArchetype(particleComponents);

            World.Active.GetOrCreateSystem<CollideMergeSystem>().ParticleDensity = _particleDensity;
        }

        private void Start()
        {
            SpawnParticles(_particleCount);
        }

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

            float2 minVal = new float2(-_particleMaxSpeed, -_particleMaxSpeed);
            float2 maxVel = new float2(_particleMaxSpeed, _particleMaxSpeed);

            for (int i = 0; i < particles.Length; i++)
            {
                float mass = r.NextFloat(_particleMass.x, _particleMass.y);
                float radius = math.pow(3 / (4 * math.PI) * mass / _particleDensity, 1 / 3f);

                Entity particle = particles[i];
                _entityManager.SetComponentData(particle, pos);                        
                _entityManager.SetComponentData(particle, bounds);
                _entityManager.SetSharedComponentData(particle, rm);
                _entityManager.SetComponentData(particle, new Velocity() { Value = r.NextFloat2(minVal, maxVel) });
                _entityManager.SetComponentData(particle, new Radius() { Value = radius });
                _entityManager.SetComponentData(particle, new Scale() { Value = radius * 2f });
            }

            particles.Dispose();
        }
    }
}
