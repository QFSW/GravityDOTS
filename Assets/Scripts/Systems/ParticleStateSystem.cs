/*using Unity.Entities;

namespace QFSW.GravityDOTS
{
	public struct ParticleTag : ISystemStateComponentData
	{

	}

	// will maintain particles that are alive, required for ComponentDataFromEntity
	public class ParticleStateSystem : ComponentSystem
	{
		private EntityQuery newParticlesQuery;
		private EntityQuery deadParticlesQuery;

		protected override void OnCreate()
		{
			newParticlesQuery = GetEntityQuery(
				typeof(Bounded),
				typeof(Mass),
				typeof(Radius),
				ComponentType.Exclude<ParticleTag>()
			);

			deadParticlesQuery = GetEntityQuery(
				ComponentType.Exclude<Bounded>(),
				ComponentType.Exclude<Mass>(),
				ComponentType.Exclude<Radius>(),
				typeof(ParticleTag)
			);
		}

		protected override void OnUpdate()
		{
			PostUpdateCommands.AddComponent(newParticlesQuery, typeof(ParticleTag));
			PostUpdateCommands.RemoveComponent(deadParticlesQuery, typeof(ParticleTag));
		}
	}
}*/