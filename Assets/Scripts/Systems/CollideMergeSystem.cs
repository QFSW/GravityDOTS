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

		private EntityCommandBufferSystem _bufferSystem;

		private NativeHashMap<Entity, byte> _destroyedEntities;
		private NativeQueue<CollisionData> _collisions;

		protected override void OnCreate()
		{
			_bufferSystem = World.GetOrCreateSystem<FixedSimulationEntityCommandBufferSystem>();

			particleQuery = GetEntityQuery(
				ComponentType.ReadOnly<Bounded>(),
				ComponentType.ReadOnly<Translation>(),
				ComponentType.ReadOnly<Velocity>(),
				ComponentType.ReadOnly<Mass>(),
				ComponentType.ReadOnly<Radius>()
			);

			_destroyedEntities = new NativeHashMap<Entity, byte>(64, Allocator.Persistent);
			_collisions = new NativeQueue<CollisionData>(Allocator.Persistent);
		}

		protected override void OnDestroy()
		{
			_destroyedEntities.Dispose();
			_collisions.Dispose();
		}

		private struct CollisionData
		{
			public Entity Entity1, Entity2;
			public float3 NewPosition;
			public float NewRadius;
			public float NewMass;
			public float2 NewVelocity;
		}

		private struct CollideMergeJob : IJobParallelFor
		{
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

			public NativeQueue<CollisionData>.ParallelWriter CollisionQueue;

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

					float3 pos1 = positionData1[i1].Value;
					float rad1 = radiusData1[i1].Value;

					for (int i2 = i1; i2 < chunk2.Count; i2++)
					{
						Entity entity2 = entityData2[i2];
						if (entity1 == entity2) continue;

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

							CollisionQueue.Enqueue(new CollisionData()
							{
								Entity1 = entity1,
								Entity2 = entity2,
								NewPosition = newPos,
								NewMass = newMass,
								NewRadius = newRadius,
								NewVelocity = newVelocity,
							});
						}
					}
				}
			}

			public void Execute(int index)
			{
				ArchetypeChunk chunk1 = Chunks[index];

				for (int i2 = 0; i2 < Chunks.Length; i2++)
				{
					ArchetypeChunk chunk2 = Chunks[i2];
					ExecuteChunks(index, chunk1, chunk2);
				}
			}
		}

		private struct ProcessCollisionsJob : IJob
		{
			public Entity ParticlePrefab;

			public NativeQueue<CollisionData> CollisionQueue;

			public NativeHashMap<Entity, byte> DestroyedEntities;

			public EntityCommandBuffer Buffer;

			public void Execute()
			{
				while (CollisionQueue.TryDequeue(out CollisionData data))
				{
					if (DestroyedEntities.ContainsKey(data.Entity1) || DestroyedEntities.ContainsKey(data.Entity2))
						continue;

					Entity particle = Buffer.Instantiate(ParticlePrefab);

					Buffer.SetComponent(particle, new Translation { Value = data.NewPosition });
					Buffer.SetComponent(particle, new Scale { Value = data.NewRadius * 2 });
					Buffer.SetComponent(particle, new Velocity { Value = data.NewVelocity });
					Buffer.SetComponent(particle, new Mass { Value = data.NewMass });
					Buffer.SetComponent(particle, new Radius { Value = data.NewRadius });

					DestroyedEntities.TryAdd(data.Entity1, 0);
					DestroyedEntities.TryAdd(data.Entity2, 0);

					Buffer.DestroyEntity(data.Entity1);
					Buffer.DestroyEntity(data.Entity2);
				}
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			EntityCommandBuffer buffer = _bufferSystem.CreateCommandBuffer();

			NativeArray<ArchetypeChunk> chunks = particleQuery.CreateArchetypeChunkArray(Allocator.TempJob);

			ArchetypeChunkEntityType entityType = GetArchetypeChunkEntityType();
			ArchetypeChunkComponentType<Translation> transformType = GetArchetypeChunkComponentType<Translation>(true);
			ArchetypeChunkComponentType<Mass> massType = GetArchetypeChunkComponentType<Mass>(true);
			ArchetypeChunkComponentType<Radius> radiusType = GetArchetypeChunkComponentType<Radius>(true);
			ArchetypeChunkComponentType<Velocity> velocityType = GetArchetypeChunkComponentType<Velocity>(true);

			CollideMergeJob job = new CollideMergeJob
			{
				ParticleDensity = ParticleDensity,
				Chunks = chunks,
				EntityType = entityType,
				PositionType = transformType,
				MassType = massType,
				RadiusType = radiusType,
				VelocityType = velocityType,
				CollisionQueue = _collisions.AsParallelWriter()
			};
			JobHandle handle = job.Schedule(chunks.Length, 1, inputDeps);

			_destroyedEntities.Clear();
			ProcessCollisionsJob processCollisionsJob = new ProcessCollisionsJob()
			{
				ParticlePrefab = World.GetExistingSystem<SpawnParticlesSystem>().ParticlePrefab,
				Buffer = buffer,
				CollisionQueue = _collisions,
				DestroyedEntities = _destroyedEntities
			};
			handle = processCollisionsJob.Schedule(handle);
			_bufferSystem.AddJobHandleForProducer(handle);

			return handle;
		}
	}
}
