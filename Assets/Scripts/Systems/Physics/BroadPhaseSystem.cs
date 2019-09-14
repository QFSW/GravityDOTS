using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace QFSW.GravityDOTS.Physics
{
	[UpdateAfter(typeof(MovementSystem))]
	[UpdateInGroup(typeof(FixedSimulationSystemGroup))]
	public class BroadPhaseSystem : JobComponentSystem
	{
		public int EntitiesPerBucket = 3;

		public int XBuckets { get; private set; }
		public int YBuckets { get; private set; }

		private NativeMultiHashMap<int, Entity> buckets;

		protected override void OnCreate()
		{
			const int bucketCount = 400;
//			float aspect = Camera.main.aspect;

//			XBuckets = Mathf.CeilToInt(Mathf.Sqrt(aspect * bucketCount / EntitiesPerBucket));
//			YBuckets = Mathf.CeilToInt(Mathf.Sqrt(1f / aspect * bucketCount / EntitiesPerBucket));

			buckets = new NativeMultiHashMap<int, Entity>(bucketCount, Allocator.Persistent);
		}

		protected override void OnDestroy()
		{
			buckets.Dispose();
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			buckets.Clear();

			return inputDeps;
		}
	}
}
