using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine;

namespace QFSW.GravityDOTS
{
    [BurstCompile]
    [UpdateInGroup(typeof(FixedSimulationSystemGroup))]
    public class MovementSystem : JobComponentSystem
    {
        private struct MovementJob : IJobForEach<Velocity, Translation>
        {
            public float DeltaTime;

            public void Execute([ReadOnly] ref Velocity velocity, ref Translation position)
            {
                float2 dx = velocity.Value * DeltaTime;

                position.Value += new float3(dx, 0);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDependencies)
        {
            MovementJob job = new MovementJob()
            {
                DeltaTime = Time.deltaTime
            };

            return job.Schedule(this, inputDependencies);
        }
    }
}
