using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine;

namespace QFSW.GravityDOTS
{
    [UpdateInGroup(typeof(FixedSimulationSystemGroup))]
    public class MovementSystem : JobComponentSystem
    {
        private struct MovementJob : IJobForEach<Velocity, LocalToWorld>
        {
            public float DeltaTime;

            public void Execute([ReadOnly] ref Velocity velocity, ref LocalToWorld transform)
            {
                float2 dx = velocity.Value * DeltaTime;

                transform.Value += new float4x4(float4.zero, float4.zero, float4.zero, new float4(dx, 0, 0));
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
