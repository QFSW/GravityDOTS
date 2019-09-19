using System;
using QFSW.QC;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace QFSW.GravityDOTS
{
	[UpdateInGroup(typeof(FixedSimulationSystemGroup))]
	public class SpawnParticlesSystem : ComponentSystem
	{
		public Entity ParticlePrefab;

		public float SpawnRate;

		public float ParticleMaxSpeed;

		public float2 ParticleMass;

		public float ParticleDensity;

		private float _remainingParticleSpawns;

		private EntityCommandBufferSystem _bufferSystem;

		public static readonly RenderBounds Bounds = new RenderBounds { Value = { Extents = new float3(1, 1, 1) } };

		private Random _random;

		protected override void OnCreate()
		{
			_bufferSystem = World.GetExistingSystem<FixedSimulationEntityCommandBufferSystem>();
			_random = new Random((uint)(new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds()));
		}

		protected override void OnUpdate()
		{
			EntityCommandBuffer buffer = _bufferSystem.CreateCommandBuffer();

			_remainingParticleSpawns += Time.deltaTime * SpawnRate;
			if (_remainingParticleSpawns > 1)
			{
				int spawnCount = (int)_remainingParticleSpawns;
				_remainingParticleSpawns -= spawnCount;

				SpawnParticles(buffer, spawnCount);
			}
		}

		[BurstCompile]
		public void SpawnParticles(EntityCommandBuffer buffer, int count)
		{
			float2 bottomLeft = (Vector2)Camera.main.ScreenToWorldPoint(new Vector2(0, 0));
			float2 topRight = (Vector2)Camera.main.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height));

			float2 minVal = new float2(-ParticleMaxSpeed, -ParticleMaxSpeed);
			float2 maxVel = new float2(ParticleMaxSpeed, ParticleMaxSpeed);

			for (int i = 0; i < count; ++i)
			{
				float mass = _random.NextFloat(ParticleMass.x, ParticleMass.y);
				float radius = math.pow(3 / (4 * math.PI) * mass / ParticleDensity, 1f / 3f);
				float scale = radius * 2f;

				Translation pos = new Translation()
					{ Value = new float3(_random.NextFloat2(bottomLeft, topRight), 0f) };

				Entity particle = buffer.Instantiate(ParticlePrefab);

				buffer.SetComponent(particle, pos);
				buffer.SetComponent(particle, new Scale() { Value = scale });
				buffer.SetComponent(particle, new Velocity() { Value = _random.NextFloat2(minVal, maxVel) });
				buffer.SetComponent(particle, new Mass() { Value = mass });
				buffer.SetComponent(particle, new Radius() { Value = radius });
			}
		}

		public void SpawnParticlesGrid(EntityCommandBuffer buffer, int count)
		{
			float2 bottomLeft = (Vector2)Camera.main.ScreenToWorldPoint(new Vector2(0, 0));
			float2 topRight = (Vector2)Camera.main.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height));

			float2 diff = (topRight - bottomLeft) / math.sqrt(count);

			float2 minVal = new float2(-ParticleMaxSpeed, -ParticleMaxSpeed);
			float2 maxVel = new float2(ParticleMaxSpeed, ParticleMaxSpeed);

			for (float y = bottomLeft.y + diff.y; y < topRight.y; y += diff.y)
			{
				for (float x = bottomLeft.x + diff.x; x < topRight.x; x += diff.x)
				{
					float mass = _random.NextFloat(ParticleMass.x, ParticleMass.y);
					float radius = math.pow(3 / (4 * math.PI) * mass / ParticleDensity, 1f / 3f);
					float scale = radius * 2f;

					Translation pos = new Translation()
						{ Value = new float3(x, y, 0f) };

					Entity particle = buffer.Instantiate(ParticlePrefab);

					buffer.SetComponent(particle, pos);
					buffer.SetComponent(particle, new Scale() { Value = scale });
					buffer.SetComponent(particle, new Velocity() { Value = _random.NextFloat2(minVal, maxVel) });
					buffer.SetComponent(particle, new Mass() { Value = mass });
					buffer.SetComponent(particle, new Radius() { Value = radius });
				}
			}
		}
	}
}
