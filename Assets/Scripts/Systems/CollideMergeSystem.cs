using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace QFSW.GravityDOTS
{
	[UpdateAfter(typeof(BoundingBoxSystem))]
	[UpdateInGroup(typeof(FixedSimulationSystemGroup))]
	public class CollideMergeSystem : JobComponentSystem
	{
		
		
		private struct CMJob
		{
			
		}
		
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			
			return inputDeps;
		}
	}
}
