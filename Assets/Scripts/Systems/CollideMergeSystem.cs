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

		private NativeHashMap<Entity, Entity> entitiesToDestroy;
		private EntityCommandBufferSystem bufferSystem;

		protected override void OnCreate()
		{
			bufferSystem = World.GetExistingSystem<EntityCommandBufferSystem>();
			entitiesToDestroy = new NativeHashMap<Entity, Entity>(1024, Allocator.Persistent);

			particleQuery = GetEntityQuery(
				ComponentType.ReadOnly<Bounded>(),
                typeof(Translation),
                typeof(Velocity),
				typeof(Mass),
				typeof(Radius),
				typeof(Scale)
			);
		}

		protected override void OnDestroy()
		{
			entitiesToDestroy.Dispose();
		}

		[BurstCompile]
		private struct CollideMergeJob : IJob
		{
			public float ParticleDensity;

			[DeallocateOnJobCompletion]
			public NativeArray<ArchetypeChunk> Chunks;

			[ReadOnly]
			public ArchetypeChunkEntityType EntityType;

			public ArchetypeChunkComponentType<Translation> PositionType;
			public ArchetypeChunkComponentType<Mass> MassType;
			public ArchetypeChunkComponentType<Radius> RadiusType;
			public ArchetypeChunkComponentType<Scale> ScaleType;
			public ArchetypeChunkComponentType<Velocity> VelocityType;

			public NativeHashMap<Entity, Entity> EntitiesToDestroy;

            private void ExecuteChunks(ArchetypeChunk chunk1, ArchetypeChunk chunk2)
            {
                NativeArray<Translation> transformData1 = chunk1.GetNativeArray(PositionType);
                NativeArray<Mass> massData1 = chunk1.GetNativeArray(MassType);
                NativeArray<Radius> radiusData1 = chunk1.GetNativeArray(RadiusType);
                NativeArray<Scale> scaleData1 = chunk1.GetNativeArray(ScaleType);
                NativeArray<Entity> entityData1 = chunk1.GetNativeArray(EntityType);
                NativeArray<Velocity> velocity1 = chunk1.GetNativeArray(VelocityType);

                NativeArray<Translation> transformData2 = chunk2.GetNativeArray(PositionType);
                NativeArray<Mass> massData2 = chunk2.GetNativeArray(MassType);
                NativeArray<Radius> radiusData2 = chunk2.GetNativeArray(RadiusType);
                NativeArray<Entity> entityData2 = chunk2.GetNativeArray(EntityType);
                NativeArray<Velocity> velocity2 = chunk2.GetNativeArray(VelocityType);

                for (int i = 0; i < chunk1.Count; i++)
                {
                    Entity current = entityData1[i];
                    if (EntitiesToDestroy.ContainsKey(current)) continue;

                    float3 pos1 = transformData1[i].Value;
                    float rad1 = radiusData1[i].Value;

                    for (int j = 0; j < chunk2.Count; j++)
                    {
                        Entity del = entityData2[j];
                        if (current == del) continue;
                        if (EntitiesToDestroy.ContainsKey(del)) continue;

                        float3 pos2 = transformData2[j].Value;
                        float rad = radiusData2[j].Value + rad1;

                        float distance = math.distancesq(pos1, pos2);
                        if (distance < rad * rad)
                        {
                            float mass = massData1[i].Value + massData2[j].Value;
                            float newRadius = math.pow(3 / (4 * math.PI) * mass / ParticleDensity, 1f / 3f);
                            float2 newVelocity = (velocity1[i].Value * massData1[i].Value + velocity2[j].Value * massData2[j].Value) / mass;
                            float3 newPos = (pos1 * massData1[i].Value + pos2 * massData2[j].Value) / mass;

                            massData1[i] = new Mass { Value = mass };
                            velocity1[i] = new Velocity { Value = newVelocity };
                            transformData1[i] = new Translation { Value = newPos };
                            radiusData1[i] = new Radius() { Value = newRadius };
                            scaleData1[i] = new Scale { Value = newRadius * 2f };

                            EntitiesToDestroy.TryAdd(del, del);
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
			ArchetypeChunkComponentType<Translation> transformType = GetArchetypeChunkComponentType<Translation>(false);
			ArchetypeChunkComponentType<Mass> massType = GetArchetypeChunkComponentType<Mass>(false);
			ArchetypeChunkComponentType<Radius> radiusType = GetArchetypeChunkComponentType<Radius>(false);
			ArchetypeChunkComponentType<Scale> scaleType = GetArchetypeChunkComponentType<Scale>(false);

			ArchetypeChunkComponentType<Velocity> velocityType = GetArchetypeChunkComponentType<Velocity>(false);

			entitiesToDestroy.Clear();

			CollideMergeJob job = new CollideMergeJob
			{
				ParticleDensity = ParticleDensity,
				Chunks = chunks,
				EntityType = entityType,
				PositionType = transformType,
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
