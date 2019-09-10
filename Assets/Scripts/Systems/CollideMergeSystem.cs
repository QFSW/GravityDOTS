using System.Collections.Concurrent;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace QFSW.GravityDOTS
{
	[UpdateAfter(typeof(BoundingBoxSystem))]
	[UpdateInGroup(typeof(FixedSimulationSystemGroup))]
	public class CollideMergeSystem : JobComponentSystem
	{
		public float ParticleDensity;

		private EntityQuery particleQuery;

		private EntityCommandBufferSystem bufferSystem;

		protected override void OnCreate()
		{
			bufferSystem = World.GetOrCreateSystem<FixedSimulationEntityCommandBufferSystem>();

			particleQuery = GetEntityQuery(
				ComponentType.ReadOnly<Bounded>(),
				ComponentType.ReadOnly<Translation>(),
				ComponentType.ReadOnly<Velocity>(),
				ComponentType.ReadOnly<Mass>(),
				ComponentType.ReadOnly<Radius>()
			);
		}

		private struct CollideMergeJob : IJobParallelFor
		{
			public Entity ParticlePrefab;
			public float ParticleDensity;

			[ReadOnly, DeallocateOnJobCompletion]
			public NativeArray<ArchetypeChunk> Chunks;

			[ReadOnly]
			public ArchetypeChunkEntityType EntityType;

			[ReadOnly]
			public ArchetypeChunkComponentType<Translation> PositionType;

			[ReadOnly]
			public ArchetypeChunkComponentType<Mass> MassType;

			[ReadOnly]
			public ArchetypeChunkComponentType<Radius> RadiusType;

			[ReadOnly]
			public ArchetypeChunkComponentType<Velocity> VelocityType;

			public EntityCommandBuffer.Concurrent Buffer;

			public static ConcurrentDictionary<Entity, Entity> EntitiesToDestroy =
				new ConcurrentDictionary<Entity, Entity>();

			private void ExecuteChunks(int index, ArchetypeChunk chunk1, ArchetypeChunk chunk2)
			{
				NativeArray<Entity> entityData1 = chunk1.GetNativeArray(EntityType);
				NativeArray<Translation> positionData1 = chunk1.GetNativeArray(PositionType);
				NativeArray<Mass> massData1 = chunk1.GetNativeArray(MassType);
				NativeArray<Radius> radiusData1 = chunk1.GetNativeArray(RadiusType);
				NativeArray<Velocity> velocityData1 = chunk1.GetNativeArray(VelocityType);

				NativeArray<Entity> entityData2 = chunk2.GetNativeArray(EntityType);
				NativeArray<Translation> positionData2 = chunk2.GetNativeArray(PositionType);
				NativeArray<Mass> massData2 = chunk2.GetNativeArray(MassType);
				NativeArray<Radius> radiusData2 = chunk2.GetNativeArray(RadiusType);
				NativeArray<Velocity> velocityData2 = chunk2.GetNativeArray(VelocityType);

				for (int i1 = 0; i1 < chunk1.Count; i1++)
				{
					Entity entity1 = entityData1[i1];
					if (EntitiesToDestroy.ContainsKey(entity1)) continue;

					float3 pos1 = positionData1[i1].Value;
					float rad1 = radiusData1[i1].Value;

					for (int i2 = i1; i2 < chunk2.Count; i2++)
					{
						Entity entity2 = entityData2[i2];
						if (entity1 == entity2) continue;
						if (EntitiesToDestroy.ContainsKey(entity2)) continue;

						float3 pos2 = positionData2[i2].Value;
						float rad = rad1 + radiusData2[i2].Value;

						float distancesq = math.distancesq(pos1, pos2);
						if (distancesq < rad * rad)
						{
							float newMass = massData1[i1].Value + massData2[i2].Value;
							float newRadius = math.pow(3f / (4f * math.PI) * newMass / ParticleDensity, 1f / 3f);
							float2 newVelocity =
								(velocityData1[i1].Value * massData1[i1].Value +
								 velocityData2[i2].Value * massData2[i2].Value) / newMass;
							float3 newPos = (pos1 * massData1[i1].Value + pos2 * massData2[i2].Value) / newMass;

							Entity particle = Buffer.Instantiate(index, ParticlePrefab);

							Buffer.SetComponent(index, particle, new Translation { Value = newPos });
							Buffer.SetComponent(index, particle, new Scale { Value = newRadius * 2 });
							Buffer.SetComponent(index, particle, new Velocity { Value = newVelocity });
							Buffer.SetComponent(index, particle, new Mass { Value = newMass });
							Buffer.SetComponent(index, particle, new Radius { Value = newRadius });

							EntitiesToDestroy.TryAdd(entity1, entity1);
							EntitiesToDestroy.TryAdd(entity2, entity2);

							Buffer.DestroyEntity(index, entity1);
							Buffer.DestroyEntity(index, entity2);
						}
					}
				}
			}

			public void Execute(int index)
			{
				ArchetypeChunk chunk1 = Chunks[index];

				for (int i2 = index; i2 < Chunks.Length; i2++)
				{
					ArchetypeChunk chunk2 = Chunks[i2];
					ExecuteChunks(index, chunk1, chunk2);
				}
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			EntityCommandBuffer buffer = bufferSystem.CreateCommandBuffer();

			NativeArray<ArchetypeChunk> chunks = particleQuery.CreateArchetypeChunkArray(Allocator.TempJob);

			ArchetypeChunkEntityType entityType = GetArchetypeChunkEntityType();
			ArchetypeChunkComponentType<Translation> transformType = GetArchetypeChunkComponentType<Translation>(true);
			ArchetypeChunkComponentType<Mass> massType = GetArchetypeChunkComponentType<Mass>(true);
			ArchetypeChunkComponentType<Radius> radiusType = GetArchetypeChunkComponentType<Radius>(true);
			ArchetypeChunkComponentType<Velocity> velocityType = GetArchetypeChunkComponentType<Velocity>(true);

			CollideMergeJob.EntitiesToDestroy.Clear();

			CollideMergeJob job = new CollideMergeJob
			{
				ParticlePrefab = World.GetExistingSystem<SpawnParticlesSystem>().ParticlePrefab,
				ParticleDensity = ParticleDensity,
				Chunks = chunks,
				EntityType = entityType,
				PositionType = transformType,
				MassType = massType,
				RadiusType = radiusType,
				VelocityType = velocityType,
				Buffer = buffer.ToConcurrent()
			};

			JobHandle handle = job.Schedule(chunks.Length, 1, inputDeps);
			bufferSystem.AddJobHandleForProducer(handle);
			return handle;
		}
	}
}
