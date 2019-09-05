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

		protected override void OnCreate()
		{
			particleQuery = GetEntityQuery(
				ComponentType.ReadOnly<Bounded>(),
				typeof(Mass),
				typeof(Radius),
				typeof(Scale)
			);
		}

		private struct CollideMergeJob : IJobParallelFor
		{
			public float ParticleDensity;
			public float DeltaTime;

			public NativeArray<ArchetypeChunk> Chunks;

			public ArchetypeChunkComponentType<Mass> ChunkMass;
			public ArchetypeChunkComponentType<Radius> ChunkRadius;
			public ArchetypeChunkComponentType<Scale> ChunkScale;

			public ArchetypeChunkComponentType<Velocity> ChunkVelocity;

			private void ExecuteChunks(ArchetypeChunk chunk1, ArchetypeChunk chunk2)
			{
				NativeArray<Mass> mass1 = chunk1.GetNativeArray(ChunkMass);
				NativeArray<Radius> radius1 = chunk1.GetNativeArray(ChunkRadius);
				NativeArray<Scale> scale1 = chunk1.GetNativeArray(ChunkScale);

				NativeArray<Mass> mass2 = chunk2.GetNativeArray(ChunkMass);
				NativeArray<Radius> radius2 = chunk2.GetNativeArray(ChunkRadius);
				
				// instead of 4 separate cases
				
				// no velocity
				// chunk1 has velocity, chunk2 does not
				// chunk1 does not, chunk2 has velocity
				// both have velocity
				
				// maybe reorder so that there are just 3 separate cases
				
				// no velocity
				// a chunk has velocity, the other does not
				// both have velocity
				if (chunk1.Has(ChunkVelocity))
				{
					NativeArray<Velocity> velocity1 = chunk1.GetNativeArray(ChunkVelocity);

					if (chunk2.Has(ChunkVelocity))
					{
						NativeArray<Velocity> velocity2 = chunk2.GetNativeArray(ChunkVelocity);
					}
					else
					{
						
					}
				}
				else
				{
					if (chunk2.Has(ChunkVelocity))
					{
						NativeArray<Velocity> velocity2 = chunk2.GetNativeArray(ChunkVelocity);
					}
					else
					{
						
					}
				}
			}

			public void Execute(int index)
			{
				ArchetypeChunk chunk = Chunks[index];

				for (int i = 0; i < Chunks.Length; i++)
				{
					ExecuteChunks(chunk, Chunks[i]);
				}
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			ArchetypeChunkComponentType<Mass> chunkMass = GetArchetypeChunkComponentType<Mass>(false);
			ArchetypeChunkComponentType<Radius> chunkRadius = GetArchetypeChunkComponentType<Radius>(false);
			ArchetypeChunkComponentType<Scale> chunkScale = GetArchetypeChunkComponentType<Scale>(false);

			ArchetypeChunkComponentType<Velocity> chunkVelocity = GetArchetypeChunkComponentType<Velocity>(false);

			NativeArray<ArchetypeChunk> chunks = particleQuery.CreateArchetypeChunkArray(Allocator.TempJob);
			
			CollideMergeJob job = new CollideMergeJob
			{
				DeltaTime = Time.deltaTime,
				ParticleDensity = ParticleDensity,
				Chunks = chunks,
				ChunkMass = chunkMass,
				ChunkRadius = chunkRadius,
				ChunkScale = chunkScale,
				ChunkVelocity = chunkVelocity
			};
			
			return job.Schedule(chunks.Length, 1, inputDeps);
		}
	}
}
