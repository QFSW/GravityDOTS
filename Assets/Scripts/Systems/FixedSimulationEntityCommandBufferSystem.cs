using Unity.Entities;
using Unity.Transforms;

namespace QFSW.GravityDOTS
{
	[UpdateAfter(typeof(FixedSimulationSystemGroup))]
	[UpdateBefore(typeof(TransformSystemGroup))]
	public class FixedSimulationEntityCommandBufferSystem : EntityCommandBufferSystem
	{

	}
}
