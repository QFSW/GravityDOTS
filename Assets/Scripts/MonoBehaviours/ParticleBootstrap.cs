using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
#if !QC_DISABLE
using QFSW.QC;
#endif

using Random = Unity.Mathematics.Random;

namespace QFSW.GravityDOTS
{
#if !QC_DISABLE
	[CommandPrefix("particles."), Preserve]
#endif
	public class ParticleBootstrap : MonoBehaviour
	{
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
		private EntityQuery _particleQuery;

		private Entity _particlePrefab;
		private RenderMesh _renderMesh;

		private SpawnParticlesSystem _spawnParticlesSystem;

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
			_particleQuery = _entityManager.CreateEntityQuery(_particleComponents);

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

			_spawnParticlesSystem = World.Active.GetExistingSystem<SpawnParticlesSystem>();

			World.Active.GetOrCreateSystem<CollideMergeSystem>().ParticleDensity = _particleDensity;
			_spawnParticlesSystem.ParticlePrefab = _particlePrefab;
			_spawnParticlesSystem.ParticleDensity = _particleDensity;
			_spawnParticlesSystem.ParticleMass = _particleMass;
			_spawnParticlesSystem.SpawnRate = _spawnRate;
			_spawnParticlesSystem.ParticleMaxSpeed = _particleMaxSpeed;
		}

#if !QC_DISABLE
		[Command("spawn-rate"), Preserve]
#endif
		private void SpawnRate(int value)
		{
			_spawnRate = value;
			_spawnParticlesSystem.SpawnRate = _spawnRate;
		}

#if !QC_DISABLE
		[Command("spawn-particles"), Preserve]
#endif
		private void SpawnParticles(int count)
		{
			_spawnParticlesSystem.SpawnParticlesGrid(
				World.Active.GetOrCreateSystem<FixedSimulationEntityCommandBufferSystem>().CreateCommandBuffer(),
				count);
		}

		private void OnValidate()
		{
			if (Application.isPlaying)
			{
				World.Active.GetExistingSystem<CollideMergeSystem>().ParticleDensity = _particleDensity;
				_spawnParticlesSystem = World.Active.GetExistingSystem<SpawnParticlesSystem>();
				_spawnParticlesSystem.SpawnRate = _spawnRate;
				_spawnParticlesSystem.ParticleDensity = _particleDensity;
				_spawnParticlesSystem.ParticleMass = _particleMass;
				_spawnParticlesSystem.ParticleMaxSpeed = _particleMaxSpeed;
			}
		}

#if !QC_DISABLE
		[Command("clear-particles"), Preserve]
#endif
		private void ClearParticles()
		{
			_entityManager.DestroyEntity(_particleQuery);
		}

#if !QC_DISABLE
		[Command("count"), Preserve]
#endif
		private int GetParticleCount()
		{
			return _entityManager.CreateEntityQuery(_particleComponents).CalculateEntityCount();
		}
	}
}
