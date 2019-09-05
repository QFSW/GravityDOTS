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

		private NativeHashMap<Entity, Entity> entitiesToDestroy;
		private EntityCommandBufferSystem bufferSystem;

		protected override void OnCreate()
		{
			bufferSystem = World.GetExistingSystem<EntityCommandBufferSystem>();
			entitiesToDestroy = new NativeHashMap<Entity, Entity>(1024, Allocator.Persistent);

			particleQuery = GetEntityQuery(
				ComponentType.ReadOnly<LocalToWorld>(),
				ComponentType.ReadOnly<Bounded>(),
				typeof(Mass),
				typeof(Radius),
				typeof(Scale)
			);
		}

		protected override void OnDestroy()
		{
			entitiesToDestroy.Dispose();
		}

		private struct CollideMergeJob : IJob
		{
			public float ParticleDensity;
			public float DeltaTime;

			[DeallocateOnJobCompletion]
			public NativeArray<ArchetypeChunk> Chunks;

			[ReadOnly]
			public ArchetypeChunkEntityType EntityType;
			
			[ReadOnly]
			public ArchetypeChunkComponentType<LocalToWorld> TransformType;

			public ArchetypeChunkComponentType<Mass> MassType;
			public ArchetypeChunkComponentType<Radius> RadiusType;
			public ArchetypeChunkComponentType<Scale> ScaleType;

			public ArchetypeChunkComponentType<Velocity> VelocityType;

			public NativeHashMap<Entity, Entity> EntitiesToDestroy;

			private void ExecuteChunks(ArchetypeChunk chunk1, ArchetypeChunk chunk2)
			{
				NativeArray<LocalToWorld> transformData1 = chunk1.GetNativeArray(TransformType);
				NativeArray<Mass> massData1 = chunk1.GetNativeArray(MassType);
				NativeArray<Radius> radiusData1 = chunk1.GetNativeArray(RadiusType);
				NativeArray<Scale> scaleData1 = chunk1.GetNativeArray(ScaleType);
				NativeArray<Entity> entityData1 = chunk1.GetNativeArray(EntityType);

				NativeArray<LocalToWorld> transformData2 = chunk2.GetNativeArray(TransformType);
				NativeArray<Mass> massData2 = chunk2.GetNativeArray(MassType);
				NativeArray<Radius> radiusData2 = chunk2.GetNativeArray(RadiusType);
				NativeArray<Entity> entityData2 = chunk2.GetNativeArray(EntityType);

				if (chunk1.Has(VelocityType))
				{
					NativeArray<Velocity> velocity1 = chunk1.GetNativeArray(VelocityType);

					// both chunks have velocity
					if (chunk2.Has(VelocityType))
					{
						NativeArray<Velocity> velocity2 = chunk2.GetNativeArray(VelocityType);

						for (int i = 0; i < chunk1.Count; i++)
						{
							Entity current = entityData1[i];
							if (EntitiesToDestroy.ContainsKey(current)) continue;

							float3 pos1 = transformData1[i].Position;
							float rad1 = radiusData1[i].Value;

							for (int j = 0; j < chunk2.Count; j++)
							{
								Entity del = entityData2[j];
								if (EntitiesToDestroy.ContainsKey(del)) continue;

								float3 pos2 = transformData2[j].Position;
								float rad = radiusData2[j].Value + rad1;

								if (!(math.distancesq(pos1, pos2) < rad * rad)) continue;
								
								velocity1[i] = new Velocity
									{ Value = (velocity1[i].Value + velocity2[j].Value) / 2f };
								
								float mass = massData1[i].Value + massData2[j].Value;
								float newRadius = math.pow(3 / (4 * math.PI) * mass / ParticleDensity, 1f / 3f);
								massData1[i] = new Mass { Value = mass };
								radiusData1[i] = new Radius() { Value = newRadius };
								scaleData1[i] = new Scale { Value = newRadius * 2f };

								
								EntitiesToDestroy.TryAdd(del, del);
							}
						}
					}
					else // chunk1 has velocity, chunk2 does not
					{
						for (int i = 0; i < chunk1.Count; i++)
						{
							for (int j = 0; j < chunk2.Count; j++)
							{

							}
						}
					}
				}
				else
				{
					// chunk1 does not have velocity, chunk2 does
					if (chunk2.Has(VelocityType))
					{
						NativeArray<Velocity> velocity2 = chunk2.GetNativeArray(VelocityType);

						for (int i = 0; i < chunk1.Count; i++)
						{
							for (int j = 0; j < chunk2.Count; j++)
							{

							}
						}
					}
					else // neither chunks have velocity
					{
						for (int i = 0; i < chunk1.Count; i++)
						{
							for (int j = 0; j < chunk2.Count; j++)
							{

							}
						}
					}
				}
			}

			public void Execute()
			{
				for (int i = 0; i < Chunks.Length; i++)
				{
					ArchetypeChunk chunk = Chunks[i];

					for (int j = 0; j < Chunks.Length; j++)
					{
						ExecuteChunks(chunk, Chunks[j]);
					}
				}
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			EntityCommandBuffer buffer = bufferSystem.CreateCommandBuffer();

			using (NativeArray<Entity> arr = entitiesToDestroy.GetKeyArray(Allocator.Temp))
			{
				for (int i = 0; i < arr.Length; i++)
				{
					buffer.DestroyEntity(arr[i]);
				}
			}

			NativeArray<ArchetypeChunk> chunks = particleQuery.CreateArchetypeChunkArray(Allocator.TempJob);

			ArchetypeChunkEntityType entityType = GetArchetypeChunkEntityType();
			ArchetypeChunkComponentType<LocalToWorld> transformType = GetArchetypeChunkComponentType<LocalToWorld>(true);
			ArchetypeChunkComponentType<Mass> massType = GetArchetypeChunkComponentType<Mass>(false);
			ArchetypeChunkComponentType<Radius> radiusType = GetArchetypeChunkComponentType<Radius>(false);
			ArchetypeChunkComponentType<Scale> scaleType = GetArchetypeChunkComponentType<Scale>(false);

			ArchetypeChunkComponentType<Velocity> velocityType = GetArchetypeChunkComponentType<Velocity>(false);

			entitiesToDestroy.Clear();

			CollideMergeJob job = new CollideMergeJob
			{
				DeltaTime = Time.deltaTime,
				ParticleDensity = ParticleDensity,
				Chunks = chunks,
				EntityType = entityType,
				TransformType = transformType,
				MassType = massType,
				RadiusType = radiusType,
				ScaleType = scaleType,
				VelocityType = velocityType,
				EntitiesToDestroy = entitiesToDestroy,
			};

			return job.Schedule(inputDeps);
		}
	}
}
