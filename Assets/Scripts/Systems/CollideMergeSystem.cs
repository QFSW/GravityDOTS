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
			bufferSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
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
		private struct CollideMergeJob : IJobForEachWithEntity<Translation, Velocity, Mass, Radius>
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

			public void Execute(Entity entity, int index, [ReadOnly] ref Translation pos, [ReadOnly] ref Velocity vel,
				[ReadOnly] ref Mass mass, [ReadOnly] ref Radius rad)
			{
				for (int c = 0; c < Chunks.Length; c++)
				{
					ArchetypeChunk chunk = Chunks[c];

					NativeArray<Translation> positionData = chunk.GetNativeArray(PositionType);
					NativeArray<Mass> massData = chunk.GetNativeArray(MassType);
					NativeArray<Radius> radiusData = chunk.GetNativeArray(RadiusType);
					NativeArray<Velocity> velocityData = chunk.GetNativeArray(VelocityType);

					for (int i = 0; i < chunk.Count; i++)
					{
						
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
			ArchetypeChunkComponentType<Translation> transformType = GetArchetypeChunkComponentType<Translation>(true);
			ArchetypeChunkComponentType<Mass> massType = GetArchetypeChunkComponentType<Mass>(true);
			ArchetypeChunkComponentType<Radius> radiusType = GetArchetypeChunkComponentType<Radius>(true);
			ArchetypeChunkComponentType<Velocity> velocityType = GetArchetypeChunkComponentType<Velocity>(true);

			entitiesToDestroy.Clear();

			CollideMergeJob job = new CollideMergeJob
			{
				ParticleDensity = ParticleDensity,
				Chunks = chunks,
				EntityType = entityType,
				PositionType = transformType,
				MassType = massType,
				RadiusType = radiusType,
				VelocityType = velocityType,
			};

			JobHandle handle = job.Schedule(this, inputDeps);
			bufferSystem.AddJobHandleForProducer(handle);
			return handle;
		}
	}
}
