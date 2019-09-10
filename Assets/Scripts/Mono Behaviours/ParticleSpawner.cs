using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

#if !QC_DISABLE
using QFSW.QC;
#endif

using Random = Unity.Mathematics.Random;

namespace QFSW.GravityDOTS
{
#if !QC_DISABLE
    [CommandPrefix("particles.")]
#endif
    public class ParticleSpawner : MonoBehaviour
    {
        [SerializeField]
        private int _particleCount = 100;

#if !QC_DISABLE
        [Command("spawn-rate")]
#endif
        [SerializeField]
        private float _spawnRate = 100;

        public static EntityArchetype ParticleType;

        [SerializeField]
        private float _particleMaxSpeed = 3;

        [SerializeField]
        private float2 _particleMass = new float2(100, 1000);

        [SerializeField]
        private float _particleDensity = 1;

        [SerializeField]
        private Material _particleMaterial;

        [SerializeField]
        private Mesh _particleMesh;

        private float _remainingParticleSpawns;
        private EntityManager _entityManager;
        private ComponentType[] _particleComponents;

        private Entity _particlePrefab;
        private RenderMesh _renderMesh;

        private void Awake()
        {
            _particleComponents = new ComponentType[]
            {
                typeof(LocalToWorld),
                typeof(Translation),
                typeof(Scale),
                typeof(Velocity),
                typeof(Mass),
                typeof(Radius),
                typeof(Bounded),
                typeof(RenderBounds),
                typeof(RenderMesh),
            };

            _entityManager = World.Active.EntityManager;
            ParticleType = _entityManager.CreateArchetype(_particleComponents);

            _renderMesh = new RenderMesh
            {
                mesh = _particleMesh,
                material = _particleMaterial,
                castShadows = ShadowCastingMode.Off,
                receiveShadows = false
            };

            _particlePrefab = _entityManager.CreateEntity(ParticleType);
            _entityManager.AddComponent(_particlePrefab, typeof(Prefab));
            _entityManager.SetComponentData(_particlePrefab, SpawnParticlesSystem.Bounds);
            _entityManager.SetSharedComponentData(_particlePrefab, _renderMesh);

            World.Active.GetOrCreateSystem<CollideMergeSystem>().ParticleDensity = _particleDensity;
            SpawnParticlesSystem spawnParticlesSystem = World.Active.GetOrCreateSystem<SpawnParticlesSystem>();
            spawnParticlesSystem.ParticlePrefab = _particlePrefab;
            spawnParticlesSystem.ParticleDensity = _particleDensity;
            spawnParticlesSystem.ParticleMass = _particleMass;
            spawnParticlesSystem.SpawnRate = _spawnRate;
            spawnParticlesSystem.ParticleMaxSpeed = _particleMaxSpeed;

            spawnParticlesSystem.SpawnParticles(
                World.Active.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>().CreateCommandBuffer(),
                _particleCount);
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                World.Active.GetExistingSystem<CollideMergeSystem>().ParticleDensity = _particleDensity;
                SpawnParticlesSystem spawnParticlesSystem = World.Active.GetExistingSystem<SpawnParticlesSystem>();
                spawnParticlesSystem.SpawnRate = _spawnRate;
                spawnParticlesSystem.ParticleDensity = _particleDensity;
                spawnParticlesSystem.ParticleMass = _particleMass;
                spawnParticlesSystem.ParticleMaxSpeed = _particleMaxSpeed;
            }
        }
        
#if !QC_DISABLE
        [Command("count")]
#endif
        private int GetParticleCount()
        {
            return _entityManager.CreateEntityQuery(_particleComponents).CalculateEntityCount();
        }
    }
}
