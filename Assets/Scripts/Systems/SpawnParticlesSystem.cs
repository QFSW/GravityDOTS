using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace QFSW.GravityDOTS
{
	public class SpawnParticlesSystem : ComponentSystem
	{
		public EntityArchetype ParticleType;

		public RenderMesh RenderMesh;
		
		public float SpawnRate;

		public float ParticleMaxSpeed;

		public float2 ParticleMass;

		public float ParticleDensity;
		
		private float _remainingParticleSpawns;
				
		private EntityCommandBufferSystem _bufferSystem;
		
		private static readonly RenderBounds Bounds = new RenderBounds { Value = { Extents = new float3(1, 1, 1) } };
		
		protected override void OnCreate()
		{
			_bufferSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
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

		private void SpawnParticles(EntityCommandBuffer buffer, int count)
		{
			float2 bottomLeft = (Vector2)Camera.main.ScreenToWorldPoint(new Vector2(0, 0));
			float2 topRight = (Vector2)Camera.main.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height));
			
			Random r = new Random((uint)(1 + count + Time.time * 1000));
			
			float2 minVal = new float2(-ParticleMaxSpeed, -ParticleMaxSpeed);
			float2 maxVel = new float2(ParticleMaxSpeed, ParticleMaxSpeed);

			for (int i = 0; i < count; ++i)
			{
				float mass = r.NextFloat(ParticleMass.x, ParticleMass.y);
				float radius = math.pow(3 / (4 * math.PI) * mass / ParticleDensity, 1f / 3f);
				float scale = radius * 2f;

				Translation pos = new Translation() { Value = new float3(r.NextFloat2(bottomLeft, topRight), 0f) };

				Entity particle = buffer.CreateEntity(ParticleType);
				buffer.SetComponent(particle, pos);
				buffer.SetComponent(particle, Bounds);
				buffer.SetComponent(particle, new Mass() { Value = mass });
				buffer.SetComponent(particle, new Velocity() { Value = r.NextFloat2(minVal, maxVel) });
				buffer.SetComponent(particle, new Radius() { Value = radius });
				buffer.SetComponent(particle, new Scale() { Value = scale });
				buffer.SetSharedComponent(particle, RenderMesh);
			}
		}
	}
}
